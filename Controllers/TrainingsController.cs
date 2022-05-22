#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public TrainingsController(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Trainings
        public async Task<IActionResult> Index(string sortOrder, string searchString)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            ViewData["CurrentFilter"] = searchString;

            var trainings = _context.Training
                .Include(t => t.Trainees)
                .AsNoTracking();

            if (!String.IsNullOrEmpty(searchString))
            {
                trainings = trainings.Where(t => t.Name.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "name_desc":
                    trainings = trainings.OrderByDescending(t => t.Name);
                    break;
                case "Date":
                    trainings = trainings.OrderBy(t => t.Date);
                    break;
                case "date_desc":
                    trainings = trainings.OrderByDescending(t => t.Date);
                    break;
                default:
                    trainings = trainings.OrderBy(t => t.Name);
                    break;
            }

            // Only assigned trainings are shown UNLESS you're an administrator or manager.
            var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                               User.IsInRole(Constants.AdministratorsRole);
            if (!isAuthorized)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userTrainings = _context.UserTraining.Where(x => x.TraineeId == currentUser.Id);
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
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var training = await _context.Training
                .Include(s=>s.Trainees).ThenInclude(t => t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (training == null)
            {
                return NotFound();
            }

            return View(training);
        }

        // GET: Trainings/Create
        public async Task<IActionResult> CreateAsync()
        {
            var documents = from d in _context.AppDocument
                            select d;
            documents = documents.Where(d => d.Status == AppDocument.DocumentStatus.Approved);
            ViewData["Policies"] = documents.ToList();
            ViewData["Trainer"] = await _userManager.GetUsersInRoleAsync("Manager");
            ViewData["Roles"] = _roleManager.Roles.ToList();

            return View();
        }

        [Authorize(Roles = "Administrator,Manager")]
        // POST: Trainings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Date,PolicyId,PolicyTitle,TrainerId,TrainerEmail,Trainees")] Training training, string roles)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var policy = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == training.PolicyId);
                    var trainer = await _userManager.FindByIdAsync(training.TrainerId);
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var training = await _context.Training
                .Include(t => t.Trainees).ThenInclude(t=>t.Trainee)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (training == null)
            {
                return NotFound();
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

        [Authorize(Roles = "Administrator,Manager")]
        // POST: Trainings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            if (await TryUpdateModelAsync<Training>(trainingToUpdate,
                "",
                t => t.Name, t => t.Description, t => t.Date, t => t.PolicyId, t => t.PolicyTitle, t => t.TrainerId, t => t.TrainerEmail))
            {
                UpdateAssignedTrainees(selectedTrainees, trainingToUpdate);
                try
                {
                    var policy = await _context.AppDocument.FirstOrDefaultAsync(m => m.Id == trainingToUpdate.PolicyId);
                    var trainer = await _userManager.FindByIdAsync(trainingToUpdate.TrainerId);
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

            UpdateAssignedTrainees(selectedTrainees, trainingToUpdate);
            PopulateAssignedTrainees(trainingToUpdate);
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

            return View(training);
        }

        [Authorize(Roles = "Administrator,Manager")]
        // POST: Trainings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var training = await _context.Training.FindAsync(id);
            _context.Training.Remove(training);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrainingExists(int id)
        {
            return _context.Training.Any(e => e.Id == id);
        }
    }
}
