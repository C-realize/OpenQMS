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
using static OpenQMS.Models.Capa;
using static OpenQMS.Models.Product;

namespace OpenQMS.Controllers
{
    public class CapasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public CapasController
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

        // GET: Capas
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Capa.Include(x => x.Deviation).Include(c => c.Product);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Capas/Details/5
        public async Task<IActionResult> Details(int? id, bool isUserVerified = true)
        {
            if (id == null || _context.Capa == null)
            {
                return NotFound();
            }

            var capa = await _context.Capa
                .Include(d => d.Deviation)
                .Include(c => c.Product)
                .Include(c => c.Changes)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (capa == null)
            {
                return NotFound();
            }

            if (!isUserVerified)
            {
                ModelState.AddModelError("", "Username or password is not correct");
            }

            return View(capa);
        }

        // GET: Capas/Create
        public IActionResult Create()
        {
            ViewData["Products"] = _context.Product.ToList();
            ViewData["Deviations"] = _context.Deviation.ToList();
            return View();
        }

        // POST: Capas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,ProductId,DeviationId,CorrectiveAction,PreventiveAction,DeterminedBy,DeterminedOn,ChangeId,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,ImplementedBy,ImplementedOn,ApprovedBy,ApprovedOn,Status")] Capa capa)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    capa.DeterminedBy = _userManager.GetUserName(User);
                    capa.DeterminedOn = DateTime.Now;
                    capa.Status = Capa.CapaStatus.Determination;
                    _context.Add(capa);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(capa);
        }

        // GET: Capas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Capa == null)
            {
                return NotFound();
            }

            var capa = await _context.Capa.FindAsync(id);
            if (capa == null)
            {
                return NotFound();
            }

            ViewData["Products"] = _context.Product.ToList();
            ViewData["Deviations"] = _context.Deviation.ToList();
            return View(capa);
        }

        // POST: Capas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ProductId,DeviationId,CorrectiveAction,PreventiveAction,DeterminedBy,DeterminedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,Status")] Capa capa)
        {
            if (id != capa.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (capa.Status == Capa.CapaStatus.Determination || capa.Status == Capa.CapaStatus.Assessment)
                    {
                        capa.AssessedBy = _userManager.GetUserName(User);
                        capa.AssessedOn = DateTime.Now;
                        capa.Status = Capa.CapaStatus.Assessment;
                    }
                    else if (capa.Status == Capa.CapaStatus.Accepted || capa.Status == Capa.CapaStatus.Implementation)
                    {
                        capa.ImplementedBy = _userManager.GetUserName(User);
                        capa.ImplementedOn = DateTime.Now;
                        capa.Status = Capa.CapaStatus.Implementation;
                    }

                    _context.Update(capa);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CapaExists(capa.Id))
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
            return View(capa);
        }

        // GET: Capas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Capa == null)
            {
                return NotFound();
            }

            var capa = await _context.Capa
                .Include(c => c.Deviation)
                .Include(c => c.Product)
                .Include(c => c.Changes)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (capa == null)
            {
                return NotFound();
            }

            return View(capa);
        }

        // POST: Capas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Capa == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Capa'  is null.");
            }

            var capa = await _context.Capa
                           .Include(c => c.Deviation)
                           .Include(c => c.Product)
                           .Include(c => c.Changes)
                           .FirstOrDefaultAsync(m => m.Id == id); if (capa != null)
            {
                _context.Capa.Remove(capa);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Accepted(int id, string email, string pwd)
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
                        var capa = _context.Capa.FirstOrDefault(x => x.Id == id);
                        capa.Status = CapaStatus.Accepted;

                        capa.AcceptedBy = _userManager.GetUserName(User);
                        capa.AcceptedOn = DateTime.Now;
                        _context.Entry(capa).State = EntityState.Modified;
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
                return Redirect("/CAPAS/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString()); ;
            }
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
                        var capa = _context.Capa.FirstOrDefault(x => x.Id == id);
                        capa.Status = CapaStatus.Approved;
                        capa.ApprovedBy = _userManager.GetUserName(User);
                        capa.ApprovedOn = DateTime.Now;
                        _context.Entry(capa).State = EntityState.Modified;
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
                return Redirect("/CAPAS/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString());
            }
        }

        private bool CapaExists(int id)
        {
            return (_context.Capa?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
