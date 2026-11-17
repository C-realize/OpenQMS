using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
using OpenQMS.Services;

namespace OpenQMS.Controllers;

[Authorize]
public class ControlledDocumentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly DocumentAuthorizationService _authorization;
    private readonly DocumentWorkflowService _workflow;

    public ControlledDocumentsController(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        DocumentAuthorizationService authorization,
        DocumentWorkflowService workflow)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _authorization = authorization;
        _workflow = workflow;
    }

    public async Task<IActionResult> Index()
    {
        var documents = await _context.ControlledDocuments
            .Include(document => document.Revisions)
            .OrderBy(document => document.DocumentId)
            .ToListAsync();
        var visible = new List<ControlledDocument>();
        foreach (var document in documents)
        {
            if (await _authorization.HasPermissionAsync(User, document.Id, DocumentPermission.Read))
            {
                visible.Add(document);
            }
        }

        return View(visible);
    }

    public async Task<IActionResult> Details(int id)
    {
        var document = await _context.ControlledDocuments
            .Include(item => item.Revisions)
            .ThenInclude(revision => revision.ControlledDocument)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        if (!await _authorization.HasPermissionAsync(User, id, DocumentPermission.Read))
        {
            return Forbid();
        }

        return View(document);
    }

    [Authorize(Roles = Constants.AdministratorsRole)]
    public async Task<IActionResult> EditMetadata(int id)
    {
        var document = await _context.ControlledDocuments.FindAsync(id);
        return document is null
            ? NotFound()
            : View(new DocumentMetadataInput
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                RequiresTraining = document.RequiresTraining,
                TrainingDueDays = document.TrainingDueDays
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.AdministratorsRole)]
    public async Task<IActionResult> EditMetadata(DocumentMetadataInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var document = await _context.ControlledDocuments.FindAsync(input.Id);
        if (document is null)
        {
            return NotFound();
        }

        var oldTrainingRequirement = document.RequiresTraining;
        document.Title = input.Title.Trim();
        document.Description = input.Description?.Trim();
        document.RequiresTraining = input.RequiresTraining;
        document.TrainingDueDays = input.TrainingDueDays;
        _context.QmsAuditEvents.Add(new QmsAuditEvent
        {
            ActorUserId = GetActorId(),
            Action = "DocumentMetadataChanged",
            EntityType = nameof(ControlledDocument),
            EntityId = document.DocumentId,
            Details = $"Title: {document.Title}; training requirement: {oldTrainingRequirement} -> {document.RequiresTraining}"
        });
        await _context.SaveChangesAsync();
        await _workflow.AssignCurrentTrainingForDocumentAsync(document.Id, GetActorId());
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    [Authorize(Roles = Constants.AdministratorsRole)]
    public IActionResult Create()
    {
        return View(new DocumentRegisterInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.AdministratorsRole)]
    public async Task<IActionResult> Create(DocumentRegisterInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var documentId = input.DocumentId.Trim();
        if (await _context.ControlledDocuments.AnyAsync(document => document.DocumentId == documentId))
        {
            ModelState.AddModelError(nameof(input.DocumentId), "That document ID is already registered.");
            return View(input);
        }

        var document = new ControlledDocument
        {
            DocumentId = documentId,
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            RequiresTraining = input.RequiresTraining,
            TrainingDueDays = input.TrainingDueDays
        };
        _context.ControlledDocuments.Add(document);
        _context.QmsAuditEvents.Add(new QmsAuditEvent
        {
            ActorUserId = GetActorId(),
            Action = "DocumentRegistered",
            EntityType = nameof(ControlledDocument),
            EntityId = document.DocumentId,
            Details = document.Title
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    public async Task<IActionResult> CreateRevision(int id)
    {
        if (!await _authorization.HasPermissionAsync(User, id, DocumentPermission.Draft))
        {
            return Forbid();
        }

        var document = await _context.ControlledDocuments.FindAsync(id);
        return document is null ? NotFound() : View(document);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRevision(int id, string revision, string changeSummary, IFormFile? file)
    {
        if (!await _authorization.HasPermissionAsync(User, id, DocumentPermission.Draft))
        {
            return Forbid();
        }

        try
        {
            var created = await _workflow.CreateRevisionAsync(
                id,
                revision,
                changeSummary,
                GetActorId(),
                file);
            return RedirectToAction(nameof(Details), new { id = created.ControlledDocumentId });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        var document = await _context.ControlledDocuments.FindAsync(id);
        return document is null ? NotFound() : View(document);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int revisionId)
    {
        var revision = await _context.DocumentRevisions.FindAsync(revisionId);
        if (revision is null)
        {
            return NotFound();
        }

        if (!await _authorization.HasPermissionAsync(User, revision.ControlledDocumentId, DocumentPermission.Draft))
        {
            return Forbid();
        }

        try
        {
            await _workflow.SubmitForReviewAsync(revisionId, GetActorId());
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Details), new { id = revision.ControlledDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int revisionId, bool accepted)
    {
        var revision = await _context.DocumentRevisions.FindAsync(revisionId);
        if (revision is null)
        {
            return NotFound();
        }

        if (!await _authorization.HasPermissionAsync(User, revision.ControlledDocumentId, DocumentPermission.Review))
        {
            return Forbid();
        }

        try
        {
            await _workflow.RecordReviewAsync(revisionId, GetActorId(), accepted);
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Details), new { id = revision.ControlledDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int revisionId, DateTime? effectiveAtUtc)
    {
        var revision = await _context.DocumentRevisions.FindAsync(revisionId);
        if (revision is null)
        {
            return NotFound();
        }

        if (!await _authorization.HasPermissionAsync(User, revision.ControlledDocumentId, DocumentPermission.Approve))
        {
            return Forbid();
        }

        if (!effectiveAtUtc.HasValue)
        {
            TempData["ErrorMessage"] = "Select an effective date.";
            return RedirectToAction(nameof(Details), new { id = revision.ControlledDocumentId });
        }

        try
        {
            await _workflow.ApproveAsync(revisionId, GetActorId(), effectiveAtUtc.Value);
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = revision.ControlledDocumentId });
    }

    public async Task<IActionResult> Download(int revisionId)
    {
        var revision = await _context.DocumentRevisions
            .FirstOrDefaultAsync(item => item.Id == revisionId);
        if (revision is null)
        {
            return NotFound();
        }

        if (!await _authorization.HasPermissionAsync(User, revision.ControlledDocumentId, DocumentPermission.Read))
        {
            return Forbid();
        }

        if (revision.FileContent is null)
        {
            return NotFound();
        }

        return File(
            revision.FileContent,
            revision.ContentType ?? "application/octet-stream",
            revision.FileName ?? $"revision-{revision.Id}");
    }

    [Authorize(Roles = Constants.AdministratorsRole)]
    public async Task<IActionResult> EditAccess(int id)
    {
        var document = await _context.ControlledDocuments
            .Include(item => item.Revisions)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        var grants = await _context.DocumentAccessGrants
            .Where(grant => grant.ControlledDocumentId == id)
            .ToDictionaryAsync(grant => grant.RoleName);
        var roles = await _roleManager.Roles
            .Where(role => role.Name != null)
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();

        return View(new DocumentAccessEditorViewModel
        {
            DocumentId = id,
            DocumentNumber = document.DocumentId,
            DocumentTitle = document.Title,
            Roles = roles.Select(role => grants.TryGetValue(role, out var grant)
                ? new DocumentRoleAccessInput
                {
                    RoleName = role,
                    CanRead = grant.CanRead,
                    CanDraft = grant.CanDraft,
                    CanReview = grant.CanReview,
                    CanApprove = grant.CanApprove
                }
                : new DocumentRoleAccessInput { RoleName = role })
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.AdministratorsRole)]
    public async Task<IActionResult> EditAccess(DocumentAccessEditorViewModel input)
    {
        var document = await _context.ControlledDocuments.FindAsync(input.DocumentId);
        if (document is null)
        {
            return NotFound();
        }

        var validRoleNames = await _roleManager.Roles
            .Where(role => role.Name != null)
            .Select(role => role.Name!)
            .ToListAsync();
        if (input.Roles.Any(role => !validRoleNames.Contains(role.RoleName, StringComparer.Ordinal)))
        {
            return BadRequest("The access list contains an unknown role.");
        }

        var grants = await _context.DocumentAccessGrants
            .Where(grant => grant.ControlledDocumentId == input.DocumentId)
            .ToDictionaryAsync(grant => grant.RoleName);

        foreach (var role in input.Roles)
        {
            if (string.Equals(role.RoleName, "Auditor", StringComparison.Ordinal))
            {
                role.CanDraft = false;
                role.CanReview = false;
                role.CanApprove = false;
            }

            var hasAccess = role.CanRead || role.CanDraft || role.CanReview || role.CanApprove;
            if (!grants.TryGetValue(role.RoleName, out var grant))
            {
                if (!hasAccess)
                {
                    continue;
                }

                grant = new DocumentAccessGrant
                {
                    ControlledDocumentId = document.Id,
                    RoleName = role.RoleName
                };
                _context.DocumentAccessGrants.Add(grant);
            }

            grant.CanRead = role.CanRead || role.CanDraft || role.CanReview || role.CanApprove;
            grant.CanDraft = role.CanDraft;
            grant.CanReview = role.CanReview;
            grant.CanApprove = role.CanApprove;
            if (!hasAccess && grant.Id != 0)
            {
                _context.DocumentAccessGrants.Remove(grant);
            }
        }

        _context.QmsAuditEvents.Add(new QmsAuditEvent
        {
            ActorUserId = GetActorId(),
            Action = "DocumentAccessChanged",
            EntityType = nameof(ControlledDocument),
            EntityId = document.DocumentId,
            Details = string.Join("; ", input.Roles.Select(role =>
                $"{role.RoleName}: read={role.CanRead || role.CanDraft || role.CanReview || role.CanApprove}, draft={role.CanDraft}, review={role.CanReview}, approve={role.CanApprove}"))
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    [Authorize(Roles = Constants.AdministratorsRole)]
    public async Task<IActionResult> Audit()
    {
        var auditEvents = await _context.QmsAuditEvents
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(500)
            .ToListAsync();
        return View(auditEvents);
    }

    private int GetActorId()
    {
        return int.TryParse(_userManager.GetUserId(User), out var actorId)
            ? actorId
            : throw new InvalidOperationException("The signed-in user ID is invalid.");
    }
}
