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
            var applicationDbContext = _context.Deviation.Include(d => d.Product);
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
                .Include(c => c.Product)
                .Include(c => c.Process)
                .Include(c => c.Asset)
                .Include(c => c.Material)
                .Include(d => d.Capas)
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
            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();

            return View();
        }

        // POST: Deviations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        //Do we need to bind all properties???
        public async Task<IActionResult> Create([Bind("Title,DeviationId,ProcessId,AssetId,MaterialId,ProductId,Identification,IdentifiedBy")] Deviation deviation)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastDeviation = _context.Deviation.OrderByDescending(x => x.Id).FirstOrDefault();
                    var prevId = 0;

                    if (lastDeviation != null && !string.IsNullOrEmpty(lastDeviation.DeviationId))
                    {
                        string[] prevDevId = lastDeviation.DeviationId.Split('-');
                        prevId = int.Parse(prevDevId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    deviation.DeviationId = $"DEV-{prevId.ToString().PadLeft(2, '0')}";
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

            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();

            return View(deviation);
        }

        // POST: Deviations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DeviationId,Title,ProductId,ProcessId,AssetId,MaterialId,Identification,IdentifiedBy,IdentifiedOn,Evaluation,EvaluatedBy,EvaluatedOn,AcceptedBy,AcceptedOn,Resolution,Status")] Deviation deviation)
        {
            if (id != deviation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (deviation.Status == Deviation.DeviationStatus.Approved)
                    {
                        return Forbid();
                    }

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

                    deviation.ExportFilePath = string.Empty;
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
                .Include(c => c.Product)
                .Include(c => c.Process)
                .Include(c => c.Asset)
                .Include(c => c.Material)
                .Include(d => d.Capas)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (deviation == null)
            {
                return NotFound();
            }

            if (deviation.Status == DeviationStatus.Accepted || deviation.Status == DeviationStatus.Approved || deviation.Status == DeviationStatus.Resolution)
            {
                return Forbid();
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
                           .Include(c => c.Product)
                           .Include(d => d.Capas)
                           .FirstOrDefaultAsync(m => m.Id == id);

            if (deviation.Status == DeviationStatus.Accepted || deviation.Status == DeviationStatus.Approved || deviation.Status == DeviationStatus.Resolution)
            {
                return Forbid();
            }

            if (deviation != null)
            {
                _context.Deviation.Remove(deviation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Accepted(int id, string email, string pwd)
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
                        var deviation = _context.Deviation.FirstOrDefault(x => x.Id == id);

                        var isAuthorized = (User.IsInRole(Constants.AdministratorsRole) || User.IsInRole(Constants.ManagersRole)) && deviation.Status == DeviationStatus.Evaluation;
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        deviation.Status = DeviationStatus.Accepted;
                        deviation.ExportFilePath = string.Empty;
                        deviation.AcceptedBy = _userManager.GetUserName(User);
                        deviation.AcceptedOn = DateTime.Now;
                        _context.Entry(deviation).State = EntityState.Modified;
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
                return Redirect("/Deviations/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString()); ;
            }
        }

        public async Task<IActionResult> Approved(int id, string email, string pwd)
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
                        var deviation = _context.Deviation.FirstOrDefault(x => x.Id == id);

                        var isAuthorized = (User.IsInRole(Constants.AdministratorsRole) || User.IsInRole(Constants.ManagersRole)) && deviation.Status == DeviationStatus.Resolution;
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        deviation.Status = DeviationStatus.Approved;
                        deviation.ExportFilePath = string.Empty;
                        deviation.ApprovedBy = _userManager.GetUserName(User);
                        deviation.ApprovedOn = DateTime.Now;
                        _context.Entry(deviation).State = EntityState.Modified;
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
                return Redirect("/Deviations/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString());
            }
        }

        public async Task<IActionResult> ExportDeviationDetail(int id)
        {
            string mimType = "";
            string path = Directory.GetCurrentDirectory() + "\\Reports\\DeviationDetailReport.rdlc";
            var deviation = await _context.Deviation
                .Where(x => x.Id == id)
                .Include(x => x.Product)
                .Include(x => x.Process)
                .Include(x => x.Asset)
                .Include(a => a.Material)
                .Include(a => a.Capas)
                .FirstOrDefaultAsync();

            if (deviation != null && string.IsNullOrEmpty(deviation.ExportFilePath))
            {
                var capaList = new List<Capa>();
                capaList = deviation.Capas != null ? deviation.Capas.ToList() : capaList;
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("DeviationId", deviation.DeviationId));
                parameters.Add(new ReportParameter("Title", deviation.Title));
                parameters.Add(new ReportParameter("Product", deviation.Product != null ? deviation.Product.Name : string.Empty));
                parameters.Add(new ReportParameter("Process", deviation.Process != null ? deviation.Process.Name : string.Empty));
                parameters.Add(new ReportParameter("Asset", deviation.Asset != null ? deviation.Asset.Name : string.Empty));
                parameters.Add(new ReportParameter("Material", deviation.Material != null ? deviation.Material.Name : string.Empty));
                parameters.Add(new ReportParameter("CapaCount", capaList != null && capaList.Count > 0 ? "false" : "true"));
                parameters.Add(new ReportParameter("Status", deviation.Status.ToString()));
                parameters.Add(new ReportParameter("Identification", deviation.Identification));
                parameters.Add(new ReportParameter("IdentifiedBy", deviation.IdentifiedBy));
                parameters.Add(new ReportParameter("IdentifiedOn", deviation.IdentifiedOn.ToShortDateString()));
                parameters.Add(new ReportParameter("Evaluation", deviation.Evaluation != null ? deviation.Evaluation : string.Empty));
                parameters.Add(new ReportParameter("EvaluatedBy", deviation.EvaluatedBy != null ? deviation.EvaluatedBy : string.Empty));
                parameters.Add(new ReportParameter("EvaluatedOn", deviation.EvaluatedOn.HasValue ? deviation.EvaluatedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("AcceptedBy", deviation.AcceptedBy != null ? deviation.AcceptedBy : string.Empty));
                parameters.Add(new ReportParameter("AcceptedOn", deviation.AcceptedOn.HasValue ? deviation.AcceptedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("Resolution", deviation.Resolution != null ? deviation.Resolution : string.Empty));
                parameters.Add(new ReportParameter("ResolvedBy", deviation.ResolvedBy != null ? deviation.ResolvedBy : string.Empty));
                parameters.Add(new ReportParameter("ResolvedOn", deviation.ResolvedOn.HasValue ? deviation.ResolvedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("ApprovedBy", deviation.ApprovedBy != null ? deviation.ApprovedBy : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", deviation.ApprovedOn != null ? deviation.ApprovedOn.Value.ToShortDateString() : string.Empty));

                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("CAPA", capaList));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var filePath = Path.Combine(basePath, $"DeviationDetail-{deviation.Id}.pdf");

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

                deviation.ExportFilePath = filePath;

                if (deviation.Status == DeviationStatus.Approved)
                {
                    deviation.ExportFilePath = Helper.SignPDF(deviation.ExportFilePath);
                }

                _context.Update(deviation);
                await _context.SaveChangesAsync();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(deviation.ExportFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", "DeviationDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Deviation deviation = _context.Deviation.Where(x => x.Id == id).FirstOrDefault();

            var builder = new StringBuilder();
            builder.AppendLine("Deviation Id,Title,Status,Product,Process,Asset,Material,Identification,Identified By,Identified On,Evaluation,Evaluated By,Evaluated On,Accepted By,Accepted On,Resolution,Resolved By,Resolved On,Approved By,Approved On");
            builder.AppendLine($"{deviation.DeviationId},{deviation.Title},{deviation.Status},{(deviation.Product != null ? deviation.Product.Name : String.Empty)},{(deviation.Process != null ? deviation.Process.Name : String.Empty)},{(deviation.Asset != null ? deviation.Asset.Name : String.Empty)},{(deviation.Material != null ? deviation.Material.Name : String.Empty)},{deviation.Identification},{deviation.IdentifiedBy},{deviation.IdentifiedOn.ToShortDateString()},{deviation.Evaluation},{deviation.EvaluatedBy},{(deviation.EvaluatedOn.HasValue ? deviation.EvaluatedOn.Value.ToShortDateString() : String.Empty)},{deviation.AcceptedBy},{(deviation.AcceptedOn.HasValue ? deviation.AcceptedOn.Value.ToShortDateString() : String.Empty)},{deviation.Resolution},{deviation.ResolvedBy},{(deviation.ResolvedOn.HasValue ? deviation.ResolvedOn.Value.ToShortDateString() : String.Empty)},{deviation.ApprovedBy},{(deviation.ApprovedOn.HasValue ? deviation.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Deviation.csv");
        }

        public ActionResult GetDocuments()
        {
            var deviations = _context.Deviation.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(DeviationStatus)).Cast<DeviationStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (deviations != null && deviations.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = deviations.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        private bool DeviationExists(int id)
        {
            return (_context.Deviation?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
