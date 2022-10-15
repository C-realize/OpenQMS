using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Change
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        public int? AssetId { get; set; }
        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }
        public int? CapaId { get; set; }
        [ForeignKey("CapaId")]
        public virtual Capa? Capa { get; set; }
        public string Title { get; set; }
        public string Proposal { get; set; }
        public string ProposedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime ProposedOn { get; set; }
        public string? Assessment { get; set; }
        public string? AssessedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime? AssessedOn { get; set; }
        public string? AcceptedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime? AcceptedOn { get; set; }
        public string? Implementation { get; set; }
        public string? ImplementedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime? ImplementedOn { get; set; }
        public string? ApprovedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime? ApprovedOn { get; set; }
        public ChangeStatus Status { get; set; }
        public enum ChangeStatus
        {
            Proposal,
            Assessment,
            Accepted,
            Implementation,
            Approved
        }
    }
}
