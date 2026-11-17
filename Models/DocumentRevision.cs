using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models;

public class DocumentRevision
{
    public int Id { get; set; }

    public int ControlledDocumentId { get; set; }

    [ForeignKey(nameof(ControlledDocumentId))]
    public ControlledDocument ControlledDocument { get; set; } = null!;

    [Required, StringLength(24)]
    public string Revision { get; set; } = "1.0";

    [Required, StringLength(2000)]
    public string ChangeSummary { get; set; } = string.Empty;

    public DocumentRevisionStatus Status { get; set; } = DocumentRevisionStatus.Draft;

    public int? AuthoredByUserId { get; set; }

    public DateTime AuthoredAtUtc { get; set; } = DateTime.UtcNow;

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? EffectiveAtUtc { get; set; }

    [StringLength(260)]
    public string? FileName { get; set; }

    [StringLength(120)]
    public string? ContentType { get; set; }

    public byte[]? FileContent { get; set; }
}

public enum DocumentRevisionStatus
{
    Draft,
    InReview,
    Approved,
    Effective,
    Obsolete,
    Rejected
}
