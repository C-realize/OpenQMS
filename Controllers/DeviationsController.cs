using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Data;
using OpenQMS.Models;
using static OpenQMS.Models.Deviation;

namespace OpenQMS.Controllers
{
    public class DeviationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public DeviationsController
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

        // GET: Deviations
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Deviation.Include(d => d.Product).Include(d => d.Asset);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Deviations/Details/5
        public async Task<IActionResult> Details(int? id, bool isUserVerified = true)
        {
            if (id == null || _context.Deviation == null)
            {
                return NotFound();
            }

            var deviation = await _context.Deviation
                .Include(d => d.Capas)
                .Include(d => d.Product)
                .Include(d => d.Asset)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deviation == null)
            {
                return NotFound();
            }

            if (!isUserVerified)
            {
                ModelState.AddModelError("", "Username or password is incorrect");
            }

            return View(deviation);
        }

        // GET: Deviations/Create
        public IActionResult Create()
        {
            ViewData["Products"] = _context.Product.ToList();
            ViewData["Assets"] = _context.Asset.ToList();
            return View();
        }

        // POST: Deviations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ProductId,AssetId,Identification,IdentifiedBy")] Deviation deviation)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    deviation.IdentifiedBy = _userManager.GetUserName(User);
                    deviation.IdentifiedOn = DateTime.Now;
                    _context.Add(deviation);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(deviation);
        }

        // GET: Deviations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Deviation == null)
            {
                return NotFound();
            }

            var deviation = await _context.Deviation.FindAsync(id);
            if (deviation == null)
            {
                return NotFound();
            }

            ViewData["Products"] = _context.Product.ToList();
            ViewData["Assets"] = _context.Asset.ToList();
            return View(deviation);
        }

        // POST: Deviations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ProductId,AssetId,Identification,IdentifiedBy,IdentifiedOn,Evaluation,EvaluatedBy,EvaluatedOn,AcceptedBy,AcceptedOn,Resolution,Status")] Deviation deviation)
        {
            if (id != deviation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (deviation.Status == Deviation.DeviationStatus.Identification || deviation.Status == Deviation.DeviationStatus.Evaluation)
                    {
                        deviation.EvaluatedBy = _userManager.GetUserName(User);
                        deviation.EvaluatedOn = DateTime.Now;
                        deviation.Status = Deviation.DeviationStatus.Evaluation;
                    }
                    else if (deviation.Status == Deviation.DeviationStatus.Accepted || deviation.Status == Deviation.DeviationStatus.Resolution)
                    {
                        deviation.ResolvedBy = _userManager.GetUserName(User);
                        deviation.ResolvedOn = DateTime.Now;
                        deviation.Status = Deviation.DeviationStatus.Resolution;
                    }

                    _context.Update(deviation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviationExists(deviation.Id))
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
            return View(deviation);
        }

        // GET: Deviations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Deviation == null)
            {
                return NotFound();
            }

            var deviation = await _context.Deviation
                .Include(d => d.Capas)
                .Include(d => d.Product)
                .Include(d => d.Asset)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deviation == null)
            {
                return NotFound();
            }

            return View(deviation);
        }

        // POST: Deviations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Deviation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Deviation'  is null.");
            }

            var deviation = await _context.Deviation
                           .Include(d => d.Capas)
                           .Include(d => d.Product)
                           .Include(d => d.Asset)
                           .FirstOrDefaultAsync(m => m.Id == id);
            if (deviation != null)
            {
                _context.Deviation.Remove(deviation);
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
                        var devication = _context.Deviation.FirstOrDefault(x => x.Id == id);
                        devication.Status = DeviationStatus.Accepted;

                        devication.AcceptedBy = _userManager.GetUserName(User);
                        devication.AcceptedOn = DateTime.Now;
                        _context.Entry(devication).State = EntityState.Modified;
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
                return Redirect("/Deviation/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString()); ;
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
                        var devication = _context.Deviation.FirstOrDefault(x => x.Id == id);
                        devication.Status = DeviationStatus.Approved;
                        devication.ApprovedBy = _userManager.GetUserName(User);
                        devication.ApprovedOn = DateTime.Now;
                        _context.Entry(devication).State = EntityState.Modified;
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
                return Redirect("/Deviation/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString());
            }
        }

        private bool DeviationExists(int id)
        {
            return (_context.Deviation?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
