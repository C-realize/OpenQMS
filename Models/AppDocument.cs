using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models
{
    public class AppDocument
    {
        public int Id { get; set; }
        public string DocumentId { get; set; }
        public string? GeneratedFrom { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Version { get; set; }
        public byte[]? Content { get; set; }
        public string? FilePath { get; set; }
        public string FileType { get; set; }
        public string FileExtension { get; set; }
        public string AuthoredBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime AuthoredOn { get; set; }
        public string? ApprovedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime? ApprovedOn { get; set; }
        public bool IsLocked { get; set; }
        public string? ExportFilePath { get; set; }
        public DocumentStatus Status { get; set; }
        public enum DocumentStatus
        {
            Draft,
            Approved,
            Rejected,
            Obsolete
        }
    }
}
