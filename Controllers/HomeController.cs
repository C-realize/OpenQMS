﻿using System.Diagnostics;
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