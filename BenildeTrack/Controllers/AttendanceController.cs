using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.Services;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Professors record absences and lates for the subjects THEY teach.
// Adding and removing a record has no business rules, so it talks to the database directly.
[Authorize(Roles = AppRoles.Professor)]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StandingService _standing;

    public AttendanceController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, StandingService standing)
    {
        _db = db;
        _userManager = userManager;
        _standing = standing;
    }

    // A professor may only open their own subjects.
    private async Task<Subject?> MySubjectAsync(int subjectId)
    {
        var professorId = _userManager.GetUserId(User);
        return await _db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.ProfessorId == professorId);
    }

    public async Task<IActionResult> Index(int subjectId)
    {
        var subject = await MySubjectAsync(subjectId);
        if (subject == null) return NotFound();

        var recent = await _db.AttendanceRecords
            .Include(r => r.Student)
            .Where(r => r.SubjectId == subjectId)
            .OrderByDescending(r => r.Date)
            .Take(15)
            .ToListAsync();

        var model = new AttendanceViewModel
        {
            Subject = subject,
            Roster = await _standing.GetRosterAsync(subjectId),
            Recent = recent
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int subjectId, string studentId, DateTime date, AttendanceType type)
    {
        var subject = await MySubjectAsync(subjectId);
        if (subject == null) return NotFound();

        if (date == default)
        {
            TempData["Error"] = "Pick a date.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        bool enrolled = await _db.Enrollments.AnyAsync(e => e.SubjectId == subjectId && e.StudentId == studentId);
        bool alreadyRecorded = await _db.AttendanceRecords.AnyAsync(r =>
            r.SubjectId == subjectId && r.StudentId == studentId && r.Date == date.Date);

        if (!enrolled)
        {
            TempData["Error"] = "That student is not in this subject.";
        }
        else if (alreadyRecorded)
        {
            TempData["Error"] = "That student already has a record for that date. Remove it first to change it.";
        }
        else
        {
            _db.AttendanceRecords.Add(new AttendanceRecord
            {
                StudentId = studentId,
                SubjectId = subjectId,
                Date = date.Date,
                Type = type
            });
            await _db.SaveChangesAsync();
            TempData["Message"] = "Recorded.";
        }

        return RedirectToAction(nameof(Index), new { subjectId });
    }

    // Fix a mistake: delete a record from one of MY subjects.
    [HttpPost]
    public async Task<IActionResult> Remove(int subjectId, int recordId)
    {
        var subject = await MySubjectAsync(subjectId);
        if (subject == null) return NotFound();

        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.Id == recordId && r.SubjectId == subjectId);
        if (record != null)
        {
            _db.AttendanceRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Record removed.";
        }

        return RedirectToAction(nameof(Index), new { subjectId });
    }
}
