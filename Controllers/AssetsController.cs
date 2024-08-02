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
using OpenQMS.Services;
using static OpenQMS.Models.Asset;
using static OpenQMS.Models.Process;

namespace OpenQMS.Controllers
{
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        public AssetsController
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

        // GET: Assets
        public async Task<IActionResult> Index()
        {
            var assetsList = await _context.Asset.Include(c => c.Process).ToListAsync();
            return View(assetsList);
        }

        // GET: Assets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Asset == null)
            {
                return NotFound();
            }

            var asset = await _context.Asset
                .Include(a => a.Process)
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, string InputEmail, string InputPassword)
        {
            var asset = _context.Asset.FirstOrDefault(x => x.Id == id);

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

                if (asset.Status == AssetStatus.Draft)
                {
                    asset.Version = Decimal.ToInt32(asset.Version) + 1;
                    asset.ApprovedBy = _userManager.GetUserName(User);
                    asset.ApprovedOn = DateTime.Now;
                    asset.Status = AssetStatus.Approved;
                    asset.ExportFilePath = String.Empty;

                    if (asset.GeneratedFrom != null && !string.IsNullOrEmpty(asset.GeneratedFrom))
                    {
                        var oldAsset = await _context.Asset.FirstOrDefaultAsync(m => m.Id.ToString().Equals(asset.GeneratedFrom));

                        oldAsset.Status = AssetStatus.Obsolete;
                        asset.ExportFilePath = String.Empty;
                        _context.Asset.Update(oldAsset);
                    }
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

        // GET: Assets/Create
        public IActionResult Create()
        {
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(ProcessStatus.Approved)).ToList();
            return View();
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AssetId,Name,Description,ProcessId")] Asset asset)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastAsset = _context.Asset.OrderByDescending(x => x.AssetId).FirstOrDefault();
                    var prevId = 0;

                    if (lastAsset != null && !string.IsNullOrEmpty(lastAsset.AssetId))
                    {
                        string[] prevAssetId = lastAsset.AssetId.Split('-');
                        prevId = int.Parse(prevAssetId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    asset.AssetId = $"AST-{prevId.ToString().PadLeft(2, '0')}";
                    asset.EditedBy = _userManager.GetUserName(User);
                    asset.EditedOn = DateTime.Now;
                    asset.Version = Convert.ToDecimal(0.01);
                    asset.IsLocked = false;

                    _context.Add(asset);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(ProcessStatus.Approved)).ToList();
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

            if (asset.IsLocked)
            {
                return Forbid();
            }

            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(ProcessStatus.Approved)).ToList();
            return View(asset);
        }

        // POST: Assets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AssetId,GeneratedFrom,Name,Description,ProcessId,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,IsLocked,Status")] Asset asset)
        {
            if (id != asset.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (asset.IsLocked)
                    {
                        return Forbid();
                    }

                    asset.Version += Convert.ToDecimal(0.01);
                    asset.EditedBy = _userManager.GetUserName(User);
                    asset.EditedOn = DateTime.Now;
                    asset.ExportFilePath = string.Empty;

                    if (asset.Status == AssetStatus.Approved)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Asset ON;");
                            asset.Id = _context.Asset.Max(m => m.Id) + 1;
                            asset.Status = AssetStatus.Draft;
                            asset.ApprovedBy = null;
                            asset.ApprovedOn = null;
                            asset.GeneratedFrom = id.ToString();
                            asset.IsLocked = false;
                            _context.Add(asset);
                            _context.SaveChanges();
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Asset OFF;");
                            transaction.Commit();
                        }

                        Asset? asset1 = await _context.Asset.FirstOrDefaultAsync(m => m.Id == id);
                        asset1.IsLocked = true;
                        _context.Update(asset1);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        asset.Status = AssetStatus.Draft;
                        _context.Update(asset);
                        await _context.SaveChangesAsync();
                    }
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

        // GET: Assets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Asset == null)
            {
                return NotFound();
            }

            var asset = await _context.Asset
                .Include(c => c.Process)
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null)
            {
                return NotFound();
            }

            if (asset.Status != AssetStatus.Draft)
            {
                return Forbid();
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
                .Include(c => c.Process)
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset.Status != AssetStatus.Draft)
            {
                return Forbid();
            }

            if (asset != null)
            {
                _context.Asset.Remove(asset);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public ActionResult GetDocuments()
        {
            var assets = _context.Asset.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(AssetStatus)).Cast<AssetStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (assets != null && assets.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = assets.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public ActionResult GetDocumentsForBarchart()
        {
            var assets = _context.Asset.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();
            var dates = assets.Select(x => x.EditedOn.Date).Distinct().ToList();

            if (assets != null && assets.Count > 0)
            {
                foreach (var date in dates)
                {
                    labels.Add(date.ToShortDateString());
                    var assetCount = assets.Where(x => x.EditedOn.Date == date).Count();
                    dataSet.Data.Add(assetCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public async Task<IActionResult> ExportAssetDetail(int id)
        {
            string path = Directory.GetCurrentDirectory() + "/Reports/AssetDetailReport.rdlc";
            var asset = await _context.Asset
                .Where(x => x.Id == id)
                .Include(a => a.Process)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync();

            if (asset != null && string.IsNullOrEmpty(asset.ExportFilePath))
            {
                var changeList = new List<Change>();
                changeList = asset.Changes != null ? asset.Changes.ToList() : changeList;
                var deviationList = new List<Deviation>();
                deviationList = asset.Deviations != null ? asset.Deviations.ToList() : deviationList;
                var capaList = new List<Capa>();
                capaList = asset.Capas != null ? asset.Capas.ToList() : capaList;
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("AssetId", asset.AssetId));
                parameters.Add(new ReportParameter("Name", asset.Name));
                parameters.Add(new ReportParameter("Description", asset.Description));
                parameters.Add(new ReportParameter("Process", asset.Process != null ? asset.Process.Name : string.Empty));
                parameters.Add(new ReportParameter("ChangeCount", changeList != null && changeList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("DeviationCount", deviationList != null && deviationList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("CapaCount", capaList != null && capaList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("Status", asset.Status.ToString()));
                parameters.Add(new ReportParameter("EditedBy", asset.EditedBy != null ? asset.EditedBy : string.Empty));
                parameters.Add(new ReportParameter("EditedOn", asset.EditedOn.ToShortDateString()));
                parameters.Add(new ReportParameter("ApprovedBy", asset.ApprovedBy != null ? asset.ApprovedBy : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", asset.ApprovedOn != null ? asset.ApprovedOn.Value.ToShortDateString() : string.Empty));
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Change", changeList));
                localReport.DataSources.Add(new ReportDataSource("Deviation", deviationList));
                localReport.DataSources.Add(new ReportDataSource("CAPA", capaList));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                //**Export details as signed pdf**
                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "/Files/");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var filePath = Path.Combine(basePath, $"AssetDetail-{asset.Id}.pdf");
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
                asset.ExportFilePath = filePath;
                //if (asset.Status == Asset.AssetStatus.Approved || asset.Status == Asset.AssetStatus.Obsolete)
                //{
                //    asset.ExportFilePath = Helper.SignPDF(asset.ExportFilePath);
                //}

                _context.Update(asset);
                await _context.SaveChangesAsync();

                return File(result, "application/pdf", "AssetDetail.pdf");
            }
            else
            {
                return NotFound();
            }

            //var memory = new MemoryStream();
            //using (var stream = new FileStream(asset.ExportFilePath, FileMode.Open))
            //{
            //    await stream.CopyToAsync(memory);
            //}
            //memory.Position = 0;

            //return File(memory, "application/pdf", "AssetDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Asset? asset = _context.Asset.Where(x => x.Id == id).Include(x => x.Process).FirstOrDefault();

            if (asset == null)
            {
                return NotFound();
            }

            var builder = new StringBuilder();
            builder.AppendLine("Asset Id,Name,Description,Process,Version,Status,Edited By,Edited On,Approved By,Approved On");
            builder.AppendLine($"{asset.AssetId},{asset.Name},{asset.Description},{(asset.Process != null ? asset.Process.Name : String.Empty)},{asset.Version},{asset.Status},{(asset.EditedBy != null ? asset.EditedBy : String.Empty)},{asset.EditedOn.ToShortDateString()},{(asset.ApprovedBy != null ? asset.ApprovedBy : String.Empty)},{(asset.ApprovedOn != null ? asset.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Asset.csv");
        }

        private bool AssetExists(int id)
        {
            return (_context.Asset?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
