using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Admin only: manage one student's enrollments, violations, and completed requirements.
// The student's id is the {id} in the URL, e.g. /Students/Details/abc123.
[Authorize(Roles = AppRoles.Admin)]
public class StudentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Finds the student, or null if the id is wrong or belongs to a non-student.
    private async Task<ApplicationUser?> FindStudentAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, AppRoles.Student)) return null;
        return user;
    }

    public async Task<IActionResult> Details(string id)
    {
        var student = await FindStudentAsync(id);
        if (student == null) return NotFound();

        var enrolled = await _db.Enrollments
            .Where(e => e.StudentId == id)
            .Select(e => e.Course!)
            .OrderBy(c => c.Code)
            .ToListAsync();

        var notEnrolled = await _db.Courses
            .Where(c => !_db.Enrollments.Any(e => e.CourseId == c.Id && e.StudentId == id))
            .OrderBy(c => c.Code)
            .ToListAsync();

        var violations = await _db.Violations
            .Where(v => v.StudentId == id)
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        var requirements = await _db.Requirements.OrderBy(r => r.Kind).ThenBy(r => r.Id).ToListAsync();
        var doneIds = await _db.CompletedRequirements
            .Where(c => c.StudentId == id)
            .Select(c => c.RequirementId)
            .ToListAsync();

        var model = new StudentDetailViewModel
        {
            Student = student,
            Enrolled = enrolled,
            NotEnrolled = notEnrolled,
            Violations = violations,
            Requirements = requirements
                .Select(r => new RequirementRow { Requirement = r, IsDone = doneIds.Contains(r.Id) })
                .ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(string id, int courseId)
    {
        if (await FindStudentAsync(id) == null) return NotFound();

        bool courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        bool alreadyIn = await _db.Enrollments.AnyAsync(e => e.StudentId == id && e.CourseId == courseId);
        if (courseExists && !alreadyIn)
        {
            _db.Enrollments.Add(new Enrollment { StudentId = id, CourseId = courseId });
            await _db.SaveChangesAsync();
            TempData["Message"] = "Student enrolled.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddViolation(string id, string title = "", string description = "")
    {
        if (await FindStudentAsync(id) == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "A violation needs a title.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _db.Violations.Add(new Violation
        {
            StudentId = id,
            Title = title.Trim(),
            Description = description.Trim(),
            Date = DateTime.Today
        });
        await _db.SaveChangesAsync();

        TempData["Message"] = "Violation recorded.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ResolveViolation(string id, int violationId)
    {
        // Match on BOTH ids so one student's page cannot change another student's violation.
        var violation = await _db.Violations.FirstOrDefaultAsync(v => v.Id == violationId && v.StudentId == id);
        if (violation == null) return NotFound();

        violation.IsResolved = true;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Violation resolved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Marks a requirement done (adds a CompletedRequirement row) or missing again (removes it).
    [HttpPost]
    public async Task<IActionResult> SetRequirement(string id, int requirementId, bool done)
    {
        if (await FindStudentAsync(id) == null) return NotFound();
        if (!await _db.Requirements.AnyAsync(r => r.Id == requirementId)) return NotFound();

        var existing = await _db.CompletedRequirements
            .FirstOrDefaultAsync(c => c.StudentId == id && c.RequirementId == requirementId);

        if (done && existing == null)
        {
            _db.CompletedRequirements.Add(new CompletedRequirement
            {
                StudentId = id,
                RequirementId = requirementId,
                DateCompleted = DateTime.Today
            });
        }
        else if (!done && existing != null)
        {
            _db.CompletedRequirements.Remove(existing);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}
