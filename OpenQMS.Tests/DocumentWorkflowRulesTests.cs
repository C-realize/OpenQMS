using OpenQMS.Models;
using OpenQMS.Services;
using Xunit;

namespace OpenQMS.Tests;

public class DocumentWorkflowRulesTests
{
    [Fact]
    public void ReviewAndApprovalRequireSeparatePeopleAndRecordedReview()
    {
        var revision = new DocumentRevision
        {
            Status = DocumentRevisionStatus.InReview,
            AuthoredByUserId = 1
        };

        Assert.Throws<InvalidOperationException>(() => DocumentWorkflowRules.EnsureCanReview(revision, 1));

        revision.ReviewedByUserId = 2;
        revision.ReviewedAtUtc = DateTime.UtcNow;
        Assert.Throws<InvalidOperationException>(() => DocumentWorkflowRules.EnsureCanApprove(revision, 2));
        DocumentWorkflowRules.EnsureCanApprove(revision, 3);
    }

    [Fact]
    public void ActivatingApprovedRevisionObsoletesButRetainsPreviousRevision()
    {
        var previous = new DocumentRevision
        {
            Id = 1,
            Status = DocumentRevisionStatus.Effective
        };
        var next = new DocumentRevision
        {
            Id = 2,
            Status = DocumentRevisionStatus.Approved,
            EffectiveAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        var history = new[] { previous, next };

        var obsoleted = DocumentWorkflowRules.Activate(next, history, DateTime.UtcNow);

        Assert.Equal(DocumentRevisionStatus.Obsolete, previous.Status);
        Assert.Equal(DocumentRevisionStatus.Effective, next.Status);
        Assert.Contains(previous, history);
        Assert.Contains(previous, obsoleted);
    }

    [Fact]
    public void FutureOrUnapprovedRevisionCannotBecomeEffective()
    {
        var revision = new DocumentRevision
        {
            Status = DocumentRevisionStatus.Approved,
            EffectiveAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Assert.Throws<InvalidOperationException>(() =>
            DocumentWorkflowRules.Activate(revision, Array.Empty<DocumentRevision>(), DateTime.UtcNow));
    }

    [Fact]
    public void AuditorGrantRemainsReadOnly()
    {
        var grant = new DocumentAccessGrant
        {
            RoleName = "Auditor",
            CanRead = true,
            CanDraft = true,
            CanReview = true,
            CanApprove = true
        };

        Assert.True(DocumentAccessRules.Allows(grant, DocumentPermission.Read));
        Assert.False(DocumentAccessRules.Allows(grant, DocumentPermission.Draft));
        Assert.False(DocumentAccessRules.Allows(grant, DocumentPermission.Review));
        Assert.False(DocumentAccessRules.Allows(grant, DocumentPermission.Approve));
    }
}

public class TrainingAssignmentRulesTests
{
    [Fact]
    public void TrainingCompletionRequiresEvidenceAndCanOnlyBeAcknowledgedOnce()
    {
        var assignment = new TrainingAssignment();
        TrainingAssignmentRules.Start(assignment);
        Assert.Throws<ArgumentException>(() =>
            TrainingAssignmentRules.Complete(assignment, 7, " ", DateTime.UtcNow));

        TrainingAssignmentRules.Complete(assignment, 7, "Read and understood", DateTime.UtcNow);
        Assert.Equal(TrainingAssignmentStatus.Completed, assignment.Status);
        Assert.Equal(7, assignment.CompletedByUserId);
        Assert.True(TrainingAssignmentRules.Acknowledge(assignment, DateTime.UtcNow));
        Assert.False(TrainingAssignmentRules.Acknowledge(assignment, DateTime.UtcNow));
    }

    [Fact]
    public void OverdueStatusIsComputedOnlyForIncompleteAssignmentsWithDueDate()
    {
        var assignment = new TrainingAssignment
        {
            DueAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(TrainingAssignmentRules.IsOverdue(assignment, DateTime.UtcNow));
        assignment.Status = TrainingAssignmentStatus.Completed;
        Assert.False(TrainingAssignmentRules.IsOverdue(assignment, DateTime.UtcNow));
    }
}

public class EmailOptionsTests
{
    [Fact]
    public void ProductionConfigurationRequiresSecurePublicEmailDelivery()
    {
        var valid = new EmailOptions
        {
            From = "qms@example.eu",
            PublicBaseUrl = "https://qms.example.eu/",
            Host = "smtp.example.eu",
            Port = 587,
            UserName = "qms-sender",
            Password = "configured",
            UseSsl = true
        };

        Assert.True(valid.IsValidForProduction());
        valid.PublicBaseUrl = "http://qms.example.eu/";
        Assert.False(valid.IsValidForProduction());
        valid.PublicBaseUrl = "https://qms.example.eu/";
        valid.Password = null;
        Assert.False(valid.IsValidForProduction());
    }
}
