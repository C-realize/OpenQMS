/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

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

using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
//using OpenQMS.Services;
using static OpenQMS.Models.Product;

namespace OpenQMS.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        public ProductsController
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

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var productsList = await _context.Product.ToListAsync();
            return View(productsList);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Product == null)
            {
                return NotFound();
            }

            var product = await _context.Product
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
                if ((userSigning == null) || (userSigning.UserName != loggedUser))
                {
                    return Forbid();
                }

                var result = await _signInManager.PasswordSignInAsync(userSigning.UserName, InputPassword, isPersistent: false, lockoutOnFailure: false);
                if (!(result.Succeeded || result.RequiresTwoFactor))
                {
                    return Forbid();
                }

                if (product.Status == ProductStatus.Draft)
                {
                    product.Version = Decimal.ToInt32(product.Version) + 1;
                    product.ApprovedBy = _userManager.GetUserName(User);
                    product.ApprovedOn = DateTime.Now;
                    product.Status = ProductStatus.Approved;
                    product.ExportFilePath = String.Empty;

                    if (product.GeneratedFrom != null && !string.IsNullOrEmpty(product.GeneratedFrom))
                    {
                        var oldProduct = await _context.Product.FirstOrDefaultAsync(m => m.Id.ToString().Equals(product.GeneratedFrom));

                        oldProduct.Status = ProductStatus.Obsolete;
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
                    product.EditedBy = _userManager.GetUserName(User);
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

            if (product.IsLocked)
            {
                return Forbid();
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

                    product.Version += Convert.ToDecimal(0.01);
                    product.EditedBy = _userManager.GetUserName(User);
                    product.EditedOn = DateTime.Now;
                    product.ExportFilePath = string.Empty;

                    if (product.Status == ProductStatus.Approved)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Product ON;");
                            product.Id = _context.Product.Max(m => m.Id) + 1;
                            product.Status = ProductStatus.Draft;
                            product.ApprovedBy = null;
                            product.ApprovedOn = null;
                            product.GeneratedFrom = id.ToString();
                            product.IsLocked = false;
                            _context.Add(product);
                            _context.SaveChanges();
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Product OFF;");
                            transaction.Commit();
                        }

                        Product? product1 = await _context.Product.FirstOrDefaultAsync(m => m.Id == id);
                        product1.IsLocked = true;
                        _context.Update(product1);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        product.Status = ProductStatus.Draft;
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
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Status != ProductStatus.Draft)
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
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product != null)
            {
                _context.Product.Remove(product);
            }

            if (product.Status != ProductStatus.Draft)
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public ActionResult GetDocuments()
        {
            var products = _context.Product.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>();
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
            string path = Directory.GetCurrentDirectory() + "/Reports/ProductDetailReport.rdlc";
            var product = await _context.Product
                .Where(x => x.Id == id)
                .Include(a => a.Processes)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync();

            if (product != null /*&& string.IsNullOrEmpty(product.ExportFilePath)*/)
            {
                var processList = new List<Models.Process>();
                processList = product.Processes != null ? product.Processes.ToList() : processList;
                var changeList = new List<Change>();
                changeList = product.Changes != null ? product.Changes.ToList() : changeList;
                var deviationList = new List<Deviation>();
                deviationList = product.Deviations != null ? product.Deviations.ToList() : deviationList;
                var capaList = new List<Capa>();
                capaList = product.Capas != null ? product.Capas.ToList() : capaList;
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("ProductId", product.ProductId));
                parameters.Add(new ReportParameter("Name", product.Name));
                parameters.Add(new ReportParameter("Description", product.Description));
                parameters.Add(new ReportParameter("ProcessCount", processList != null && processList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("ChangeCount", changeList != null && changeList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("DeviationCount", deviationList != null && deviationList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("CapaCount", capaList != null && capaList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("Status", product.Status.ToString()));
                parameters.Add(new ReportParameter("EditedBy", product.EditedBy != null ? product.EditedBy : string.Empty));
                parameters.Add(new ReportParameter("EditedOn", product.EditedOn.ToShortDateString()));
                parameters.Add(new ReportParameter("ApprovedBy", product.ApprovedBy != null ? product.ApprovedBy : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", product.ApprovedOn != null ? product.ApprovedOn.Value.ToShortDateString() : string.Empty));
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Process", processList));
                localReport.DataSources.Add(new ReportDataSource("Change", changeList));
                localReport.DataSources.Add(new ReportDataSource("Deviation", deviationList));
                localReport.DataSources.Add(new ReportDataSource("CAPA", capaList));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                //**Export details as signed pdf**
                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "/Files/");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var filePath = Path.Combine(basePath, $"ProductDetail-{product.Id}.pdf");
                //if (System.IO.File.Exists(filePath))
                //{
                //    System.IO.File.Delete(filePath);
                //}
                //if (!System.IO.File.Exists(filePath))
                //{
                //    using (var stream = new FileStream(filePath, FileMode.Create))
                //    {
                //        await stream.WriteAsync(result);
                //    }
                //}
                product.ExportFilePath = filePath;
                //if (product.Status == ProductStatus.Approved || product.Status == ProductStatus.Obsolete)
                //{
                //    product.ExportFilePath = Helper.SignPDF(product.ExportFilePath);
                //}

                _context.Update(product);
                await _context.SaveChangesAsync();

                return File(result, "application/pdf", "ProductDetail.pdf");
            }
            else
            {
                return NotFound();
            }

            //var memory = new MemoryStream();
            //using (var stream = new FileStream(product.ExportFilePath, FileMode.Open))
            //{
            //    await stream.CopyToAsync(memory);
            //}
            //memory.Position = 0;

            //return File(memory, "application/pdf", "ProductDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Product? product = _context.Product.Where(x => x.Id == id).FirstOrDefault();

            if (product == null)
            {
                return NotFound();
            }

            var builder = new StringBuilder();
            builder.AppendLine("Product Id,Name,Description,Version,Status,Edited By,Edited On,Approved By,Approved On");
            builder.AppendLine($"{product.ProductId},{product.Name},{product.Description},{product.Version},{product.Status},{(product.EditedBy != null ? product.EditedBy : String.Empty)},{product.EditedOn.ToShortDateString()},{(product.ApprovedBy != null ? product.ApprovedBy : String.Empty)},{(product.ApprovedOn != null ? product.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Product.csv");
        }

        private bool ProductExists(int id)
        {
            return (_context.Product?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
