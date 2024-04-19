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

using System.Data;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
using OpenQMS.Services;
using static OpenQMS.Models.Capa;

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
            return _context.Capa != null ?
                View(await _context.Capa.ToListAsync()) :
                Problem("Entity set 'ApplicationDbContext.Capa'  is null.");
        }

        // GET: Capas/Details/5
        public async Task<IActionResult> Details(int? id, bool isUserVerified = true)
        {
            if (id == null || _context.Capa == null)
            {
                return NotFound();
            }

            var capa = await _context.Capa
                .Include(c => c.Product)
                .Include(c => c.Process)
                .Include(c => c.Asset)
                .Include(c => c.Material)
                .Include(d => d.Deviation)
                .Include(d => d.Changes)
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
            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();
            ViewData["Deviations"] = _context.Deviation.Where(x => x.Status.Equals(Deviation.DeviationStatus.Approved)).ToList();

            return View();
        }

        // POST: Capas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CapaId,Title,ProductId,ProcessId,AssetId,MaterialId,DeviationId,CorrectiveAction,PreventiveAction,DeterminedBy,DeterminedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,ImplementedBy,ImplementedOn,ApprovedBy,ApprovedOn,Status")] Capa capa)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastCapa = _context.Capa.OrderByDescending(x => x.CapaId).FirstOrDefault();
                    var prevId = 0;

                    if (lastCapa != null && !string.IsNullOrEmpty(lastCapa.CapaId))
                    {
                        string[] prevCapaId = lastCapa.CapaId.Split('-');
                        prevId = int.Parse(prevCapaId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    capa.CapaId = $"CPA-{prevId.ToString().PadLeft(2, '0')}";
                    capa.DeterminedBy = _userManager.GetUserName(User);
                    capa.DeterminedOn = DateTime.Now;
                    capa.Status = Capa.CapaStatus.Determination;

                    _context.Add(capa);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }

            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();
            ViewData["Deviations"] = _context.Deviation.Where(x => x.Status.Equals(Deviation.DeviationStatus.Approved)).ToList();
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

            if (capa.Status == CapaStatus.Approved)
            {
                return Forbid();
            }

            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();
            ViewData["Deviations"] = _context.Deviation.Where(x => x.Status.Equals(Deviation.DeviationStatus.Approved)).ToList();

            return View(capa);
        }

        // POST: Capas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CapaId,Title,ProductId,ProcessId,AssetId,MaterialId,DeviationId,CorrectiveAction,PreventiveAction,DeterminedBy,DeterminedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,Status")] Capa capa)
        {
            if (id != capa.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (capa.Status == CapaStatus.Approved)
                    {
                        return Forbid();
                    }

                    if (capa.Status == CapaStatus.Determination || capa.Status == CapaStatus.Assessment)
                    {
                        capa.AssessedBy = _userManager.GetUserName(User);
                        capa.AssessedOn = DateTime.Now;
                        capa.Status = CapaStatus.Assessment;
                    }
                    else if (capa.Status == CapaStatus.Accepted || capa.Status == CapaStatus.Implementation)
                    {
                        capa.ImplementedBy = _userManager.GetUserName(User);
                        capa.ImplementedOn = DateTime.Now;
                        capa.Status = CapaStatus.Implementation;
                    }

                    capa.ExportFilePath = string.Empty;
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
                .Include(c => c.Product)
                .Include(c => c.Process)
                .Include(c => c.Asset)
                .Include(c => c.Material)
                .Include(d => d.Deviation)
                .Include(d => d.Changes)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (capa == null)
            {
                return NotFound();
            }

            if (capa.Status == CapaStatus.Accepted || capa.Status == CapaStatus.Approved || capa.Status == CapaStatus.Implementation)
            {
                return Forbid();
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
                           .Include(c => c.Product)
                           .Include(d => d.Deviation)
                           .Include(d => d.Changes)
                           .FirstOrDefaultAsync(m => m.Id == id);

            if (capa != null)
            {
                _context.Capa.Remove(capa);
            }

            if (capa.Status == CapaStatus.Accepted || capa.Status == CapaStatus.Approved || capa.Status == CapaStatus.Implementation)
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Accepted(int id, string email, string pwd)
        {
            if (_context.Capa == null)
            {
                return NotFound();
            }

            bool isUserVerified;
            AppUser user = new();
            user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
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

                        var isAuthorized = (User.IsInRole(Constants.AdministratorsRole) || User.IsInRole(Constants.ManagersRole)) && capa.Status == CapaStatus.Assessment;
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        capa.Status = CapaStatus.Accepted;
                        capa.ExportFilePath = string.Empty;
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

        public async Task<IActionResult> Approved(int id, string email, string pwd)
        {
            if (_context.Capa == null)
            {
                return NotFound();
            }

            bool isUserVerified;
            AppUser user = new();
            user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
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

                        var isAuthorized = (User.IsInRole(Constants.AdministratorsRole) || User.IsInRole(Constants.ManagersRole)) && capa.Status == CapaStatus.Implementation;
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        capa.Status = CapaStatus.Approved;
                        capa.ExportFilePath = string.Empty;
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

        public async Task<IActionResult> ExportCAPADetail(int id)
        {
            if (_context.Capa == null)
            {
                return NotFound();
            }

            string path = Directory.GetCurrentDirectory() + "\\Reports\\CapaDetailReport.rdlc";
            var capa = await _context.Capa
                .Where(x => x.Id == id)
                .Include(x => x.Product)
                .Include(x => x.Process)
                .Include(x => x.Asset)
                .Include(x => x.Material)
                .Include(x => x.Deviation)
                .Include(x => x.Changes)
                .FirstOrDefaultAsync();

            if (capa != null && string.IsNullOrEmpty(capa.ExportFilePath))
            {
                var changeList = new List<Change>();
                changeList = capa.Changes != null ? capa.Changes.ToList() : changeList;
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("CapaId", capa.CapaId));
                parameters.Add(new ReportParameter("Title", capa.Title));
                parameters.Add(new ReportParameter("Product", capa.Product != null ? capa.Product.Name : string.Empty));
                parameters.Add(new ReportParameter("Process", capa.Process != null ? capa.Process.Name : string.Empty));
                parameters.Add(new ReportParameter("Asset", capa.Asset != null ? capa.Asset.Name : string.Empty));
                parameters.Add(new ReportParameter("Material", capa.Material != null ? capa.Material.Name : string.Empty));
                parameters.Add(new ReportParameter("Deviation", capa.Deviation != null ? capa.Deviation.Title : string.Empty));
                parameters.Add(new ReportParameter("ChangeCount", changeList != null && changeList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("Status", capa.Status.ToString()));
                parameters.Add(new ReportParameter("CorrectiveAction", capa.CorrectiveAction));
                parameters.Add(new ReportParameter("PreventiveAction", capa.PreventiveAction));
                parameters.Add(new ReportParameter("DeterminedBy", capa.DeterminedBy));
                parameters.Add(new ReportParameter("DeterminedOn", capa.DeterminedOn.ToShortDateString()));
                parameters.Add(new ReportParameter("Assessment", capa.Assessment != null ? capa.Assessment : string.Empty));
                parameters.Add(new ReportParameter("AssessedBy", capa.AssessedBy != null ? capa.AssessedBy : string.Empty));
                parameters.Add(new ReportParameter("AssessedOn", capa.AssessedOn.HasValue ? capa.AssessedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("AcceptedBy", capa.AcceptedBy != null ? capa.AcceptedBy : string.Empty));
                parameters.Add(new ReportParameter("AcceptedOn", capa.AcceptedOn.HasValue ? capa.AcceptedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("Implementation", capa.Implementation != null ? capa.Implementation : string.Empty));
                parameters.Add(new ReportParameter("ImplementedBy", capa.ImplementedBy != null ? capa.ImplementedBy : string.Empty));
                parameters.Add(new ReportParameter("ImplementedOn", capa.ImplementedOn.HasValue ? capa.ImplementedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("ApprovedBy", capa.ApprovedBy != null ? capa.ApprovedBy : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", capa.ApprovedOn != null ? capa.ApprovedOn.Value.ToShortDateString() : string.Empty));
                LocalReport localReport = new();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Change", changeList));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                //**Export details as signed pdf**
                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var filePath = Path.Combine(basePath, $"CapaDetail-{capa.Id}.pdf");
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
                capa.ExportFilePath = filePath;
                //if (capa.Status == CapaStatus.Approved)
                //{
                //    capa.ExportFilePath = Helper.SignPDF(capa.ExportFilePath);
                //}

                _context.Update(capa);
                await _context.SaveChangesAsync();

                return File(result, "application/pdf", "CapaDetail.pdf");
            }
            else
            {
                return NotFound();
            }

            //var memory = new MemoryStream();
            //using (var stream = new FileStream(capa.ExportFilePath, FileMode.Open))
            //{
            //    await stream.CopyToAsync(memory);
            //}
            //memory.Position = 0;

            //return File(memory, "application/pdf", "CapaDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            if(_context.Capa == null)
            {
                return NotFound();
            }

            Capa capa = _context.Capa.Where(x => x.Id == id).FirstOrDefault();
            if(capa == null)
            {
                return NotFound();
            }

            var builder = new StringBuilder();
            builder.AppendLine("CAPA Id,Title,Status,Product,Process,Asset,Material,Deviation,CorrectiveAction,PreventiveAction,Determined By,Determined On,Assessment,Assessed By,Assessed On,Accepted By,Accepted On,Implementation,Implemented By,Implemented On,Approved By,Approved On");
            builder.AppendLine($"{capa.CapaId},{capa.Title},{capa.Status},{(capa.Product != null ? capa.Product.Name : String.Empty)},{(capa.Process != null ? capa.Process.Name : String.Empty)},{(capa.Asset != null ? capa.Asset.Name : String.Empty)},{(capa.Material != null ? capa.Material.Name : String.Empty)},{(capa.Deviation != null ? capa.Deviation.Title : String.Empty)},{capa.CorrectiveAction},{capa.PreventiveAction},{capa.DeterminedBy},{capa.DeterminedOn.ToShortDateString()},{capa.Assessment},{capa.AssessedBy},{(capa.AssessedOn.HasValue ? capa.AssessedOn.Value.ToShortDateString() : String.Empty)},{capa.AcceptedBy},{(capa.AcceptedOn.HasValue ? capa.AcceptedOn.Value.ToShortDateString() : String.Empty)},{capa.Implementation},{capa.ImplementedBy},{(capa.ImplementedOn.HasValue ? capa.ImplementedOn.Value.ToShortDateString() : String.Empty)},{capa.ApprovedBy},{(capa.ApprovedOn.HasValue ? capa.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "CAPA.csv");
        }

        public ActionResult GetDocuments()
        {
            if (_context.Capa == null)
            {
                return NotFound();
            }

            var capas = _context.Capa.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(CapaStatus)).Cast<CapaStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (capas != null && capas.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = capas.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        private bool CapaExists(int id)
        {
            return (_context.Capa?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
