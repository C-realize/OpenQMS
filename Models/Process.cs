using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models
{
    public class Process
    {
        public int Id { get; set; }
        [Display(Name = "Process Name")]
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Version { get; set; }
        public string? EditedBy { get; set; }
        public DateTime EditedOn { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public virtual ICollection<Deviation>? Deviations { get; set; }
        public virtual ICollection<Capa>? Capas { get; set; }
        public virtual ICollection<Change>? Changes { get; set; }
        public ProcessStatus Status { get; set; }
        public enum ProcessStatus
        {
            Draft,
            Approved,
        }
    }
}
