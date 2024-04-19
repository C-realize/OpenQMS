/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models
{
    public class Asset
    {
        public Asset()
        {
            this.Changes = new HashSet<Change>();
        }

        public int Id { get; set; }
        public string AssetId { get; set; }
        public string? GeneratedFrom { get; set; }
        [Display(Name = "Asset Name")]
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Version { get; set; }
        [Display(Name = "Edited By")]
        public string? EditedBy { get; set; }
        [Display(Name = "Edited On")]
        public DateTime EditedOn { get; set; }
        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }
        [Display(Name = "Approved On")]
        public DateTime? ApprovedOn { get; set; }
        public bool IsLocked { get; set; }
        public string? ExportFilePath { get; set; }
        public int? ProcessId { get; set; }
        [ForeignKey("ProcessId")]
        public virtual Process? Process { get; set; }
        public virtual ICollection<Change>? Changes { get; set; }
        public virtual ICollection<Deviation>? Deviations { get; set; }
        public virtual ICollection<Capa>? Capas { get; set; }
        public AssetStatus Status { get; set; }
        public enum AssetStatus
        {
            Draft,
            Approved,
            Obsolete
        }
    }
}
