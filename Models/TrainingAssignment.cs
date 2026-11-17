using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models;

public class TrainingAssignment
{
    public int Id { get; set; }

    public int DocumentRevisionId { get; set; }

    public DocumentRevision DocumentRevision { get; set; } = null!;

    public int TraineeId { get; set; }

    public AppUser Trainee { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DueAtUtc { get; set; }

    public TrainingAssignmentStatus Status { get; set; } = TrainingAssignmentStatus.Assigned;

    public DateTime? CompletedAtUtc { get; set; }

    public int? CompletedByUserId { get; set; }

    [StringLength(2000)]
    public string? CompletionEvidence { get; set; }

    public DateTime? AcknowledgedAtUtc { get; set; }
}

public enum TrainingAssignmentStatus
{
    Assigned,
    InProgress,
    Completed
}
