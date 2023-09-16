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
using static OpenQMS.Models.Process;

namespace OpenQMS.Controllers
{
    public class MaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public MaterialsController(ApplicationDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Materials
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Material.Include(c => c.Process).Include(x => x.EditedByUser).Include(a => a.ApprovedByUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Materials/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Material == null)
            {
                return NotFound();
            }

            var material = await _context.Material
                .Include(a => a.Process)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .Include(a => a.EditedByUser)
                .Include(a => a.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
            {
                return NotFound();
            }

            return View(material);
        }

        // POST: Materials/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, string InputEmail, string InputPassword)
        {
            var material = _context.Material.FirstOrDefault(x => x.Id == id);

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

                if (material.Status == Material.MaterialStatus.Draft)
                {
                    material.Version = Decimal.ToInt32(material.Version) + 1;
                    material.ApprovedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    material.ApprovedOn = DateTime.Now;
                    material.Status = Material.MaterialStatus.Approved;
                    material.ExportFilePath = String.Empty;

                    if (material.GeneratedFrom != null && !string.IsNullOrEmpty(material.GeneratedFrom))
                    {
                        var oldMaterial = await _context.Material.FirstOrDefaultAsync(m => m.Id.ToString().Equals(material.GeneratedFrom));

                        oldMaterial.Status = Material.MaterialStatus.Obsolete;
                        material.ExportFilePath = String.Empty;
                        _context.Material.Update(oldMaterial);
                    }
                }

                _context.Material.Update(material);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaterialExists(material.Id))
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

        // GET: Materials/Create
        public IActionResult Create()
        {
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(ProcessStatus.Approved)).ToList();
            return View();
        }

        // POST: Materials/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaterialId,Name,Description,ProcessId")] Material material)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastMaterial = _context.Material.OrderByDescending(x => x.MaterialId).FirstOrDefault();
                    var prevId = 0;

