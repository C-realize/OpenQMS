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

namespace OpenQMS.Models.ViewModels
{
    public class PoliciesChartViewModel
    {
        public PoliciesChartViewModel()
        {
            this.Data = new List<double>();
            this.BackgroundColor = new List<string>();
            this.Fill = false;
        }

        public string? Label { get; set; }
        public List<string> BackgroundColor { get; set; }
        public string? BorderColor { get; set; }
        public bool Fill { get; set; }
        public int BarThickness { get; set; }
        public List<double> Data { get; set; }
    }
}
