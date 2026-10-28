using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Students pick their course once, then tick the subjects they take.
// Picked subjects show up in the Course Panel with dropout points automatically.
[Authorize(Roles = AppRoles.Student)]
public class SubjectsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubjectsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new SubjectPickerViewModel();

        // Step 1: no course yet, so show the course list.
        if (user.CourseId == null)
        {
            model.Courses = await _db.Courses.OrderBy(c => c.Code).ToListAsync();
            return View(model);
        }

        // Step 2: show the subjects of the student's course.
        model.Course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == user.CourseId);
        model.Subjects = await _db.Subjects
            .Include(s => s.Professor)
            .Where(s => s.CourseId == user.CourseId)
            .OrderBy(s => s.Code)
            .ToListAsync();
        model.EnrolledIds = await _db.Enrollments
            .Where(e => e.StudentId == user.Id)
            .Select(e => e.SubjectId)
            .ToListAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SetCourse(int courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // The course is chosen once. If it was a mistake, an admin can change it on the account page.
        if (user.CourseId != null)
        {
            TempData["Error"] = "Your course is already set. Ask an admin to change it.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _db.Courses.AnyAsync(c => c.Id == courseId)) return NotFound();

        user.CourseId = courseId;
        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Save(int[]? subjectIds)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (user.CourseId == null) return RedirectToAction(nameof(Index));

        // Only subjects of the student's own course are allowed.
        var allowedIds = await _db.Subjects.Where(s => s.CourseId == user.CourseId).Select(s => s.Id).ToListAsync();
        var wanted = (subjectIds ?? Array.Empty<int>()).Distinct().Where(id => allowedIds.Contains(id)).ToList();

        var current = await _db.Enrollments.Where(e => e.StudentId == user.Id).ToListAsync();

        // Add the newly ticked subjects.
        var toAdd = wanted.Where(w => current.All(e => e.SubjectId != w)).ToList();
        foreach (var subjectId in toAdd)
        {
            _db.Enrollments.Add(new Enrollment { StudentId = user.Id, SubjectId = subjectId });
        }

        // Remove unticked subjects, unless attendance was already recorded (we keep that history).
        int kept = 0;
        foreach (var enrollment in current.Where(e => allowedIds.Contains(e.SubjectId) && !wanted.Contains(e.SubjectId)))
        {
            bool hasRecords = await _db.AttendanceRecords
                .AnyAsync(r => r.StudentId == user.Id && r.SubjectId == enrollment.SubjectId);

            if (hasRecords) kept++;
            else _db.Enrollments.Remove(enrollment);
        }

        await _db.SaveChangesAsync();

        TempData["Message"] = kept == 0
            ? "Subjects saved."
            : $"Subjects saved. {kept} subject(s) already have attendance records, so ask an admin to remove them.";
        return RedirectToAction("Index", "Dashboard");
    }
}
