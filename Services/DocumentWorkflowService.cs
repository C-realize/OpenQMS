using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Data;
using OpenQMS.Models;

namespace OpenQMS.Services;

public class DocumentWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public DocumentWorkflowService(
        ApplicationDbContext context,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<DocumentRevision> CreateRevisionAsync(
        int documentId,
        string revision,
        string changeSummary,
        int actorId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.ControlledDocuments.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException("The controlled document does not exist.");

        if (string.IsNullOrWhiteSpace(revision) || string.IsNullOrWhiteSpace(changeSummary))
        {
            throw new ArgumentException("A revision identifier and change summary are required.");
        }

        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("The uploaded document file is empty.");
        }

        if (await _context.DocumentRevisions.AnyAsync(
            item => item.ControlledDocumentId == documentId && item.Revision == revision,
            cancellationToken))
        {
            throw new InvalidOperationException("That revision identifier already exists for this document.");
        }

        await using var content = new MemoryStream();
        await file.CopyToAsync(content, cancellationToken);

        var newRevision = new DocumentRevision
        {
            ControlledDocumentId = document.Id,
            Revision = revision.Trim(),
            ChangeSummary = changeSummary.Trim(),
            AuthoredByUserId = actorId,
            FileName = Path.GetFileName(file.FileName.Replace('\\', '/')),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            FileContent = content.ToArray()
        };

        _context.DocumentRevisions.Add(newRevision);
        AddAudit(actorId, "RevisionCreated", "DocumentRevision", newRevision.Revision,
            $"Document {document.DocumentId}");
        await _context.SaveChangesAsync(cancellationToken);
        return newRevision;
    }

    public async Task SubmitForReviewAsync(int revisionId, int actorId, CancellationToken cancellationToken = default)
    {
        var revision = await GetRevisionAsync(revisionId, cancellationToken);
        DocumentWorkflowRules.EnsureCanSubmitForReview(revision);
        revision.Status = DocumentRevisionStatus.InReview;
        AddAudit(actorId, "RevisionSubmittedForReview", "DocumentRevision", revision.Id.ToString());
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordReviewAsync(int revisionId, int actorId, bool accepted, CancellationToken cancellationToken = default)
    {
        var revision = await GetRevisionAsync(revisionId, cancellationToken);
        DocumentWorkflowRules.EnsureCanReview(revision, actorId);

        if (!accepted)
        {
            revision.Status = DocumentRevisionStatus.Rejected;
            AddAudit(actorId, "RevisionRejected", "DocumentRevision", revision.Id.ToString());
        }
        else
        {
            revision.ReviewedByUserId = actorId;
            revision.ReviewedAtUtc = DateTime.UtcNow;
            AddAudit(actorId, "RevisionReviewed", "DocumentRevision", revision.Id.ToString());
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(int revisionId, int actorId, DateTime effectiveAtUtc, CancellationToken cancellationToken = default)
    {
        var revision = await GetRevisionAsync(revisionId, cancellationToken);
        DocumentWorkflowRules.EnsureCanApprove(revision, actorId);

        revision.Status = DocumentRevisionStatus.Approved;
        revision.ApprovedByUserId = actorId;
        revision.ApprovedAtUtc = DateTime.UtcNow;
        revision.EffectiveAtUtc = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        AddAudit(actorId, "RevisionApproved", "DocumentRevision", revision.Id.ToString(),
            $"Effective {revision.EffectiveAtUtc:O}");
        await _context.SaveChangesAsync(cancellationToken);
        await ActivateDueRevisionsAsync(cancellationToken);
    }

    public async Task ActivateDueRevisionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueRevisions = await _context.DocumentRevisions
            .Include(revision => revision.ControlledDocument)
            .Where(revision => revision.Status == DocumentRevisionStatus.Approved
                && revision.EffectiveAtUtc <= now)
            .OrderBy(revision => revision.ControlledDocumentId)
            .ThenBy(revision => revision.EffectiveAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var revisionsForDocument in dueRevisions.GroupBy(revision => revision.ControlledDocumentId))
        {
            var orderedRevisions = revisionsForDocument
                .OrderBy(revision => revision.EffectiveAtUtc)
                .ThenBy(revision => revision.Id)
                .ToList();
            foreach (var superseded in orderedRevisions.Take(orderedRevisions.Count - 1))
            {
                superseded.Status = DocumentRevisionStatus.Obsolete;
                AddAudit(superseded.ApprovedByUserId, "RevisionSupersededBeforeEffective",
                    "DocumentRevision", superseded.Id.ToString());
            }

            var revision = orderedRevisions[^1];
            var otherEffectiveRevisions = await _context.DocumentRevisions
                .Where(other => other.ControlledDocumentId == revision.ControlledDocumentId
                    && other.Status == DocumentRevisionStatus.Effective)
                .ToListAsync(cancellationToken);

            var obsoletedRevisions = DocumentWorkflowRules.Activate(revision, otherEffectiveRevisions, now);
            foreach (var oldRevision in obsoletedRevisions)
            {
                AddAudit(null, "RevisionObsoleted", "DocumentRevision", oldRevision.Id.ToString());
            }

            AddAudit(revision.ApprovedByUserId, "RevisionEffective", "DocumentRevision", revision.Id.ToString());

            if (revision.ControlledDocument.RequiresTraining)
            {
                var assignedUsers = await _userManager.Users
                    .Where(user => user.EmailConfirmed)
                    .Select(user => user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var userId in assignedUsers)
                {
                    await EnsureTrainingAssignmentAsync(revision, userId, revision.ApprovedByUserId, cancellationToken);
                }
            }
        }

        if (dueRevisions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AssignCurrentTrainingToUserAsync(
        int traineeId,
        int? actorId,
        CancellationToken cancellationToken = default)
    {
        var activeRevisions = await _context.DocumentRevisions
            .Include(revision => revision.ControlledDocument)
            .Where(revision => revision.Status == DocumentRevisionStatus.Effective
                && revision.ControlledDocument.RequiresTraining)
            .ToListAsync(cancellationToken);

        foreach (var revision in activeRevisions)
        {
            await EnsureTrainingAssignmentAsync(revision, traineeId, actorId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignCurrentTrainingForDocumentAsync(
        int documentId,
        int actorId,
        CancellationToken cancellationToken = default)
    {
        var revision = await _context.DocumentRevisions
            .Include(item => item.ControlledDocument)
            .FirstOrDefaultAsync(item => item.ControlledDocumentId == documentId
                && item.Status == DocumentRevisionStatus.Effective,
                cancellationToken);
        if (revision is null || !revision.ControlledDocument.RequiresTraining)
        {
            return;
        }

        var confirmedUsers = await _userManager.Users
            .Where(user => user.EmailConfirmed)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in confirmedUsers)
        {
            await EnsureTrainingAssignmentAsync(revision, userId, actorId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureTrainingAssignmentAsync(
        DocumentRevision revision,
        int traineeId,
        int? actorId,
        CancellationToken cancellationToken)
    {
        if (await _context.TrainingAssignments.AnyAsync(
            assignment => assignment.DocumentRevisionId == revision.Id
                && assignment.TraineeId == traineeId,
            cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _context.TrainingAssignments.Add(new TrainingAssignment
        {
            DocumentRevisionId = revision.Id,
            TraineeId = traineeId,
            AssignedAtUtc = now,
            DueAtUtc = revision.ControlledDocument.TrainingDueDays.HasValue
                ? now.AddDays(revision.ControlledDocument.TrainingDueDays.Value)
                : null
        });
        AddAudit(actorId, "TrainingAssigned", "TrainingAssignment", $"{revision.Id}:{traineeId}");
    }

    private async Task<DocumentRevision> GetRevisionAsync(int revisionId, CancellationToken cancellationToken)
    {
        return await _context.DocumentRevisions
            .Include(revision => revision.ControlledDocument)
            .FirstOrDefaultAsync(revision => revision.Id == revisionId, cancellationToken)
            ?? throw new KeyNotFoundException("The document revision does not exist.");
    }

    private void AddAudit(int? actorId, string action, string entityType, string entityId, string? details = null)
    {
        _context.QmsAuditEvents.Add(new QmsAuditEvent
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });
    }
}

public class DocumentEffectivityWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentEffectivityWorker> _logger;

    public DocumentEffectivityWorker(IServiceScopeFactory scopeFactory, ILogger<DocumentEffectivityWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var workflow = scope.ServiceProvider.GetRequiredService<DocumentWorkflowService>();
                await workflow.ActivateDueRevisionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not activate scheduled document revisions.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
