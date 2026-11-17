using OpenQMS.Models;

namespace OpenQMS.Services;

public static class DocumentWorkflowRules
{
    public static void EnsureCanSubmitForReview(DocumentRevision revision)
    {
        EnsureStatus(revision, DocumentRevisionStatus.Draft);
    }

    public static void EnsureCanReview(DocumentRevision revision, int actorId)
    {
        EnsureStatus(revision, DocumentRevisionStatus.InReview);
        if (revision.AuthoredByUserId == actorId)
        {
            throw new InvalidOperationException("The author cannot review their own revision.");
        }

        if (revision.ReviewedAtUtc.HasValue)
        {
            throw new InvalidOperationException("This revision has already been reviewed.");
        }
    }

    public static void EnsureCanApprove(DocumentRevision revision, int actorId)
    {
        EnsureStatus(revision, DocumentRevisionStatus.InReview);
        if (!revision.ReviewedAtUtc.HasValue
            || revision.AuthoredByUserId == actorId
            || revision.ReviewedByUserId == actorId)
        {
            throw new InvalidOperationException("Approval requires a completed review by someone other than the author and approver.");
        }
    }

    public static IReadOnlyList<DocumentRevision> Activate(
        DocumentRevision revision,
        IEnumerable<DocumentRevision> previousEffectiveRevisions,
        DateTime nowUtc)
    {
        if (revision.Status != DocumentRevisionStatus.Approved
            || !revision.EffectiveAtUtc.HasValue
            || revision.EffectiveAtUtc.Value > nowUtc)
        {
            throw new InvalidOperationException("Only an approved revision whose effective date has arrived can become effective.");
        }

        var obsoleted = previousEffectiveRevisions
            .Where(previous => previous.Id != revision.Id
                && previous.Status == DocumentRevisionStatus.Effective)
            .ToList();
        foreach (var previous in obsoleted)
        {
            previous.Status = DocumentRevisionStatus.Obsolete;
        }

        revision.Status = DocumentRevisionStatus.Effective;
        return obsoleted;
    }

    private static void EnsureStatus(DocumentRevision revision, DocumentRevisionStatus expected)
    {
        if (revision.Status != expected)
        {
            throw new InvalidOperationException($"This action requires a revision in {expected} state.");
        }
    }
}

public static class DocumentAccessRules
{
    public static bool Allows(DocumentAccessGrant grant, DocumentPermission permission)
    {
        if (string.Equals(grant.RoleName, "Auditor", StringComparison.Ordinal)
            && permission != DocumentPermission.Read)
        {
            return false;
        }

        return permission switch
        {
            DocumentPermission.Read => grant.CanRead,
            DocumentPermission.Draft => grant.CanDraft,
            DocumentPermission.Review => grant.CanReview,
            DocumentPermission.Approve => grant.CanApprove,
            _ => false
        };
    }
}

public static class TrainingAssignmentRules
{
    public static void Start(TrainingAssignment assignment)
    {
        if (assignment.Status != TrainingAssignmentStatus.Assigned)
        {
            throw new InvalidOperationException("Only assigned training can be started.");
        }

        assignment.Status = TrainingAssignmentStatus.InProgress;
    }

    public static void Complete(TrainingAssignment assignment, int actorId, string evidence, DateTime completedAtUtc)
    {
        if (assignment.Status == TrainingAssignmentStatus.Completed)
        {
            throw new InvalidOperationException("Completed training cannot be altered.");
        }

        if (string.IsNullOrWhiteSpace(evidence))
        {
            throw new ArgumentException("Completion evidence is required.", nameof(evidence));
        }

        assignment.Status = TrainingAssignmentStatus.Completed;
        assignment.CompletedAtUtc = completedAtUtc;
        assignment.CompletedByUserId = actorId;
        assignment.CompletionEvidence = evidence.Trim();
    }

    public static bool Acknowledge(TrainingAssignment assignment, DateTime acknowledgedAtUtc)
    {
        if (assignment.Status != TrainingAssignmentStatus.Completed)
        {
            throw new InvalidOperationException("Only completed training can be acknowledged.");
        }

        if (assignment.AcknowledgedAtUtc.HasValue)
        {
            return false;
        }

        assignment.AcknowledgedAtUtc = acknowledgedAtUtc;
        return true;
    }

    public static bool IsOverdue(TrainingAssignment assignment, DateTime nowUtc)
    {
        return assignment.Status != TrainingAssignmentStatus.Completed
            && assignment.DueAtUtc.HasValue
            && assignment.DueAtUtc.Value < nowUtc;
    }
}
