using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Services;

// Answers "where does this student stand?": dropout points, violations, and missing requirements.
public class StandingService
{
    private readonly ApplicationDbContext _db;

    // ASP.NET passes the database in here automatically (dependency injection).
    public StandingService(ApplicationDbContext db)
    {
        _db = db;
    }

    // One row per course the student is enrolled in, with absences, lates and points.
    public async Task<List<CoursePoints>> GetCoursePointsAsync(string studentId)
    {
        var courses = await _db.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.Course!)
            .OrderBy(c => c.Code)
            .ToListAsync();

        var records = await _db.AttendanceRecords
            .Where(r => r.StudentId == studentId)
            .ToListAsync();

        var rows = new List<CoursePoints>();
        foreach (var course in courses)
        {
            var mine = records.Where(r => r.CourseId == course.Id).ToList();
            rows.Add(new CoursePoints
            {
                Course = course,
                Absences = mine.Count(r => r.Type == AttendanceType.Absent),
                Lates = mine.Count(r => r.Type == AttendanceType.Late)
            });
        }
        return rows;
    }

    public Task<List<Violation>> GetOpenViolationsAsync(string studentId)
    {
        return _db.Violations
            .Where(v => v.StudentId == studentId && !v.IsResolved)
            .OrderByDescending(v => v.Date)
            .ToListAsync();
    }

    // Requirements of one kind that the student has NOT completed yet.
    public Task<List<Requirement>> GetMissingAsync(string studentId, RequirementKind kind)
    {
        return _db.Requirements
            .Where(r => r.Kind == kind
                     && !_db.CompletedRequirements.Any(c => c.RequirementId == r.Id && c.StudentId == studentId))
            .OrderBy(r => r.Id)
            .ToListAsync();
    }
}
