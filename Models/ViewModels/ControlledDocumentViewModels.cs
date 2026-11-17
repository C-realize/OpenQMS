using System.ComponentModel.DataAnnotations;
using OpenQMS.Models;

namespace OpenQMS.Models.ViewModels;

public class DocumentRegisterInput
{
    [Required, StringLength(40)]
    public string DocumentId { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool RequiresTraining { get; set; }

    [Range(1, 3650)]
    public int? TrainingDueDays { get; set; }
}

public class DocumentMetadataInput
{
    public int Id { get; set; }

    [Required, StringLength(240)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool RequiresTraining { get; set; }

    [Range(1, 3650)]
    public int? TrainingDueDays { get; set; }
}

public class DocumentRoleAccessInput
{
    public string RoleName { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanDraft { get; set; }
    public bool CanReview { get; set; }
    public bool CanApprove { get; set; }
}

public class DocumentAccessEditorViewModel
{
    public int DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public List<DocumentRoleAccessInput> Roles { get; set; } = new();
}

public class TrainingCompletionInput
{
    [Required, StringLength(2000)]
    public string CompletionEvidence { get; set; } = string.Empty;
}

public class TrainingAssignmentListItem
{
    public required TrainingAssignment Assignment { get; init; }
    public required string TraineeName { get; init; }
    public required string DocumentNumber { get; init; }
    public required string DocumentTitle { get; init; }
    public bool IsOverdue { get; init; }
}
