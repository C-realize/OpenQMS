using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Product
    {
        public Product()
        {
            this.Changes = new HashSet<Change>();
        }
        public int Id { get; set; }
        [Display(Name ="Product Name")]
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Version { get; set; }
        [Display(Name = "Edited By")]
        public int? EditedBy { get; set; }
        [ForeignKey("EditedBy")]
        public virtual AppUser? EditedByUser { get; set; }
        [Display(Name = "Edited On")]
        public DateTime EditedOn { get; set; }
        [Display(Name = "Approved By")]
        public int? ApprovedBy { get; set; }
        [ForeignKey("ApprovedBy")]
        public virtual AppUser? ApprovedByUser { get; set; }
        [Display(Name = "Approved On")]
        public DateTime? ApprovedOn { get; set; }
        public virtual ICollection<Change>? Changes { get; set; }
        public virtual ICollection<Deviation>? Deviation { get; set; }
        public virtual ICollection<Capa>? Capa { get; set; }
        public ProductStatus Status { get; set; }
        public enum ProductStatus
        {
            Draft,
            Approved
        }
    }
}
