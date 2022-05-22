using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Models;

namespace OpenQMS.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AppRolesController : Controller
    {
        private readonly RoleManager<AppRole> _roleManager;
        public AppRolesController(RoleManager<AppRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // GET: RolesController
        public async Task<IActionResult> Index()
        {
            return View(await _roleManager.Roles.ToListAsync());
        }

        // GET: RolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName, string description)
        {
            try
            {
                if (roleName != null)
                {
                    await _roleManager.CreateAsync
                    (
                        new AppRole 
                        { 
                            Name = roleName.Trim(),
                            Description = description
                        }
                    );
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