                    if (lastMaterial != null && !string.IsNullOrEmpty(lastMaterial.MaterialId))
                    {
                        string[] prevMaterialId = lastMaterial.MaterialId.Split('-');
                        prevId = int.Parse(prevMaterialId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    material.MaterialId = $"MAT-{prevId.ToString().PadLeft(2, '0')}";
                    material.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    material.EditedOn = DateTime.Now;
                    material.Version = Convert.ToDecimal(0.01);
                    material.IsLocked = false;

                    _context.Add(material);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DataException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(material);
        }

        // GET: Materials/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Material == null)
            {
                return NotFound();
            }

            var material = await _context.Material.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(ProcessStatus.Approved)).ToList();
            return View(material);
        }

        // POST: Materials/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MaterialId,Name,Description,ProcessId,Version,EditedBy,EditedOn,ApprovedBy,ApprovedOn,GeneratedFrom,IsLocked,Status")] Material material)
        {
            if (id != material.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    material.EditedBy = Convert.ToInt32(_userManager.GetUserId(User));
                    material.EditedOn = DateTime.Now;
                    material.Version += Convert.ToDecimal(0.01);
                    material.ExportFilePath = string.Empty;

                    if (material.Status == Material.MaterialStatus.Approved)
                    {
                        if (material.IsLocked)
                        {
                            return Forbid();
                        }
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Material ON;");
                            material.Id = _context.Material.Max(m => m.Id) + 1;
                            material.Status = Material.MaterialStatus.Draft;
                            material.ApprovedBy = null;
                            material.ApprovedOn = null;
                            material.GeneratedFrom = id.ToString();
                            material.IsLocked = false;
                            _context.Add(material);
                            _context.SaveChanges();
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Material OFF;");
                            transaction.Commit();
                        }

                        Material material1 = await _context.Material.FirstOrDefaultAsync(m => m.Id == id);
                        material1.IsLocked = true;
                        _context.Update(material1);
                        _context.SaveChanges();
                    }
                    else
                    {
                        material.Status = Material.MaterialStatus.Draft;
                        _context.Update(material);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialExists(material.Id))
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

            return View(material);
        }

        // GET: Materials/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Material == null)
            {
                return NotFound();
            }

            var material = await _context.Material
                .Include(c => c.Process)
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .Include(p => p.EditedByUser)
                .Include(p => p.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
            {
                return NotFound();
            }

            if (material.Status != Material.MaterialStatus.Draft)
            {
                return Forbid();
            }

            return View(material);
        }

        // POST: Materials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Material == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Material' is null.");
            }

            var material = await _context.Material
                .Include(c => c.Process)
                .Include(p => p.Changes)
                .Include(p => p.Deviations)
                .Include(p => p.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material.Status != Material.MaterialStatus.Draft)
            {
                return Forbid();
            }

            if (material != null)
            {
                _context.Material.Remove(material);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public ActionResult GetDocuments()
        {
            var materials = _context.Material.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(Material.MaterialStatus)).Cast<Material.MaterialStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (materials != null && materials.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = materials.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public ActionResult GetDocumentsForBarchart()
        {
            var materials = _context.Material.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();
            var dates = materials.Select(x => x.EditedOn.Date).Distinct().ToList();

            if (materials != null && materials.Count > 0)
            {
                foreach (var date in dates)
                {
                    labels.Add(date.ToShortDateString());
                    var materialCount = materials.Where(x => x.EditedOn.Date == date).Count();
                    dataSet.Data.Add(materialCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public async Task<IActionResult> ExportMaterialDetail(int id)
        {
            string mimType = "";
            string path = Directory.GetCurrentDirectory() + "\\Reports\\MaterialDetailReport.rdlc";
            var material = await _context.Material
                .Where(x => x.Id == id)
                .Include(a => a.Process)
                .Include(a => a.Changes)
                .Include(a => a.Deviations)
                .Include(a => a.Capas)
                .Include(a => a.EditedByUser)
                .Include(a => a.ApprovedByUser)
                .FirstOrDefaultAsync();

            if (material != null && string.IsNullOrEmpty(material.ExportFilePath))
            {
                var capaList = new List<Capa>();
                capaList = material.Capas != null ? material.Capas.ToList() : capaList;
                var changeList = new List<Change>();
                changeList = material.Changes != null ? material.Changes.ToList() : changeList;
                var deviationList = new List<Deviation>();
                deviationList = material.Deviations != null ? material.Deviations.ToList() : deviationList;
                //Dictionary<string, string> parameters = new Dictionary<string, string>();
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("MaterialId", material.MaterialId));
                parameters.Add(new ReportParameter("Name", material.Name));
                parameters.Add(new ReportParameter("Description", material.Description));
                parameters.Add(new ReportParameter("Process", material.Process != null ? material.Process.Name : string.Empty));
                parameters.Add(new ReportParameter("ChangeCount", changeList != null && changeList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("DeviationCount", deviationList != null && deviationList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("CapaCount", capaList != null && capaList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("Status", material.Status.ToString()));
                parameters.Add(new ReportParameter("EditedBy", material.EditedByUser != null ? material.EditedByUser.FirstName : string.Empty));
                parameters.Add(new ReportParameter("EditedOn", material.EditedOn != null ? material.EditedOn.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("ApprovedBy", material.ApprovedByUser != null ? material.ApprovedByUser.FirstName : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", material.ApprovedOn != null ? material.ApprovedOn.Value.ToShortDateString() : string.Empty));
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Change", changeList));
                localReport.DataSources.Add(new ReportDataSource("Deviation", deviationList));
                localReport.DataSources.Add(new ReportDataSource("CAPA", capaList));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var filePath = Path.Combine(basePath, $"MaterialDetail-{material.Id}.pdf");

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

                material.ExportFilePath = filePath;

                if (material.Status == Material.MaterialStatus.Approved || material.Status == Material.MaterialStatus.Obsolete)
                {
                    material.ExportFilePath = Helper.SignPDF(material.ExportFilePath);
                }

                _context.Update(material);
                await _context.SaveChangesAsync();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(material.ExportFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", "MaterialDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Material material = _context.Material.Where(x => x.Id == id).Include(x => x.Process).Include(x => x.EditedByUser).Include(x => x.ApprovedByUser).FirstOrDefault();

            var builder = new StringBuilder();
            builder.AppendLine("Material Id,Name,Description,Process,Version,Status,Edited By,Edited On,Approved By,Approved On");
            builder.AppendLine($"{material.MaterialId},{material.Name},{material.Description},{(material.Process != null ? material.Process.Name : String.Empty)},{material.Version},{material.Status},{(material.EditedByUser != null ? material.EditedByUser.FirstName : String.Empty)},{(material.EditedOn != null ? material.EditedOn.ToShortDateString() : String.Empty)},{(material.ApprovedByUser != null ? material.ApprovedByUser.FirstName : String.Empty)},{(material.ApprovedOn != null ? material.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Process.csv");
        }

        private bool MaterialExists(int id)
        {
            return (_context.Material?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
