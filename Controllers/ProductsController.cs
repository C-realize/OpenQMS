using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;
using Microsoft.ReportingServices.Interfaces;
using NuGet.ContentModel;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
using OpenQMS.Services;
using Org.BouncyCastle.Asn1.X509;

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
                //Should be Deviations
                .Include(a => a.Deviation)
                .Include(a => a.Capa)
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
        public async Task<IActionResult> Details(int id, string InputEmail, string InputPassword)
        {
            var product = _context.Product.FirstOrDefault(x => x.Id == id);

            try
            {
                var isAuthorized = User.IsInRole(Constants.ManagersRole) || User.IsInRole(Constants.AdministratorsRole);
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
                    product.ExportFilePath = String.Empty;

                    if (product.GeneratedFrom != null && !string.IsNullOrEmpty(product.GeneratedFrom))
                    {
                        var oldProduct = await _context.Product.FirstOrDefaultAsync(m => m.Id.ToString().Equals(product.GeneratedFrom));

                        oldProduct.Status = Product.ProductStatus.Obsolete;
                        product.ExportFilePath = String.Empty;
                        _context.Product.Update(oldProduct);
                    }
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
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description")] Product product)
        {
            try
            {
                if(ModelState.IsValid)
                {
                    var lastProduct = _context.Product.OrderByDescending(x => x.ProductId).FirstOrDefault();
                    var prevId = 0;

                    if (lastProduct != null && !string.IsNullOrEmpty(lastProduct.ProductId))
                    {
                        string[] prevProductId = lastProduct.ProductId.Split('-');
                        prevId = int.Parse(prevProductId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    product.ProductId = $"PRD-{prevId.ToString().PadLeft(2, '0')}";
                    product.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    product.EditedOn = DateTime.Now;
                    product.Version = Convert.ToDecimal(0.01);
                    product.IsLocked = false;

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductId,GeneratedFrom,Name,Description,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,IsLocked,Status")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.IsLocked)
                    {
                        return Forbid();
                    }

                    product.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    product.EditedOn = DateTime.Now;
                    product.Version += Convert.ToDecimal(0.01);
                    product.ExportFilePath = string.Empty;

                    if (product.Status == Product.ProductStatus.Approved)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Product ON;");
                            product.Id = _context.Product.Max(m => m.Id) + 1;
                            product.Status = Product.ProductStatus.Draft;
                            product.ApprovedBy = null;
                            product.ApprovedOn = null;
                            product.GeneratedFrom = id.ToString();
                            product.IsLocked = false;
                            _context.Add(product);
                            _context.SaveChanges();
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Product OFF;");
                            transaction.Commit();
                        }

                        Product product1 = await _context.Product.FirstOrDefaultAsync(m => m.Id == id);
                        product1.IsLocked = true;
                        _context.Update(product1);
                        _context.SaveChanges();
                    }
                    else
                    {
                        product.Status = Product.ProductStatus.Draft;
                        _context.Update(product);
                        await _context.SaveChangesAsync();
                    }
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
                .Include(p => p.Deviation)
                .Include(p => p.Capa)
                .Include(p => p.EditedByUser)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Status != Product.ProductStatus.Draft)
            {
                return Forbid();
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
                .Include(p => p.Deviation)
                .Include(p => p.Capa)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product.Status != Product.ProductStatus.Draft)
            {
                return Forbid();
            }

            if (product != null)
            {
                _context.Product.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public ActionResult GetDocuments()
        {
            var products = _context.Product.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(Product.ProductStatus)).Cast<Product.ProductStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (products != null && products.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = products.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public ActionResult GetDocumentsForBarchart()
        {
            var products = _context.Product.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();
            var dates = products.Select(x => x.EditedOn.Date).Distinct().ToList();

            if (products != null && products.Count > 0)
            {
                foreach (var date in dates)
                {
                    labels.Add(date.ToShortDateString());
                    var productCount = products.Where(x => x.EditedOn.Date == date).Count();
                    dataSet.Data.Add(productCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        //public async Task<IActionResult> ExportProducts()
        //{
        //    string mimType = "";
        //    int extension = 1;
        //    string path = Directory.GetCurrentDirectory() + "\\Reports\\ProductsReport.rdlc";
        //    Dictionary<string, string> parameters = new Dictionary<string, string>();
        //    var products = await _context.Product.Include(x => x.EditedByUser).Include(a => a.ApprovedByUser).ToListAsync();
        //    AspNetCore.Reporting.LocalReport localReport = new AspNetCore.Reporting.LocalReport(path);
        //    localReport.AddDataSource("DataSet1", products);
        //    var result = localReport.Execute(AspNetCore.Reporting.RenderType.Excel, extension, parameters, mimType);
        //    return File(result.MainStream, "application/vnd.ms-excel");
        //}

        public async Task<IActionResult> ExportProductDetail(int id)
        {
            string mimType = "";
            string path = Directory.GetCurrentDirectory() + "\\Reports\\ProductDetailReport.rdlc";
            var product = await _context.Product
                .Where(x => x.Id == id)
                .Include(a => a.Processes)
                .Include(a => a.Changes)
                .Include(a => a.Deviation)
                .Include(a => a.Capa)
                .Include(a => a.EditedByUser)
                .Include(a => a.ApprovedByUser)
                .FirstOrDefaultAsync();

            if (product != null && string.IsNullOrEmpty(product.ExportFilePath))
            {
                var processList = new List<Process>();
                processList = product.Processes != null ? product.Processes.ToList() : processList;
                var changeList = new List<Change>();
                changeList = product.Changes != null ? product.Changes.ToList() : changeList;
                var deviationList = new List<Deviation>();
                deviationList = product.Deviation != null ? product.Deviation.ToList() : deviationList;
                var capaList = new List<Capa>();
                capaList = product.Capa != null ? product.Capa.ToList() : capaList;
                List<ReportParameter> parameters1 = new List<ReportParameter>();
                //Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters1.Add(new ReportParameter("ProductId", product.ProductId));
                parameters1.Add(new ReportParameter("Name", product.Name));
                parameters1.Add(new ReportParameter("Description", product.Description));
                parameters1.Add(new ReportParameter("ProcessCount", processList != null && processList.Count > 0 ? "false" : "true"));
                parameters1.Add(new ReportParameter("ChangeCount", changeList != null && changeList.Count > 0 ? "false" : "true"));
                parameters1.Add(new ReportParameter("DeviationCount", deviationList != null && deviationList.Count > 0 ? "false" : "true"));
                parameters1.Add(new ReportParameter("CapaCount", capaList != null && capaList.Count > 0 ? "false" : "true"));
                parameters1.Add(new ReportParameter("Status", product.Status.ToString()));
                parameters1.Add(new ReportParameter("EditedBy", product.EditedByUser != null ? product.EditedByUser.FirstName : string.Empty));
                parameters1.Add(new ReportParameter("EditedOn", product.EditedOn != null ? product.EditedOn.ToShortDateString() : string.Empty));
                parameters1.Add(new ReportParameter("ApprovedBy", product.ApprovedByUser != null ? product.ApprovedByUser.FirstName : string.Empty));
                parameters1.Add(new ReportParameter("ApprovedOn", product.ApprovedOn != null ? product.ApprovedOn.Value.ToShortDateString() : string.Empty));
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Process", processList));
                localReport.DataSources.Add(new ReportDataSource("Change", changeList));
                localReport.DataSources.Add(new ReportDataSource("Deviation", deviationList));
                localReport.DataSources.Add(new ReportDataSource("CAPA", capaList));
                localReport.SetParameters(parameters1);
                //localReport.AddDataSource("CAPA", capaList);
                //localReport.AddDataSource("Change", changeList);
                //localReport.AddDataSource("Deviation", deviationList);
                //int ext = (int)(DateTime.Now.Ticks >> 10);
                try
                {
                    var result = localReport.Render("PDF");

                    var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                    bool basePathExists = System.IO.Directory.Exists(basePath);
                    if (!basePathExists) Directory.CreateDirectory(basePath);
                    var filePath = Path.Combine(basePath, $"ProductDetail-{product.Id}.pdf");

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    if (!System.IO.File.Exists(filePath))
                    {
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await stream.WriteAsync(result);
                        }
                    }

                    product.ExportFilePath = filePath;

                    if (product.Status == Product.ProductStatus.Approved || product.Status == Product.ProductStatus.Obsolete)
                    {
                        product.ExportFilePath = Helper.SignPDF(product.ExportFilePath);
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {

                }
                
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(product.ExportFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", "ProductDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Product product = _context.Product.Where(x => x.Id == id).Include(x => x.EditedByUser).Include(x => x.ApprovedByUser).FirstOrDefault();

            var builder = new StringBuilder();
            builder.AppendLine("Product Id,Name,Description,Version,Status,Edited By,Edited On,Approved By,Approved On");
            builder.AppendLine($"{product.ProductId},{product.Name},{product.Description},{product.Version},{product.Status},{(product.EditedByUser != null ? product.EditedByUser.FirstName : String.Empty)},{(product.EditedByUser != null ? product.EditedOn.ToShortDateString() : String.Empty)},{(product.ApprovedByUser != null ? product.ApprovedByUser.FirstName : String.Empty)},{(product.ApprovedByUser != null ? product.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Product.csv");
        }

        private bool ProductExists(int id)
        {
            return (_context.Product?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
