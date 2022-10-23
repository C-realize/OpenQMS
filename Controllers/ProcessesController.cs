using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Data;
using OpenQMS.Migrations;
using OpenQMS.Models;
using static OpenQMS.Models.Process;
using Process = OpenQMS.Models.Process;

namespace OpenQMS.Controllers
{
    public class ProcessesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        public ProcessesController
             (
                 ApplicationDbContext context,
                 UserManager<AppUser> userManager,
                 SignInManager<AppUser> signInManager
             )
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Processes
        public async Task<IActionResult> Index()
        {
            return View(await _context.Process.ToListAsync());
        }

        // GET: Processes/Details/5
        public async Task<IActionResult> Details(int? id, bool isUserVerified = true)
        {
            if (id == null || _context.Process == null)
            {
                return NotFound();
            }

            var process = await _context.Process
                .Include(a => a.Deviations)
                .Include(b => b.Capas)
                .Include(c => c.Changes)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (process == null)
            {
                return NotFound();
            }
            if (!isUserVerified)
            {
                ModelState.AddModelError("", "Username or passowrd is not matched");
            }

            return View(process);
        }

        // GET: Processes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Processes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,Status")] Process process)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    process.EditedBy = _userManager.GetUserName(User);
                    process.EditedOn = DateTime.Now;
                    process.Version = Convert.ToDecimal(0.01);
                    _context.Add(process);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(process);
        }

        // GET: Processes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Process == null)
            {
                return NotFound();
            }

            var process = await _context.Process.FindAsync(id);
            if (process == null)
            {
                return NotFound();
            }

            return View(process);
        }

        // POST: Processes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,Status")] Process process)
        {
            if (id != process.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    process.Version += Convert.ToDecimal(0.01);
                    process.EditedBy = _userManager.GetUserName(User);
                    process.EditedOn = DateTime.Now;
                    _context.Update(process);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProcessExists(process.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(process);
        }

        // GET: Processes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Process == null)
            {
                return NotFound();
            }

            var process = await _context.Process
                .Include(a => a.Deviations)
                .Include(b => b.Capas)
                .Include(c => c.Changes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (process == null)
            {
                return NotFound();
            }

            return View(process);
        }

        // POST: Processes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Process == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Process'  is null.");
            }

            var process = await _context.Process
                .Include(a => a.Deviations)
                .Include(b => b.Capas)
                .Include(c => c.Changes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (process != null)
            {
                _context.Process.Remove(process);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProcessExists(int id)
        {
            return _context.Process.Any(e => e.Id == id);
        }

        public IActionResult Approved(int id, string email, string pwd)
        {
            bool isUserVerified;
            AppUser user = new AppUser();
            user = _context.Users.FirstOrDefault(x => x.Email == email);
            if (user != null)
            {
                if (string.IsNullOrEmpty(pwd))
                {
                    isUserVerified = false;
                }
                else
                {
                    PasswordVerificationResult result =
                        _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, pwd);
                    if (result == PasswordVerificationResult.Success)
                    {
                        var process = _context.Process.FirstOrDefault(x => x.Id == id);
                        process.Version = Decimal.ToInt32(process.Version) + 1;
                        process.Status = ProcessStatus.Approved;
                        process.ApprovedBy = _userManager.GetUserName(User);
                        process.ApprovedOn = DateTime.Now;
                        _context.Entry(process).State = EntityState.Modified;
                        _context.SaveChanges();
                        isUserVerified = true;
                    }
                    else
                    {
                        isUserVerified = false;
                    }
                }
            }
            else
            {
                isUserVerified = false;
            }
            if (isUserVerified)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return Redirect("/Processes/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString());
            }
        }

    }
}
