using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models
{
    public class Process
    {
        public Process()
        {
            this.Changes = new HashSet<Change>();
        }

        public int Id { get; set; }
        public string ProcessId { get; set; }
        public string? GeneratedFrom { get; set; }
        [Display(Name = "Process Name")]
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Version { get; set; }
        [Display(Name = "Edited By")]
        public string? EditedBy { get; set; }
        [Display(Name = "Edited On")]
        public DateTime EditedOn { get; set; }
        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }
        [ForeignKey("ApprovedBy")]
        public DateTime? ApprovedOn { get; set; }
        public bool IsLocked { get; set; }
        public string? ExportFilePath { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        public virtual ICollection<Asset>? Assets { get; set; }
        public virtual ICollection<Material>? Materials { get; set; }
        public virtual ICollection<Deviation>? Deviations { get; set; }
        public virtual ICollection<Capa>? Capas { get; set; }
        public virtual ICollection<Change>? Changes { get; set; }
        public ProcessStatus Status { get; set; }
        public enum ProcessStatus
        {
            Draft,
            Approved,
            Obsolete
        }
    }
}
