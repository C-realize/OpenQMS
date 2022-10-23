using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Deviation
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        public int? ProcessId { get; set; }
        [ForeignKey("ProcessId")]
        public virtual Process? Process { get; set; }
        public int? AssetId { get; set; }
        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }
        public virtual ICollection<Capa>? Capas { get; set; }
        public string Title { get; set; }
        public string Identification { get; set; }
        public string IdentifiedBy { get; set; }
        public DateTime IdentifiedOn { get; set; }
        public string? Evaluation { get; set; }
        public string? EvaluatedBy { get; set; }
        public DateTime? EvaluatedOn { get; set; }
        public string? AcceptedBy { get; set; }
        public DateTime? AcceptedOn { get; set; }
        public string? Resolution { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public DeviationStatus Status { get; set; }
        public enum DeviationStatus
        {
            Identification,
            Evaluation,
            Accepted,
            Resolution,
            Approved
        }
    }
}
