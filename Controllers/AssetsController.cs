using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;

namespace OpenQMS.Controllers
{
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AssetsController(ApplicationDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Assets
        public async Task<IActionResult> Index(string sortOrder, string searchString)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString;

            var applicationDbContext = _context.Asset.Include(x => x.EditedByUser).Include(a => a.ApprovedByUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Assets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Asset asset)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    asset.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    asset.EditedOn = DateTime.Now;
                    asset.Version = Convert.ToDecimal(0.01);
                    asset.Changes = new List<Change>();
                    _context.Add(asset);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(asset);
        }

        // GET: Assets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Asset == null)
            {
                return NotFound();
            }

            var asset = await _context.Asset.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // POST: Assets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,Status")] Asset Asset)
        {
            if (id != Asset.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Asset.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    Asset.EditedOn = DateTime.Now;
                    Asset.Status = Asset.AssetStatus.Draft;
                    Asset.Version += Convert.ToDecimal(0.01);
                    _context.Update(Asset);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssetExists(Asset.Id))
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

            return View(Asset);
        }

        // GET: Assets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Asset == null)
            {
                return NotFound();
            }

            var asset = await _context.Asset
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .Include(p => p.EditedByUser)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // POST: Assets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Asset == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Asset' is null.");
            }

            var asset = await _context.Asset
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset != null)
            {
                _context.Asset.Remove(asset);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Assets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Asset == null)
            {
                return NotFound();
            }

            var asset = await _context.Asset
                .Include(a => a.EditedByUser)
                .Include(a => a.ApprovedByUser)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // POST: Assets/Details/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, [Bind("Id,Name,Description,Version,EditedBy,EditedOn,Status")] Asset asset, string InputEmail, string InputPassword)
        {
            if (id != asset.Id)
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

                    if (asset.Status == Asset.AssetStatus.Draft)
                    {
                        asset.Version = Decimal.ToInt32(asset.Version) + 1;
                        asset.ApprovedBy = Convert.ToInt32(_userManager.GetUserId(User));
                        asset.ApprovedOn = DateTime.Now;
                        asset.Status = Asset.AssetStatus.Approved;
                    }

                    _context.Asset.Update(asset);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssetExists(asset.Id))
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
            return View(asset);
        }

        private bool AssetExists(int id)
        {
            return (_context.Asset?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
