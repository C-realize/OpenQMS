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
using Microsoft.Reporting.NETCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.Navigation;
using OpenQMS.Models.ViewModels;

namespace OpenQMS.Controllers
{
    public class TrainingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        //private readonly IAuthorizationService _authorizationService;

        public TrainingsController
        (
            ApplicationDbContext context, 
            UserManager<AppUser> userManager, 
            RoleManager<AppRole> roleManager
            //IAuthorizationService authorizationService
        )
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            //_authorizationService = authorizationService;
        }

        // GET: Trainings
        public async Task<IActionResult> Index()
        {
            var trainings = await _context.Training
                .Include(t => t.Trainees)
                .ToListAsync();

            // Only assigned trainings are shown UNLESS you're an administrator or manager.
            var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                               User.IsInRole(Constants.AdministratorsRole);
            if (!isAuthorized)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userTrainings = await _context.UserTraining.Where(x => x.TraineeId == currentUser.Id).ToListAsync();
                var assignedTrainings = new List<Training>();

                foreach (var training in userTrainings)
                {
                    Training assignedTraining = await _context.Training.FirstOrDefaultAsync(m => m.Id == training.TrainingId);
                    assignedTrainings.Add(assignedTraining);
                }

                return View(assignedTrainings);
            }
            else
            {
                return View(trainings);
            }
        }

        // GET: Trainings/Details/5
        public async Task<IActionResult> Details(int? id, bool isUserVerified = true)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentTraining = await _context.Training
                .Include(x => x.CompletedByUser)
                .Include(s => s.Trainees).ThenInclude(t => t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (currentTraining == null)
            {
                return NotFound();
            }

            var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                   User.IsInRole(Constants.AdministratorsRole);
            if (!isAuthorized)
            {
                bool userIsTrainee = false;
                foreach (var trainee in currentTraining.Trainees)
                {
                    if (trainee.Trainee.UserName == _userManager.GetUserName(User))
                    {
                        userIsTrainee = true;
                    }
                }
                if (userIsTrainee)
                {
                    return View(currentTraining);
                }
                else
                {
                    return Forbid();
                }
            }
            else
            {
                if (!isUserVerified)
                {
                    ModelState.AddModelError("", "Username or password is incorrect");
                }

                return View(currentTraining);
            }
        }

        // GET: Trainings/Create
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create()
        {
            var documents = from d in _context.AppDocument
                            select d;
            documents = documents.Where(d => d.Status == AppDocument.DocumentStatus.Approved);

            ViewData["Policies"] = documents.ToList();
            ViewData["Trainer"] = await _userManager.GetUsersInRoleAsync("Manager");
            ViewData["Roles"] = _roleManager.Roles.ToList();

            return View();
        }

        // POST: Trainings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create([Bind("Id,TrainingId,Name,Description,Date,PolicyId,PolicyTitle,TrainerId,TrainerEmail,Trainees")] Training training, string roles)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lastTraining = await _context.Training.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    var prevId = 0;

                    if (lastTraining != null && !string.IsNullOrEmpty(lastTraining.TrainingId))
                    {
                        string[] prevTrainingId = lastTraining.TrainingId.Split('-');
                        prevId = int.Parse(prevTrainingId[1]);
                    }

                    prevId = prevId > 0 ? prevId + 1 : 1;
                    var policy = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == training.PolicyId);
                    var trainer = await _userManager.FindByIdAsync(training.TrainerId);
                    if (policy == null) { return NotFound(); }
                    training.TrainingId = $"TRN-{prevId.ToString().PadLeft(2, '0')}";
                    training.Status = Training.TrainingStatus.Scheduled;
                    training.PolicyTitle = policy.Title;
                    training.TrainerEmail = trainer.Email;
                    training.Trainees = new List<UserTraining>();

                    var usersInRole = await _userManager.GetUsersInRoleAsync(roles);
                    foreach (var user in usersInRole)
                    {
                        var userToAdd = new UserTraining
                        {
                            TraineeId = user.Id,
                            TrainingId = training.Id,
                        };
                        training.Trainees.Add(userToAdd);
                    }

                    _context.Add(training);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(training);
        }

        // GET: Trainings/Edit/5
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var training = await _context.Training
                .Include(t => t.Trainees).ThenInclude(t => t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (training == null)
            {
                return NotFound();
            }

            if (training.Status == Training.TrainingStatus.Completed)
            {
                return Forbid();
            }

            ViewData["Policies"] = _context.AppDocument.ToList();
            ViewData["Trainer"] = _userManager.Users.ToList();

            PopulateAssignedTrainees(training);
            return View(training);
        }

        private void PopulateAssignedTrainees(Training training)
        {
            var allUsers = _userManager.Users;
            var assignedTrainees = new HashSet<int>(training.Trainees.Select(t => t.TraineeId));
            var viewModel = new List<AssignedTrainees>();
            foreach (var user in allUsers)
            {
                viewModel.Add(new AssignedTrainees
                {
                    UserID = user.Id,
                    Email = user.Email,
                    Assigned = assignedTrainees.Contains(user.Id)
                });
            }

            ViewData["Trainees"] = viewModel;
        }

        // POST: Trainings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int? id, string[] selectedTrainees)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var trainingToUpdate = await _context.Training
                .Include(t => t.Trainees)
                    .ThenInclude(t => t.Trainee)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trainingToUpdate == null) { return NotFound(); }

            if (await TryUpdateModelAsync<Training>(trainingToUpdate,
                "",
                t => t.Name, t => t.Description, t => t.Date, t => t.PolicyId, t => t.PolicyTitle, t => t.TrainerId, t => t.TrainerEmail))
            {
                if (trainingToUpdate.Status != Training.TrainingStatus.Completed) 
                {
                    UpdateAssignedTrainees(selectedTrainees, trainingToUpdate);
                    try
                    {
                        var policy = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == trainingToUpdate.PolicyId);
                        var trainer = await _userManager.FindByIdAsync(trainingToUpdate.TrainerId);
                        if (policy == null) { return NotFound(); }
                        trainingToUpdate.PolicyTitle = policy.Title;
                        trainingToUpdate.TrainerEmail = trainer.Email;
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException /* ex */)
                    {
                        //Log the error (uncomment ex variable name and write a log.)
                        ModelState.AddModelError("", "Unable to save changes. " +
                            "Try again, and if the problem persists, " +
                            "see your system administrator.");
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return Forbid();
                }
            }

            return View(trainingToUpdate);
        }

        private void UpdateAssignedTrainees(string[] selectedTrainees, Training trainingToUpdate)
        {
            if (selectedTrainees == null)
            {
                trainingToUpdate.Trainees = new List<UserTraining>();
                return;
            }

            var selectedTraineesHS = new HashSet<string>(selectedTrainees);
            var assignedTraineesHS = new HashSet<int>
                (trainingToUpdate.Trainees.Select(t => t.TraineeId));
            foreach (var user in _userManager.Users)
            {
                if (selectedTraineesHS.Contains(user.Id.ToString()))
                {
                    if (!assignedTraineesHS.Contains(user.Id))
                    {
                        trainingToUpdate.Trainees.Add(new UserTraining { TrainingId = trainingToUpdate.Id, TraineeId = user.Id});
                    }
                }
                else
                {
                    if (assignedTraineesHS.Contains(user.Id))
                    {
                        UserTraining traineeToRemove = trainingToUpdate.Trainees.FirstOrDefault(t => t.TraineeId == user.Id);
                        trainingToUpdate.Trainees.Remove(traineeToRemove);
                    }
                }
            }
        }

        // GET: Trainings/Delete/5
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var training = await _context.Training
                .Include(s => s.Trainees).ThenInclude(t => t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (training == null)
            {
                return NotFound();
            }

            if (training.Status == Training.TrainingStatus.Completed)
            {
                return Forbid();
            }

            return View(training);
        }

        // POST: Trainings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var training = await _context.Training.FindAsync(id);
            if (training == null)
            {
                return NotFound();
            }

            if (training.Status != Training.TrainingStatus.Completed)
            {
                _context.Training.Remove(training);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return Forbid();
            }
        }

        public async Task<IActionResult> Complete(int id, string email, string pwd)
        {
            bool isUserVerified;
            AppUser user = new AppUser();
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
                        var training = await _context.Training.FirstOrDefaultAsync(x => x.Id == id);

                        var isAuthorized = User.IsInRole(Constants.ManagersRole) || User.IsInRole(Constants.AdministratorsRole);
                        if (!isAuthorized)
                        {
                            return Forbid();
                        }

                        training.Status = Training.TrainingStatus.Completed;
                        training.CompletedBy = Convert.ToInt32(_userManager.GetUserId(User));
                        training.CompletedOn = DateTime.Now;
                        _context.Entry(training).State = EntityState.Modified;
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
                return Redirect("/Trainings/Details?id=" + id.ToString() + "&isUserVerified=" + isUserVerified.ToString());
            }
        }

        public ActionResult GetDocuments()
        {
            var trainings = _context.Training.ToList();
            var dataSets = new List<PoliciesChartViewModel>();
            var statusList = Enum.GetValues(typeof(Training.TrainingStatus)).Cast<Training.TrainingStatus>();
            var labels = new List<string>();
            var random = new Random();
            var dataSet = new PoliciesChartViewModel();

            if (trainings != null && trainings.Count > 0)
            {
                foreach (var label in statusList)
                {
                    labels.Add(label.ToString());
                    var statusCount = trainings.Where(x => x.Status.Equals(label)).Count();
                    dataSet.Data.Add(statusCount);
                    dataSet.BackgroundColor.Add(String.Format("#{0:X6}", random.Next(0x1000000)));
                }
            }

            dataSets.Add(dataSet);

            return Json(new { dataSet = dataSets, labels });
        }

        public async Task<IActionResult> ExportTrainingCertificate(int id)
        {
            string path = Directory.GetCurrentDirectory() + "\\Reports\\TrainingCertificateReport.rdl";
            var training = await _context.Training
                .Include(x => x.CompletedByUser)
                .Include(s => s.Trainees).ThenInclude(t => t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            var trainee = await _userManager.GetUserAsync(User);

            bool userIsTrainee = false;
            foreach (var user in training.Trainees)
            {
                if (user.Trainee.UserName == trainee.UserName)
                {
                    userIsTrainee = true;
                }
            }
            if (!userIsTrainee)
            {
                return Forbid();
            }

            if (training != null /*&& string.IsNullOrEmpty(training.ExportFilePath)*/)
            {
                var trainingDetails = new List<Training> { training };
                var traineeDetails = new List<AppUser> { trainee };
                List<ReportParameter> parameters = new List<ReportParameter>();
                parameters.Add(new ReportParameter("UserName", trainee.UserName));
                parameters.Add(new ReportParameter("FirstName", trainee.FirstName));
                parameters.Add(new ReportParameter("LastName", trainee.LastName));
                parameters.Add(new ReportParameter("Email", trainee.Email));
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Training", trainingDetails));
                localReport.DataSources.Add(new ReportDataSource("Trainee", traineeDetails));
                localReport.SetParameters(parameters);
                var result = localReport.Render("PDF");

                //**Export details as signed pdf**
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
                //if (product.Status == ProductStatus.Approved || product.Status == ProductStatus.Obsolete)
                //{
                //    product.ExportFilePath = Helper.SignPDF(product.ExportFilePath);
                //}

                return File(result, "application/pdf", "TrainingDetail.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ExportTrainingAttendance(int id)
        {
            string path = Directory.GetCurrentDirectory() + "\\Reports\\TrainingAttendanceReport.rdl";
            var training = await _context.Training
                .Include(x => x.CompletedByUser)
                .Include(s => s.Trainees).ThenInclude(t => t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (training != null /*&& string.IsNullOrEmpty(training.ExportFilePath)*/)
            {
                var trainingDetails = new List<Training> { training };
                var attendanceList = new List<AppUser>();
                foreach (var user in training.Trainees)
                {
                    attendanceList.Add(user.Trainee);
                }
                LocalReport localReport = new LocalReport();
                localReport.ReportPath = path;
                localReport.DataSources.Add(new ReportDataSource("Training", trainingDetails));
                localReport.DataSources.Add(new ReportDataSource("Trainee", attendanceList));
                var result = localReport.Render("PDF");

                //**Export details as signed pdf**
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
                //if (product.Status == ProductStatus.Approved || product.Status == ProductStatus.Obsolete)
                //{
                //    product.ExportFilePath = Helper.SignPDF(product.ExportFilePath);
                //}

                return File(result, "application/pdf", "TrainingDetail.pdf");
            }
            else
            {
                return NotFound();
            }
        }

        private bool TrainingExists(int id)
        {
            return _context.Training.Any(e => e.Id == id);
        }
    }
}
