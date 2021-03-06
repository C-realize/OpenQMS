#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Word = Microsoft.Office.Interop.Word;

namespace OpenQMS.Controllers
{
    public class AppDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAuthorizationService _authorizationService;

        public AppDocumentsController(ApplicationDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IAuthorizationService authorizationService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _authorizationService = authorizationService;
        }

        // GET: Documents
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["ApproveDateSortParm"] = sortOrder == "ApproveDate" ? "approvedate_desc" : "ApproveDate";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var documents = from d in _context.AppDocument
                            select d;
            if (!String.IsNullOrEmpty(searchString))
            {
                documents = documents.Where(d => d.Title.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    documents = documents.OrderByDescending(d => d.Title);
                    break;
                case "Date":
                    documents = documents.OrderBy(d => d.AuthoredOn);
                    break;
                case "date_desc":
                    documents = documents.OrderByDescending(d => d.AuthoredOn);
                    break;
                case "ApproveDate":
                    documents = documents.OrderBy(d => d.ApprovedOn);
                    break;
                case "approvedate_desc":
                    documents = documents.OrderByDescending(d => d.ApprovedOn);
                    break;
                default:
                    documents = documents.OrderBy(d => d.Title);
                    break;
            }

            var currentUserName = _userManager.GetUserName(User);
            var isAuthorized = User.IsInRole(Constants.ManagersRole) 
                            || User.IsInRole(Constants.AdministratorsRole);
            if (!isAuthorized)
            {
                documents = documents.Where(d => d.Status == AppDocument.DocumentStatus.Approved
                                              || d.AuthoredBy == currentUserName);
            }

            int pageSize = 5;
            return View(await PaginatedList<AppDocument>.CreateAsync(documents.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Documents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == id);
            if (document == null)
            {
                return NotFound();
            }

            var isAuthorized = User.IsInRole(Constants.ManagersRole) 
                            || User.IsInRole(Constants.AdministratorsRole);
            var currentUserName = _userManager.GetUserName(User);
            if (!isAuthorized
                && currentUserName != document.AuthoredBy
                && document.Status != AppDocument.DocumentStatus.Approved)
            {
                return Forbid();
            }

            return View(document);
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var document = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == id);
            if (document == null)
            {
                return NotFound();
            }

            return File(document.Content, document.FileType, document.Title + document.FileExtension);
        }

