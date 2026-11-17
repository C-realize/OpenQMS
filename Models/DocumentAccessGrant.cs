namespace OpenQMS.Models;

public class DocumentAccessGrant
{
    public int Id { get; set; }

    public int ControlledDocumentId { get; set; }

    public ControlledDocument ControlledDocument { get; set; } = null!;

    public string RoleName { get; set; } = string.Empty;

    public bool CanRead { get; set; }

    public bool CanDraft { get; set; }

    public bool CanReview { get; set; }

    public bool CanApprove { get; set; }
}
