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

using OpenQMS.Models.Navigation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Training
    {
        public int Id { get; set; }
        [Display(Name = "ID")]
        public string TrainingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; }
        public string TrainerId { get; set; }
        public string TrainerEmail { get; set; }
        public ICollection<UserTraining> Trainees { get; set; }
        [Display(Name = "Completed By")]
        public int? CompletedBy { get; set; }
        [ForeignKey("CompletedBy")]
        public virtual AppUser? CompletedByUser { get; set; }
        [Display(Name = "Completed On")]
        public DateTime? CompletedOn { get; set; }
        public TrainingStatus Status { get; set; }
        public enum TrainingStatus
        {
            Scheduled,
            Completed
        }
    }
}
