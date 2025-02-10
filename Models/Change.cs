/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2025  C-realize IT Services SRL (https://www.c-realize.com)

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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Change
    {
        public int Id { get; set; }
        [Display(Name = "ID")]
        public string ChangeId { get; set; }
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
        public int? CapaId { get; set; }
        [ForeignKey("CapaId")]
        public virtual Capa? Capa { get; set; }
        public string Title { get; set; }
        public string Proposal { get; set; }
        [Display(Name = "Proposed By")]
        public string ProposedBy { get; set; }
        [Display(Name = "Proposed On")]
        //[DataType(DataType.Date)]
        public DateTime ProposedOn { get; set; }
        public string? Assessment { get; set; }
        [Display(Name = "Assessed By")]
        public string? AssessedBy { get; set; }
        [Display(Name = "Assessed On")]
        //[DataType(DataType.Date)]
        public DateTime? AssessedOn { get; set; }
        [Display(Name = "Accepted By")]
        public string? AcceptedBy { get; set; }
        [Display(Name = "Accepted On")]
        //[DataType(DataType.Date)]
        public DateTime? AcceptedOn { get; set; }
        public string? Implementation { get; set; }
        [Display(Name = "Implemented By")]
        public string? ImplementedBy { get; set; }
        [Display(Name = "Implemented On")]
        //[DataType(DataType.Date)]
        public DateTime? ImplementedOn { get; set; }
        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }
        [Display(Name = "Approved On")]
        //[DataType(DataType.Date)]
        public DateTime? ApprovedOn { get; set; }
        public string? ExportFilePath { get; set; }
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
