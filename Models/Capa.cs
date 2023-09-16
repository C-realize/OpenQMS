using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Capa
    {
        public int Id { get; set; }
        public string CapaId { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        public int? ProcessId { get; set; }
        [ForeignKey("ProcessId")]
        public virtual Process? Process { get; set; }
        public int? AssetId { get; set; }
        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }
        public int? MaterialId { get; set; }
        [ForeignKey("MaterialId")]
        public virtual Material? Material { get; set; }
        public int? DeviationId { get; set; }
        [ForeignKey("DeviationId")]
        public virtual Deviation? Deviation { get; set; }
        public virtual ICollection<Change>? Changes { get; set; }
        public string Title { get; set; }
        public string CorrectiveAction { get; set; }
        public string PreventiveAction { get; set; }
        public string DeterminedBy { get; set; }
        public DateTime DeterminedOn { get; set; }
        public string? Assessment { get; set; }
        public string? AssessedBy { get; set; }
        public DateTime? AssessedOn { get; set; }
        public string? AcceptedBy { get; set; }
        public DateTime? AcceptedOn { get; set; }
        public string? Implementation { get; set; }
        public string? ImplementedBy { get; set; }
        public DateTime? ImplementedOn { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string? ExportFilePath { get; set; }
        public CapaStatus Status { get; set; }
        public enum CapaStatus
        {
            Determination,
            Assessment,
            Accepted,
            Implementation,
            Approved
        }
    }
}
