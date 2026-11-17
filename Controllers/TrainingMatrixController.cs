using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
using OpenQMS.Services;

namespace OpenQMS.Controllers;

[Authorize]
public class TrainingMatrixController : Controller
{
    private const string TrainingCoordinatorRole = "Training Coordinator";
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public TrainingMatrixController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = GetActorId();
        var canViewAll = User.IsInRole(Constants.AdministratorsRole)
            || User.IsInRole(TrainingCoordinatorRole);
        var assignments = _context.TrainingAssignments
            .Include(assignment => assignment.Trainee)
            .Include(assignment => assignment.DocumentRevision)
            .ThenInclude(revision => revision.ControlledDocument)
            .AsQueryable();

        if (!canViewAll)
        {
            assignments = assignments.Where(assignment => assignment.TraineeId == currentUserId);
        }

        var now = DateTime.UtcNow;
        var items = await assignments
            .OrderBy(assignment => assignment.DueAtUtc)
            .ThenBy(assignment => assignment.Trainee.LastName)
            .ToListAsync();
        var rows = items
            .Select(assignment => new TrainingAssignmentListItem
            {
                Assignment = assignment,
                TraineeName = assignment.Trainee.FirstName + " " + assignment.Trainee.LastName,
                DocumentNumber = assignment.DocumentRevision.ControlledDocument.DocumentId,
                DocumentTitle = assignment.DocumentRevision.ControlledDocument.Title,
                IsOverdue = TrainingAssignmentRules.IsOverdue(assignment, now)
            })
            .ToList();

        ViewData["CanViewAll"] = canViewAll;
        ViewData["CurrentUserId"] = currentUserId;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var assignment = await FindAssignmentAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        if (assignment.TraineeId != GetActorId())
        {
            return Forbid();
        }

        try
        {
            TrainingAssignmentRules.Start(assignment);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
        AddAudit(assignment, "TrainingStarted", GetActorId());
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, TrainingCompletionInput input)
    {
        var assignment = await FindAssignmentAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        if (assignment.TraineeId != GetActorId())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var actorId = GetActorId();
        try
        {
            TrainingAssignmentRules.Complete(assignment, actorId, input.CompletionEvidence, DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        AddAudit(assignment, "TrainingCompleted", actorId, input.CompletionEvidence.Trim());
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var assignment = await FindAssignmentAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        if (assignment.TraineeId != GetActorId())
        {
            return Forbid();
        }

        bool updated;
        try
        {
            updated = TrainingAssignmentRules.Acknowledge(assignment, DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }

        if (updated)
        {
            AddAudit(assignment, "TrainingAcknowledged", GetActorId());
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<TrainingAssignment?> FindAssignmentAsync(int id)
    {
        return _context.TrainingAssignments.FirstOrDefaultAsync(assignment => assignment.Id == id);
    }

    private int GetActorId()
    {
        return int.TryParse(_userManager.GetUserId(User), out var actorId)
            ? actorId
            : throw new InvalidOperationException("The signed-in user ID is invalid.");
    }

    private void AddAudit(TrainingAssignment assignment, string action, int actorId, string? details = null)
    {
        _context.QmsAuditEvents.Add(new QmsAuditEvent
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = nameof(TrainingAssignment),
            EntityId = assignment.Id.ToString(),
            Details = details
        });
    }
}
