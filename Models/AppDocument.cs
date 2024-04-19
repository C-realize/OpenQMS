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

using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models
{
    public class AppDocument
    {
        public int Id { get; set; }
        public string DocumentId { get; set; }
        public string? GeneratedFrom { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Version { get; set; }
        public byte[]? Content { get; set; }
        public string? FilePath { get; set; }
        public string FileType { get; set; }
        public string FileExtension { get; set; }
        public string AuthoredBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime AuthoredOn { get; set; }
        public string? ApprovedBy { get; set; }
        [DataType(DataType.Date)]
        public DateTime? ApprovedOn { get; set; }
        public bool IsLocked { get; set; }
        public string? ExportFilePath { get; set; }
        public DocumentStatus Status { get; set; }
        public enum DocumentStatus
        {
            Draft,
            Approved,
            Rejected,
            Obsolete
        }
    }
}
