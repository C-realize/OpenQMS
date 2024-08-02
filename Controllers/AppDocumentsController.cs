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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
using OpenQMS.Services;

//**DocumentFormat.OpenXml nuget package**
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Wordprocessing;

//**Microsoft Word 16.0 Object Library COM reference**
//using Word = Microsoft.Office.Interop.Word;

namespace OpenQMS.Controllers
{
    public class AppDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAuthorizationService _authorizationService;

        public AppDocumentsController
        (
            ApplicationDbContext context, 
            UserManager<AppUser> userManager, 
            SignInManager<AppUser> signInManager, 
            IAuthorizationService authorizationService
        )
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _authorizationService = authorizationService;
        }

        // GET: Documents
        public async Task<IActionResult> Index()
        {
            var documents = from d in _context.AppDocument
                            select d;

            var isAuthorized = User.IsInRole(Constants.ManagersRole)
                            || User.IsInRole(Constants.AdministratorsRole);
            if (!isAuthorized)
            {
                var currentUserName = _userManager.GetUserName(User);
                documents = documents.Where(d => d.Status == AppDocument.DocumentStatus.Approved
                                              || d.AuthoredBy == currentUserName);
            }

            return View(await documents.ToListAsync());
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

            //Dowload from filesystem
            //var memory = new MemoryStream();
            //using (var stream = new FileStream(document.FilePath, FileMode.Open))
            //{
            //    await stream.CopyToAsync(memory);
            //}
            //memory.Position = 0;

            return File(document.Content, /*memory,*/ document.FileType, document.Title + document.FileExtension);
        }

        // POST: Documents/Details/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, AppDocument.DocumentStatus status, string InputEmail, string InputPassword)
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
                if (!(result.Succeeded || result.RequiresTwoFactor))
                {
                    return Forbid();
                }

                documentPdf.Version = Math.Ceiling(document.Version);
                documentPdf.ApprovedBy = _userManager.GetUserName(User);
                documentPdf.ApprovedOn = DateTime.Now;
                documentPdf.Status = AppDocument.DocumentStatus.Approved;

                if (documentPdf.FileExtension.Equals(".pdf") /*&& !string.IsNullOrEmpty(documentPdf.FilePath)*/)
                {
                    documentPdf./*FilePath*/Content = Helper.SignPDF(documentPdf./*FilePath*/Content);
                }

                if (documentPdf.GeneratedFrom != null && !string.IsNullOrEmpty(documentPdf.GeneratedFrom))
                {
                    var oldDocument = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id.ToString().Equals(documentPdf.GeneratedFrom));

                    oldDocument.Status = AppDocument.DocumentStatus.Obsolete;
                    _context.AppDocument.Update(oldDocument);
                }

                //**Conversion of a Word document to PDF with the addition of a footer.**

                //string strDoc = @"C:\Users\Hao\Downloads\repos\temp\" + document.Title;
                //MemoryStream stream = new MemoryStream();
                //stream.Write(document.Content, 0, document.Content.Length);
                //string txt = "Approved in OpenQMS.net by " + documentPdf.ApprovedBy + " on " + documentPdf.ApprovedOn;
                //OpenAndAddToWordprocessingStream(stream, txt, strDoc);
                //stream.Close();
                //string input = strDoc + ".docx";
                //string output = strDoc + ".pdf";
                //WaitForFile(input);

                //ConvertWordToSpecifiedFormat(input, output, Word.WdSaveFormat.wdFormatPDF);
                //WaitForFile(output);

                //OpenAndSaveToWordprocessingStream(output, documentPdf);
                //stream.Close();
                //WaitForFile(input);

                //System.IO.File.Delete(input);
                //System.IO.File.Delete(output);

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

        //private static void OpenAndAddToWordprocessingStream(Stream stream, string txt, string strDoc)
        //{
        //    // Open a WordProcessingDocument based on a stream.
        //    WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(stream, true);
        //    // Assign a reference to the existing document body.
        //    MainDocumentPart mainDocumentPart = wordprocessingDocument.MainDocumentPart;
        //    FooterPart footerPart = mainDocumentPart.FooterParts.Last();
        //    // Add new text.
        //    Paragraph paragraph1 = new Paragraph() { };
        //    Run run1 = new Run();
        //    Text text1 = new Text();
        //    text1.Text = txt;

        //    run1.Append(text1);
        //    paragraph1.Append(run1);
        //    footerPart.Footer.Append(paragraph1);

        //    wordprocessingDocument.SaveAs(strDoc + ".docx").Dispose();

        //    // Close the document handle.
        //    wordprocessingDocument.Close();

        //    // Caller must close the stream.
        //}

        //private static void ConvertWordToSpecifiedFormat(object input, object output, object format)
        //{
        //    Word._Application application = new Word.Application();
        //    application.Visible = false;
        //    object missing = Missing.Value;
        //    object isVisible = false;
        //    object readOnly = false;
        //    Word._Document document = application.Documents.Open(ref input, ref missing, ref readOnly, ref missing, ref missing, ref missing, ref missing,
        //                            ref missing, ref missing, ref missing, ref missing, ref isVisible, ref missing, ref missing, ref missing, ref missing);
        //    document.Activate();
        //    document.SaveAs(ref output, ref format, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing,
        //                    ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
        //    document.Save();
        //    application.Quit(ref missing, ref missing, ref missing);
        //}

        //private static async void WaitForFile(string doc)
        //{
        //    //This will lock the execution until the file is ready
        //    //TODO: Add some logic to make it async and cancelable
        //    while (!IsFileReady(doc)) 
        //    {
        //        Thread.Sleep(1000);
        //    }
        //}

        //private static bool IsFileReady(string doc)
        //{
        //    // If the file can be opened for exclusive access it means that the file
        //    // is no longer locked by another process.
        //    try
        //    {
        //        using (FileStream file = new FileStream(doc, FileMode.Open, FileAccess.Read))
        //            return file.Length > 0;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //private static void OpenAndSaveToWordprocessingStream(string output, AppDocument documentPdf)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    using (FileStream file = new FileStream(output, FileMode.Open, FileAccess.Read))
        //    {
        //        byte[] bytes = new byte[file.Length];
        //        file.Read(bytes, 0, (int)file.Length);
        //        ms.Write(bytes, 0, (int)file.Length);
        //        documentPdf.Content = ms.ToArray();
        //        documentPdf.FileType = "application/pdf";
        //        documentPdf.FileExtension = ".pdf";
        //    }
        //}

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
        public async Task<IActionResult> Create([Bind("Title,DocumentId,Description,Version,Content,FileType,FileExtension,Status,AuthoredBy,AuthoredOn,ApprovedBy,ApprovedOn")] AppDocument document, IFormFile file)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastdoc = _context.AppDocument.OrderByDescending(x => x.DocumentId).FirstOrDefault();
                    var prevId = 0;

                    if (lastdoc != null && !string.IsNullOrEmpty(lastdoc.DocumentId))
                    {
                        string[] prevDocId = lastdoc.DocumentId.Split('-');
                        prevId = int.Parse(prevDocId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    document.DocumentId = $"Doc-{prevId.ToString().PadLeft(2, '0')}";
                    document.Version = Convert.ToDecimal(0.01);
                    document.IsLocked = false;
                    document.AuthoredBy = _userManager.GetUserName(User);
                    document.AuthoredOn = DateTime.Now;
                    document.FileType = file.ContentType;
                    document.FileExtension = Path.GetExtension(file.FileName);

                    //**Upload file to filesystem**
                    //var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                    //bool basePathExists = System.IO.Directory.Exists(basePath);
                    //if (!basePathExists) Directory.CreateDirectory(basePath);
                    //var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    //var filePath = Path.Combine(basePath, file.FileName);
                    //if (!System.IO.File.Exists(filePath))
                    //{
                    //    using (var stream = new FileStream(filePath, FileMode.Create))
                    //    {
                    //        await file.CopyToAsync(stream);
                    //    }
                    //    document.FilePath = filePath;
                    //}

                    //**Upload file to database**
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);

                        //Upload the file if less than 2 MB(recommendation)
                        if (memoryStream.Length < 2097152)
                        {
                            document.Content = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("File", "The file is too large.");
                        }
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,DocumentId,Title,Description,Version,Content,FileType,FileExtension,AuthoredBy,AuthoredOn,ApprovedBy,ApprovedOn,GeneratedFrom,IsLocked,Status")] AppDocument document, IFormFile file)
        {
            if (id != document.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (document.IsLocked)
                    {
                        return Forbid();
                    }

                    document.AuthoredBy = _userManager.GetUserName(User);
                    document.AuthoredOn = DateTime.Now;
                    document.Version += Convert.ToDecimal(0.01);
                    document.FileType = file.ContentType;
                    document.FileExtension = Path.GetExtension(file.FileName);

                    //**Upload file to filesystem**
                    //var basePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Files\\");
                    //bool basePathExists = System.IO.Directory.Exists(basePath);
                    //if (!basePathExists) Directory.CreateDirectory(basePath);
                    //var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    //var filePath = Path.Combine(basePath, file.FileName);
                    //if (!System.IO.File.Exists(filePath))
                    //{
                    //    using (var stream = new FileStream(filePath, FileMode.Create))
                    //    {
                    //        await file.CopyToAsync(stream);
                    //    }
                    //    document.FilePath = filePath;
                    //}

                    //**Upload file to database * *
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);

                        //Upload the file if less than 2 MB(recommendation)
                        if (memoryStream.Length < 2097152)
                        {
                            document.Content = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("File", "The file is too large.");
                        }
                    }

                    var isAuthorized = await _authorizationService.AuthorizeAsync(User, document, DocumentOperations.Update);
                    if (!isAuthorized.Succeeded)
                    {
                        return Forbid();
                    }

                    if (document.Status == AppDocument.DocumentStatus.Approved)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT AppDocument ON;");
                            document.Id = _context.AppDocument.Max(m => m.Id) + 1;
                            document.Status = AppDocument.DocumentStatus.Draft;
                            document.GeneratedFrom = id.ToString();
                            document.IsLocked = false;
                            _context.Add(document);
                            _context.SaveChanges();
                            _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT AppDocument OFF;");
                            transaction.Commit();
                        }
                        AppDocument document1 = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == id);
                        document1.IsLocked = true;
                        _context.Update(document1);
                        await _context.SaveChangesAsync();
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

            if (document.Status != AppDocument.DocumentStatus.Approved && document.Status != AppDocument.DocumentStatus.Obsolete)
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

        public ActionResult GetDocuments()
        {
            var appDocuments = _context.AppDocument.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(AppDocument.DocumentStatus)).Cast<AppDocument.DocumentStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (appDocuments != null && appDocuments.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = appDocuments.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public ActionResult GetDocumentsForBarchart()
        {
            var appDocuments = _context.AppDocument.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();
            var dates = appDocuments.Select(x => x.AuthoredOn.Date).Distinct().ToList();

            if (appDocuments != null && appDocuments.Count > 0)
            {
                foreach (var date in dates)
                {
                    labels.Add(date.ToShortDateString());
                    var docCount = appDocuments.Where(x => x.AuthoredOn.Date == date).Count();
                    dataSet.Data.Add(docCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        private bool DocumentExists(int id)
        {
            return _context.AppDocument.Any(e => e.Id == id);
        }
    }
}