        // POST: Documents/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, AppDocument.DocumentStatus status, string InputEmail, String InputPassword)
        {
            var document = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == id);
            var documentPdf = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == id);
            if (document == null)
            {
                return NotFound();
            }

            var documentOperation = (status == AppDocument.DocumentStatus.Approved)
                                                        ? DocumentOperations.Approve
                                                        : DocumentOperations.Reject;
            var isAuthorized = await _authorizationService.AuthorizeAsync(User, document, documentOperation);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            if(documentOperation==DocumentOperations.Approve)
            {
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

                documentPdf.Version = Math.Ceiling(document.Version);
                documentPdf.ApprovedBy = _userManager.GetUserName(User);
                documentPdf.ApprovedOn = DateTime.Now;
                documentPdf.Status = AppDocument.DocumentStatus.Approved;

                string strDoc = @"C:\Users\Hao\Downloads\repos\temp\" + document.Title;
                MemoryStream stream = new MemoryStream();
                stream.Write(document.Content, 0, document.Content.Length);
                string txt = "Approved in OpenQMS.net by " + documentPdf.ApprovedBy + " on " + documentPdf.ApprovedOn;
                OpenAndAddToWordprocessingStream(stream, txt, strDoc);
                stream.Close();
                string input = strDoc + ".docx";
                string output = strDoc + ".pdf";
                WaitForFile(input);

                ConvertWordToSpecifiedFormat(input, output, Word.WdSaveFormat.wdFormatPDF);
                WaitForFile(output);

                OpenAndSaveToWordprocessingStream(output, documentPdf);
                stream.Close();
                WaitForFile(input);

                System.IO.File.Delete(input);
                System.IO.File.Delete(output);

                _context.AppDocument.Update(documentPdf);
            }
            else
            {
                document.Status = status;
                _context.AppDocument.Update(document);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static void OpenAndAddToWordprocessingStream(Stream stream, string txt, string strDoc)
        {
            // Open a WordProcessingDocument based on a stream.
            WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(stream, true);
            // Assign a reference to the existing document body.
            MainDocumentPart mainDocumentPart = wordprocessingDocument.MainDocumentPart;
            FooterPart footerPart = mainDocumentPart.FooterParts.Last();
            // Add new text.
            Paragraph paragraph1 = new Paragraph() { };
            Run run1 = new Run();
            Text text1 = new Text();
            text1.Text = txt;

            run1.Append(text1);
            paragraph1.Append(run1);
            footerPart.Footer.Append(paragraph1);

            wordprocessingDocument.SaveAs(strDoc + ".docx").Dispose();

            // Close the document handle.
            wordprocessingDocument.Close();

            // Caller must close the stream.
        }

        private static void ConvertWordToSpecifiedFormat(object input, object output, object format)
        {
            Word._Application application = new Word.Application();
            application.Visible = false;
            object missing = Missing.Value;
            object isVisible = false;
            object readOnly = false;
            Word._Document document = application.Documents.Open(ref input, ref missing, ref readOnly, ref missing, ref missing, ref missing, ref missing,
                                    ref missing, ref missing, ref missing, ref missing, ref isVisible, ref missing, ref missing, ref missing, ref missing);
            document.Activate();
            document.SaveAs(ref output, ref format, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing,
                            ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
            document.Save();
            application.Quit(ref missing, ref missing, ref missing);
        }

        private static async void WaitForFile(string doc)
        {
            //This will lock the execution until the file is ready
            //TODO: Add some logic to make it async and cancelable
            while (!IsFileReady(doc)) 
            {
                Thread.Sleep(1000);
            }
        }

        private static bool IsFileReady(string doc)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream file = new FileStream(doc, FileMode.Open, FileAccess.Read))
                    return file.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void OpenAndSaveToWordprocessingStream(string output, AppDocument documentPdf)
        {
            using (MemoryStream ms = new MemoryStream())
            using (FileStream file = new FileStream(output, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                ms.Write(bytes, 0, (int)file.Length);
                documentPdf.Content = ms.ToArray();
                documentPdf.FileType = "application/pdf";
                documentPdf.FileExtension = ".pdf";
            }
        }

        // GET: Documents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Documents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Version,Content,FileType,FileExtension,Status,AuthoredBy,AuthoredOn,ApprovedBy,ApprovedOn")] AppDocument document, IFormFile file)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    document.Version = 0.01;
                    document.IsLocked = false;
                    document.AuthoredBy = _userManager.GetUserName(User);
                    document.AuthoredOn = DateTime.Now;
                    document.FileType = file.ContentType;
                    document.FileExtension = Path.GetExtension(file.FileName);
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        document.Content = memoryStream.ToArray();
                    }

                    var isAuthorized = await _authorizationService.AuthorizeAsync(User, document, DocumentOperations.Create);
                    if (!isAuthorized.Succeeded)
                    {
                        return Forbid();
                    }

                    _context.AppDocument.Add(document);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(document);
        }

        // GET: Documents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.AppDocument.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, document, DocumentOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return View(document);
        }

        // POST: Documents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Version,Content,FileType,FileExtension,Status,AuthoredBy,AuthoredOn,ApprovedBy,ApprovedOn,IsLocked")] AppDocument document, IFormFile file)
        {
            if (id != document.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    document.AuthoredBy = _userManager.GetUserName(User);
                    document.AuthoredOn = DateTime.Now;
                    document.Version += 0.01;
                    document.FileType = file.ContentType;
                    document.FileExtension = Path.GetExtension(file.FileName);
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);

                        // Upload the file if less than 2 MB
                        //if (memoryStream.Length < 2097152)
                        //{
                            document.Content = memoryStream.ToArray();
                        //}
                        //else
                        //{
                        //    ModelState.AddModelError("File", "The file is too large.");
                        //}
                    }

                    var isAuthorized = await _authorizationService.AuthorizeAsync(User, document, DocumentOperations.Update);
                    if (!isAuthorized.Succeeded)
                    {
                        return Forbid();
                    }

                    if (document.Status == AppDocument.DocumentStatus.Approved)
                    {
                        if (document.IsLocked)
                        {
                            return Forbid();
                        }
                            using (var transaction = _context.Database.BeginTransaction())
                            {
                                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT AppDocument ON;");
                                document.Id = _context.AppDocument.Max(m => m.Id) + 1;
                                document.Status = AppDocument.DocumentStatus.Draft;
                                document.IsLocked = false;
                                _context.Add(document);
                                _context.SaveChanges();
                                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT AppDocument OFF;");
                                transaction.Commit();
                            }
                            AppDocument document1 = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == id);
                            document1.IsLocked = true;
                            _context.Update(document1);
                            _context.SaveChanges();
                    }
                    else
                    {
                        document.Status = AppDocument.DocumentStatus.Draft;
                        _context.Update(document);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentExists(document.Id))
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
            return View(document);
        }

        // GET: Documents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.AppDocument
                .FirstOrDefaultAsync(m => m.Id == id);
            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.AppDocument.FindAsync(id);
            if (document.Status != AppDocument.DocumentStatus.Approved)
            {
                _context.AppDocument.Remove(document);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return Forbid();
            }
        }

        private bool DocumentExists(int id)
        {
            return _context.AppDocument.Any(e => e.Id == id);
        }
    }
}
