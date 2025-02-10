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

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Data;
using OpenQMS.Models.ViewModels;

namespace OpenQMS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult UnderConstruction()
        {
            return View();
        }

        public IActionResult UpgradePlan()
        {
            return View();
        }

        public async Task<ActionResult> Dashboard()
        {
            IQueryable<AuthoredOnGroup> data =
                from document in _context.AppDocument
                group document by document.AuthoredOn.Date into dateGroup
                select new AuthoredOnGroup()
                {
                    AuthoredOn = dateGroup.Key,
                    DocumentCount = dateGroup.Count()
                };

            return View(await data.AsNoTracking().ToListAsync());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}