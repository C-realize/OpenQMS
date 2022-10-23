using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Reporting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;

namespace OpenQMS.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public ProductsController(ApplicationDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Products
        public async Task<IActionResult> Index(string sortOrder, string searchString)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            ViewData["CurrentFilter"] = searchString;

            var applicationDbContext = _context.Product.Include(x => x.EditedByUser).Include(a => a.ApprovedByUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Product == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(a => a.EditedByUser)
                .Include(a => a.ApprovedByUser)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, [Bind("Id,Name,Description,Version,EditedBy,EditedOn,Status")] Product product, string InputEmail, string InputPassword)
        {
            if (id != product.Id)
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

                    if (product.Status == Product.ProductStatus.Draft)
                    {
                        product.Version = Decimal.ToInt32(product.Version) + 1;
                        product.ApprovedBy = Convert.ToInt32(_userManager.GetUserId(User));
                        product.ApprovedOn = DateTime.Now;
                        product.Status = Product.ProductStatus.Approved;
                    }

                    _context.Product.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Product product)
        {
            try
            {
                if(ModelState.IsValid)
                {
                    product.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    product.EditedOn = DateTime.Now;
                    product.Version = Convert.ToDecimal(0.01);
                    product.Changes = new List<Change>();
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Product == null)
            {
                return NotFound();
            }

            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,Status")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    product.EditedOn = DateTime.Now;
                    product.Version += Convert.ToDecimal(0.01);
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Product == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .Include(p => p.EditedByUser)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Product == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Product' is null.");
            }

            var product = await _context.Product
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product != null)
            {
                _context.Product.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportProducts()
        {
            string mimType = "";
            int extension = 1;
            string path = Directory.GetCurrentDirectory() + "\\Reports\\ProductsReport.rdlc";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            var products = await _context.Product.Include(x => x.EditedByUser).Include(a => a.ApprovedByUser).ToListAsync();
            LocalReport localReport = new LocalReport(path);
            localReport.AddDataSource("DataSet1", products);
            var result = localReport.Execute(RenderType.Excel, extension, parameters, mimType);
            return File(result.MainStream, "application/vnd.ms-excel");
        }

        private bool ProductExists(int id)
        {
            return (_context.Product?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
