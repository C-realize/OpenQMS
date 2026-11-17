using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models;

public class QmsAuditEvent
{
    public long Id { get; set; }

    public int? ActorUserId { get; set; }

    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EntityType { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string EntityId { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Details { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
