using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models;

public class ControlledDocument
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string DocumentId { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool RequiresTraining { get; set; }

    public int? TrainingDueDays { get; set; }

    public ICollection<DocumentRevision> Revisions { get; set; } = new List<DocumentRevision>();
}
