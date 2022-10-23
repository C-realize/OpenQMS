﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Asset
    {
        public Asset()
        {
            this.Changes = new HashSet<Change>();
        }
        public int Id { get; set; }
        [Display(Name = "Asset Name")]
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
        public virtual ICollection<Deviation>? Deviations { get; set; }
        public virtual ICollection<Capa>? Capas { get; set; }
        public AssetStatus Status { get; set; }
        public enum AssetStatus
        {
            Draft,
            Approved
        }
    }
}
