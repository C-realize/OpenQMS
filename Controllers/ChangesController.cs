using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;

namespace OpenQMS.Controllers
{
    public class ChangesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public ChangesController
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

        // GET: Changes
        public async Task<IActionResult> Index()
        {
            return _context.Change != null ?
                View(await _context.Change.Include(x => x.Product).Include(x => x.Asset).ToListAsync()) :
                Problem("Entity set 'ApplicationDbContext.Change'  is null.");
        }

        // GET: Changes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Change == null)
            {
                return NotFound();
            }

            var change = await _context.Change.Include(a => a.Product).Include(a => a.Asset).Include(x => x.Capa)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (change == null)
            {
                return NotFound();
            }

            return View(change);
        }

        // POST: Changes/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, [Bind("Id,ProductId,AssetId,CapaId,Title,Proposal,ProposedBy,ProposedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,ImplementedBy,ImplementedOn,Status")] Change change, string InputEmail, string InputPassword)
        {
            if (id != change.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var isAuthorized = User.IsInRole(Constants.ManagersRole)
                            || User.IsInRole(Constants.AdministratorsRole);
                    if (!isAuthorized)
                    {
                        return Forbid();
                    }

                    var userSigning = await _userManager.FindByEmailAsync(InputEmail);
                    var loggedUser = _userManager.GetUserName(User);
                    if (userSigning.UserName != loggedUser)
                    {
                        return Forbid();
                    }

                    var result = await _signInManager.PasswordSignInAsync(userSigning.UserName, InputPassword, isPersistent: false, lockoutOnFailure: false);
                    if (!result.Succeeded)
                    {
                        return Forbid();
                    }

                    if (change.Status == Change.ChangeStatus.Assessment)
                    {
                        change.AcceptedBy = _userManager.GetUserName(User);
                        change.AcceptedOn = DateTime.Now;
                        change.Status = Change.ChangeStatus.Accepted;
                    }

                    if (change.Status == Change.ChangeStatus.Implementation)
                    {
                        change.ApprovedBy = _userManager.GetUserName(User);
                        change.ApprovedOn = DateTime.Now;
                        change.Status = Change.ChangeStatus.Approved;
                    }

                    _context.Change.Update(change);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChangeExists(change.Id))
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
            return View(change);
        }

        // GET: Changes/Create
        public IActionResult Create()
        {
            ViewData["Products"] = _context.Product.ToList();
            ViewData["Assets"] = _context.Asset.ToList();
            ViewData["Capa"] = _context.Capa.ToList();

            return View();
        }

        // POST: Changes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Proposal,ProposedBy,ProductId,AssetId,CapaId")] Change change)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //change.AssessedBy = _userManager.GetUserName(User);
                    change.ProposedBy = _userManager.GetUserName(User);
                    change.ProposedOn = DateTime.Now;
                    change.Status = Change.ChangeStatus.Proposal;
                    _context.Add(change);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(change);
        }

        // GET: Changes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Change == null)
            {
                return NotFound();
            }

            var change = await _context.Change.FindAsync(id);
            if (change == null)
            {
                return NotFound();
            }

            ViewData["Products"] = _context.Product.ToList();
            ViewData["Assets"] = _context.Asset.ToList();
            ViewData["Capa"] = _context.Capa.ToList();
            return View(change);
        }

        // POST: Changes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Proposal,ProposedBy,ProposedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,Status,ProductId,AssetId,CapaId")] Change change)
        {
            if (id != change.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (change.Status == Change.ChangeStatus.Proposal || change.Status == Change.ChangeStatus.Assessment)
                    {
                        change.AssessedBy = _userManager.GetUserName(User);
                        change.AssessedOn = DateTime.Now;
                        change.Status = Change.ChangeStatus.Assessment;
                    }
                    if (change.Status == Change.ChangeStatus.Accepted || change.Status == Change.ChangeStatus.Implementation)
                    {
                        change.ImplementedBy = _userManager.GetUserName(User);
                        change.ImplementedOn = DateTime.Now;
                        change.Status = Change.ChangeStatus.Implementation;
                    }
                    _context.Update(change);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChangeExists(change.Id))
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
            return View(change);
        }

        // GET: Changes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Change == null)
            {
                return NotFound();
            }

            var change = await _context.Change
                .Include(a => a.Product)
                .Include(a => a.Asset)
                .Include(x => x.Capa)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (change == null)
            {
                return NotFound();
            }

            return View(change);
        }

        // POST: Changes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Change == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Change'  is null.");
            }
            var change = await _context.Change
                            .Include(a => a.Product)
                            .Include(a => a.Asset)
                            .Include(x => x.Capa)
                            .FirstOrDefaultAsync(m => m.Id == id); if (change != null)
            {
                _context.Change.Remove(change);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChangeExists(int id)
        {
            return (_context.Change?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
