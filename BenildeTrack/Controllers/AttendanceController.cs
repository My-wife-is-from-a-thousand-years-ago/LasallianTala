using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Teachers record absences and lates for the courses THEY teach.
// No business rules here yet, so it talks to the database directly.
[Authorize(Roles = AppRoles.Teacher)]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // A teacher may only open their own courses.
    private async Task<Course?> MyCourseAsync(int courseId)
    {
        var teacherId = _userManager.GetUserId(User);
        return await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);
    }

    public async Task<IActionResult> Index(int courseId)
    {
        var course = await MyCourseAsync(courseId);
        if (course == null) return NotFound();

        var students = await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.Student!)
            .ToListAsync();
        students = students.OrderBy(s => s.DisplayName).ToList();

        var records = await _db.AttendanceRecords
            .Include(r => r.Student)
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        var model = new AttendanceViewModel
        {
            Course = course,
            Students = students,
            Recent = records.Take(15).ToList(),
            Roster = students.Select(s => new RosterRow
            {
                Student = s,
                Absences = records.Count(r => r.StudentId == s.Id && r.Type == AttendanceType.Absent),
                Lates = records.Count(r => r.StudentId == s.Id && r.Type == AttendanceType.Late)
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int courseId, string studentId, DateTime date, AttendanceType type)
    {
        var course = await MyCourseAsync(courseId);
        if (course == null) return NotFound();

        if (date == default)
        {
            TempData["Error"] = "Pick a date.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        bool enrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);
        bool alreadyRecorded = await _db.AttendanceRecords.AnyAsync(r =>
            r.CourseId == courseId && r.StudentId == studentId && r.Date == date.Date);

        if (!enrolled)
        {
            TempData["Error"] = "That student is not in this course.";
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
                CourseId = courseId,
                Date = date.Date,
                Type = type
            });
            await _db.SaveChangesAsync();
            TempData["Message"] = "Recorded.";
        }

        return RedirectToAction(nameof(Index), new { courseId });
    }

    // Fix a mistake: delete a record from one of MY courses.
    [HttpPost]
    public async Task<IActionResult> Remove(int courseId, int recordId)
    {
        var course = await MyCourseAsync(courseId);
        if (course == null) return NotFound();

        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.Id == recordId && r.CourseId == courseId);
        if (record != null)
        {
            _db.AttendanceRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Record removed.";
        }

        return RedirectToAction(nameof(Index), new { courseId });
    }
}
