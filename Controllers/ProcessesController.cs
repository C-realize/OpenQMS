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
using static OpenQMS.Models.Process;
using static OpenQMS.Models.Product;

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
            var processList = await _context.Process.Include(c => c.Product).ToListAsync();
            return View(processList);
        }

        // GET: Processes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Process == null)
            {
                return NotFound();
            }

            var process = await _context.Process
                .Include(a => a.Product)
                .Include(a => a.Assets)
                .Include(a => a.Materials)
                .Include(a => a.Deviations)
                .Include(a => a.Changes)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (process == null)
            {
                return NotFound();
            }

            return View(process);
        }

        // POST: Processes/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, string InputEmail, string InputPassword)
        {
            var process = _context.Process.FirstOrDefault(x => x.Id == id);

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
                if (!result.Succeeded)
                {
                    return Forbid();
                }

                if (process.Status == ProcessStatus.Draft)
                {
                    process.Version = decimal.ToInt32(process.Version) + 1;
                    process.ApprovedBy = _userManager.GetUserName(User);
                    process.ApprovedOn = DateTime.Now;
                    process.Status = ProcessStatus.Approved;
                    process.ExportFilePath = string.Empty;

                    if (process.GeneratedFrom != null && !string.IsNullOrEmpty(process.GeneratedFrom))
                    {
                        var oldProcess = await _context.Process.FirstOrDefaultAsync(m => m.Id.ToString().Equals(process.GeneratedFrom));

                        oldProcess.Status = ProcessStatus.Obsolete;
                        process.ExportFilePath = string.Empty;
                        _context.Process.Update(oldProcess);
                    }
                }

                _context.Process.Update(process);
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

        // GET: Processes/Create
        public IActionResult Create()
        {
            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            return View();
        }

        // POST: Processes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProcessId,Name,Description,ProductId")] Process process)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastProcess = _context.Process.OrderByDescending(x => x.ProcessId).FirstOrDefault();
                    var prevId = 0;

                    if (lastProcess != null && !string.IsNullOrEmpty(lastProcess.ProcessId))
                    {
                        string[] prevProcessId = lastProcess.ProcessId.Split('-');
                        prevId = int.Parse(prevProcessId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    process.ProcessId = $"PRC-{prevId.ToString().PadLeft(2, '0')}";
                    process.EditedBy = _userManager.GetUserName(User);
                    process.EditedOn = DateTime.Now;
                    process.Version = Convert.ToDecimal(0.01);
                    process.IsLocked = false;

                    _context.Add(process);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(ProductStatus.Approved)).ToList();
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

            if (process.IsLocked)
            {
                return Forbid();
            }

            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(ProductStatus.Approved)).ToList();
            return View(process);
        }

        // POST: Processes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProcessId,GeneratedFrom,Name,ProductId,Description,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,IsLocked,Status")] Process process)
        {
            if (id != process.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (process.IsLocked)
                    {
                        return Forbid();
                    }

                    process.Version += Convert.ToDecimal(0.01);
                    process.EditedBy = _userManager.GetUserName(User);
                    process.EditedOn = DateTime.Now;
                    process.ExportFilePath = string.Empty;

                    if (process.Status == ProcessStatus.Approved)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Process ON;");
                            process.Id = _context.Process.Max(m => m.Id) + 1;
                            process.Status = ProcessStatus.Draft;
                            process.ApprovedBy = string.Empty;
                            process.ApprovedOn = null;
                            process.GeneratedFrom = id.ToString();
                            process.IsLocked = false;
                            _context.Add(process);
                            _context.SaveChanges();
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Process OFF;");
                            transaction.Commit();
                        }

                        Process? process1 = await _context.Process.FirstOrDefaultAsync(m => m.Id == id);
                        process1.IsLocked = true;
                        _context.Update(process1);
                        _context.SaveChanges();
                    }
                    else
                    {
                        process.Status = ProcessStatus.Draft;
                        _context.Update(process);
                        await _context.SaveChangesAsync();
                    }
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
                .Include(c => c.Product)
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (process == null)
            {
                return NotFound();
            }

            if (process.Status != ProcessStatus.Draft)
            {
                return Forbid();
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
                .Include(c => c.Product)
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (process.Status != ProcessStatus.Draft)
            {
                return Forbid();
            }

            if (process != null)
            {
                _context.Process.Remove(process);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public ActionResult GetDocuments()
        {
            var processes = _context.Process.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(ProcessStatus)).Cast<ProcessStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (processes != null && processes.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = processes.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public ActionResult GetDocumentsForBarchart()
        {
            var processes = _context.Process.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();
            var dates = processes.Select(x => x.EditedOn.Date).Distinct().ToList();

            if (processes != null && processes.Count > 0)
            {
                foreach (var date in dates)
                {
                    labels.Add(date.ToShortDateString());
                    var processCount = processes.Where(x => x.EditedOn.Date == date).Count();
                    dataSet.Data.Add(processCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public async Task<IActionResult> ExportProcessDetail(int id)
        {
            string path = Directory.GetCurrentDirectory() + "\\Reports\\ProcessDetailReport.rdlc";
            var process = await _context.Process
                .Where(x => x.Id == id)
                .Include(a => a.Product)
                .Include(a => a.Assets)
                .Include(a => a.Materials)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync();

            if (process != null && string.IsNullOrEmpty(process.ExportFilePath))
            {
                var assetList = new List<Asset>();
                assetList = process.Assets != null ? process.Assets.ToList() : assetList;
                var materialList = new List<Material>();
                materialList = process.Materials != null ? process.Materials.ToList() : materialList;
                var changeList = new List<Change>();
                changeList = process.Changes != null ? process.Changes.ToList() : changeList;
                var deviationList = new List<Deviation>();
                deviationList = process.Deviations != null ? process.Deviations.ToList() : deviationList;
                var capaList = new List<Capa>();
                capaList = process.Capas != null ? process.Capas.ToList() : capaList;
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("ProcessId", process.ProcessId));
                parameters.Add(new ReportParameter("Name", process.Name));
                parameters.Add(new ReportParameter("Description", process.Description));
                parameters.Add(new ReportParameter("Product", process.Product != null ? process.Product.Name : string.Empty));
                parameters.Add(new ReportParameter("AssetCount", assetList != null && assetList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("MaterialCount", materialList != null && materialList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("ChangeCount", changeList != null && changeList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("DeviationCount", deviationList != null && deviationList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("CapaCount", capaList != null && capaList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("Status", process.Status.ToString()));
                parameters.Add(new ReportParameter("EditedBy", process.EditedBy != null ? process.EditedBy : string.Empty));
                parameters.Add(new ReportParameter("EditedOn", process.EditedOn.ToShortDateString()));
                parameters.Add(new ReportParameter("ApprovedBy", process.ApprovedBy != null ? process.ApprovedBy : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", process.ApprovedOn != null ? process.ApprovedOn.Value.ToShortDateString() : string.Empty));
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Material", materialList));
                localReport.DataSources.Add(new ReportDataSource("Asset", assetList));
                localReport.DataSources.Add(new ReportDataSource("Change", changeList));
                localReport.DataSources.Add(new ReportDataSource("Deviation", deviationList));
                localReport.DataSources.Add(new ReportDataSource("CAPA", capaList));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                //**Export details as signed pdf**
                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var fileName = Path.GetFileNameWithoutExtension("ProcessDetail.pdf");
                var filePath = Path.Combine(basePath, $"ProcessDetail-{process.Id}.pdf");
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
                process.ExportFilePath = filePath;
                //if (process.Status == ProcessStatus.Approved || process.Status == ProcessStatus.Obsolete)
                //{
                //    process.ExportFilePath = Helper.SignPDF(process.ExportFilePath);
                //}

                _context.Update(process);
                await _context.SaveChangesAsync();

                return File(result, "application/pdf", "ProcessDetail.pdf");
            }
            else
            {
                return NotFound();
            }

            //var memory = new MemoryStream();
            //using (var stream = new FileStream(process.ExportFilePath, FileMode.Open))
            //{
            //    await stream.CopyToAsync(memory);
            //}
            //memory.Position = 0;

            //return File(memory, "application/pdf", "ProcessDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Process? process = _context.Process.Where(x => x.Id == id).Include(x => x.Product).FirstOrDefault();

            if (process == null)
            {
                return NotFound();
            }

            var builder = new StringBuilder();
            builder.AppendLine("Process Id,Name,Description,Product,Version,Status,Edited By,Edited On,Approved By,Approved On");
            builder.AppendLine($"{process.ProcessId},{process.Name},{process.Description},{(process.Product != null ? process.Product.Name : String.Empty)},{process.Version},{process.Status},{(process.EditedBy != null ? process.EditedBy : String.Empty)},{process.EditedOn.ToShortDateString()},{(process.ApprovedBy != null ? process.ApprovedBy : String.Empty)},{(process.ApprovedOn != null ? process.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Process.csv");
        }

        private bool ProcessExists(int id)
        {
            return _context.Process.Any(e => e.Id == id);
        }
    }
}
