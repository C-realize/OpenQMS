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
using static OpenQMS.Models.Change;

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
                View(await _context.Change.Include(x => x.Product).ToListAsync()) :
                Problem("Entity set 'ApplicationDbContext.Change'  is null.");
        }

        // GET: Changes/Details/5
        public async Task<IActionResult> Details(int? id, bool isUserVerified = true)
        {
            if (id == null || _context.Change == null)
            {
                return NotFound();
            }

            var change = await _context.Change
                .Include(c => c.Product)
                .Include(c => c.Process)
                .Include(c => c.Asset)
                .Include(c => c.Material)
                .Include(d => d.Capa)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (change == null)
            {
                return NotFound();
            }

            if (!isUserVerified)
            {
                ModelState.AddModelError("", "Username or password is not correct");
            }

            return View(change);
        }

        // POST: Changes/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, [Bind("Id,ChangeId,ProductId,ProcessId,AssetId,MaterialId,CapaId,Title,Proposal,ProposedBy,ProposedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,ImplementedBy,ImplementedOn,Status")] Change change, string InputEmail, string InputPassword)
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
            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();
            ViewData["Capa"] = _context.Capa.Where(x => x.Status.Equals(Capa.CapaStatus.Approved)).ToList();

            return View();
        }

        // POST: Changes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ChangeId,Proposal,ProposedBy,ProductId,ProcessId,AssetId,MaterialId,CapaId")] Change change)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastChange = _context.Change.OrderByDescending(x => x.Id).FirstOrDefault();
                    var prevId = 0;

                    if (lastChange != null && !string.IsNullOrEmpty(lastChange.ChangeId))
                    {
                        string[] prevChangeId = lastChange.ChangeId.Split('-');
                        prevId = int.Parse(prevChangeId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    change.ChangeId = $"CHG-{prevId.ToString().PadLeft(2, '0')}";
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

            ViewData["Products"] = _context.Product.Where(x => x.Status.Equals(Product.ProductStatus.Approved)).ToList();
            ViewData["Processes"] = _context.Process.Where(x => x.Status.Equals(Process.ProcessStatus.Approved)).ToList();
            ViewData["Assets"] = _context.Asset.Where(x => x.Status.Equals(Asset.AssetStatus.Approved)).ToList();
            ViewData["Materials"] = _context.Material.Where(x => x.Status.Equals(Material.MaterialStatus.Approved)).ToList();
            ViewData["Capa"] = _context.Capa.Where(x => x.Status.Equals(Capa.CapaStatus.Approved)).ToList();

            return View(change);
        }

        // POST: Changes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ChangeId,Title,Proposal,ProposedBy,ProposedOn,Assessment,AssessedBy,AssessedOn,AcceptedBy,AcceptedOn,Implementation,Status,ProductId,ProcessId,AssetId,MaterialId,CapaId")] Change change)
        {
            if (id != change.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (change.Status == Change.ChangeStatus.Approved)
                    {
                        return Forbid();
                    }

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

                    change.ExportFilePath = string.Empty;
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
                .Include(c => c.Product)
                .Include(c => c.Process)
                .Include(c => c.Asset)
                .Include(c => c.Material)
                .Include(d => d.Capa)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (change == null)
            {
                return NotFound();
            }

            if (change.Status == ChangeStatus.Accepted || change.Status == ChangeStatus.Approved || change.Status == ChangeStatus.Implementation)
            {
                return Forbid();
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
                            .Include(c => c.Product)
                            .Include(d => d.Capa)
                            .FirstOrDefaultAsync(m => m.Id == id);

            if (change.Status == ChangeStatus.Accepted || change.Status == ChangeStatus.Approved || change.Status == ChangeStatus.Implementation)
            {
                return Forbid();
            }

            if (change != null)
            {
                _context.Change.Remove(change);
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
                        var change = _context.Change.FirstOrDefault(x => x.Id == id);

                        var isAuthorized = (User.IsInRole(Constants.AdministratorsRole) || User.IsInRole(Constants.ManagersRole)) && change.Status == Change.ChangeStatus.Assessment;
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        change.Status = ChangeStatus.Accepted;
                        change.ExportFilePath = string.Empty;
                        change.AcceptedBy = _userManager.GetUserName(User);
                        change.AcceptedOn = DateTime.Now;
                        _context.Entry(change).State = EntityState.Modified;
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
                return Redirect("/Changes/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString()); ;
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
                        var change = _context.Change.FirstOrDefault(x => x.Id == id);

                        var isAuthorized = (User.IsInRole(Constants.AdministratorsRole) || User.IsInRole(Constants.ManagersRole)) && change.Status == Change.ChangeStatus.Implementation;
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        change.Status = ChangeStatus.Approved;
                        change.ExportFilePath = string.Empty;
                        change.ApprovedBy = _userManager.GetUserName(User);
                        change.ApprovedOn = DateTime.Now;
                        _context.Entry(change).State = EntityState.Modified;
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
                return Redirect("/Changes/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString());
            }
        }

        public async Task<IActionResult> ExportChangeDetail(int id)
        {
            string mimType = "";
            string path = Directory.GetCurrentDirectory() + "\\Reports\\ChangeDetailReport.rdlc";
            var change = await _context.Change
                .Where(x => x.Id == id)
                .Include(x => x.Product)
                .Include(x => x.Process)
                .Include(x => x.Asset)
                .Include(a => a.Material)
                .Include(x => x.Capa)
                .FirstOrDefaultAsync();
            
            if (change != null && string.IsNullOrEmpty(change.ExportFilePath))
            {
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("ChangeId", change.ChangeId));
                parameters.Add(new ReportParameter("Title", change.Title));
                parameters.Add(new ReportParameter("Product", change.Product != null ? change.Product.Name : string.Empty));
                parameters.Add(new ReportParameter("Process", change.Process != null ? change.Process.Name : string.Empty));
                parameters.Add(new ReportParameter("Asset", change.Asset != null ? change.Asset.Name : string.Empty));
                parameters.Add(new ReportParameter("Material", change.Material != null ? change.Material.Name : string.Empty));
                parameters.Add(new ReportParameter("Capa", change.Capa != null ? change.Capa.Title : string.Empty));
                parameters.Add(new ReportParameter("Status", change.Status.ToString()));
                parameters.Add(new ReportParameter("Proposal", change.Proposal));
                parameters.Add(new ReportParameter("ProposedBy", change.ProposedBy));
                parameters.Add(new ReportParameter("ProposedOn", change.ProposedOn.ToShortDateString()));
                parameters.Add(new ReportParameter("Assessment", change.Assessment != null ? change.Assessment : string.Empty));
                parameters.Add(new ReportParameter("AssessedBy", change.AssessedBy != null ? change.AssessedBy : string.Empty));
                parameters.Add(new ReportParameter("AssessedOn", change.AssessedOn.HasValue ? change.AssessedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("AcceptedBy", change.AcceptedBy != null ? change.AcceptedBy : string.Empty));
                parameters.Add(new ReportParameter("AcceptedOn", change.AcceptedOn.HasValue ? change.AcceptedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("Implementation", change.Implementation != null ? change.Implementation : string.Empty));
                parameters.Add(new ReportParameter("ImplementedBy", change.ImplementedBy != null ? change.ImplementedBy : string.Empty));
                parameters.Add(new ReportParameter("ImplementedOn", change.ImplementedOn.HasValue ? change.ImplementedOn.Value.ToShortDateString() : string.Empty));
                parameters.Add(new ReportParameter("ApprovedBy", change.ApprovedBy != null ? change.ApprovedBy : string.Empty));
                parameters.Add(new ReportParameter("ApprovedOn", change.ApprovedOn != null ? change.ApprovedOn.Value.ToShortDateString() : string.Empty));
                
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                bool basePathExists = System.IO.Directory.Exists(basePath);
                if (!basePathExists) Directory.CreateDirectory(basePath);
                var filePath = Path.Combine(basePath, $"ChangeDetail-{change.Id}.pdf");

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
                
                change.ExportFilePath = filePath;

                if (change.Status == ChangeStatus.Approved)
                {
                    change.ExportFilePath = Helper.SignPDF(change.ExportFilePath);
                }

                _context.Update(change);
                await _context.SaveChangesAsync();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(change.ExportFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", "ChangeDetail.pdf");
        }

        public ActionResult ExportDetailInCSV(int id)
        {
            Change change = _context.Change.Where(x => x.Id == id).FirstOrDefault();

            var builder = new StringBuilder();
            builder.AppendLine("Change Id,Title,Status,Product,Process,Asset,Material,CAPA,Proposal,Proposed By,Proposed On,Assessment,Assessed By,Assessed On,Accepted By,Accepted On,Implementation,Implemented By,Implemented On,Approved By,Approved On");
            builder.AppendLine($"{change.ChangeId},{change.Title},{change.Status},{(change.Product != null ? change.Product.Name : String.Empty)},{(change.Process != null ? change.Process.Name : String.Empty)},{(change.Asset != null ? change.Asset.Name : String.Empty)},{(change.Material != null ? change.Material.Name : String.Empty)},{(change.Capa != null ? change.Capa.Title : String.Empty)},{change.Proposal},{change.ProposedBy},{change.ProposedOn.ToShortDateString()},{change.Assessment},{change.AssessedBy},{(change.AssessedOn.HasValue ? change.AssessedOn.Value.ToShortDateString() : String.Empty)},{change.AcceptedBy},{(change.AcceptedOn.HasValue ? change.AcceptedOn.Value.ToShortDateString() : String.Empty)},{change.Implementation},{change.ImplementedBy},{(change.ImplementedOn.HasValue ? change.ImplementedOn.Value.ToShortDateString() : String.Empty)},{change.ApprovedBy},{(change.ApprovedOn.HasValue ? change.ApprovedOn.Value.ToShortDateString() : String.Empty)}");

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Change.csv");
        }

        public ActionResult GetDocuments()
        {
            var changes = _context.Change.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(ChangeStatus)).Cast<ChangeStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (changes != null && changes.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = changes.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        private bool ChangeExists(int id)
        {
            return (_context.Change?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
