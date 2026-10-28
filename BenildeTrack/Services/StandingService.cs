using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Services;

// Answers "where does this student stand?": dropout points, violations, missing requirements, rosters.
public class StandingService
{
    private readonly ApplicationDbContext _db;

    // ASP.NET passes the database in here automatically (dependency injection).
    public StandingService(ApplicationDbContext db)
    {
        _db = db;
    }

    // One row per subject the student takes, with absences, lates and points.
    public async Task<List<SubjectPoints>> GetSubjectPointsAsync(string studentId)
    {
        var subjects = await _db.Enrollments
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Subject!.Code)
            .Select(e => e.Subject!)
            .ToListAsync();

        var records = await _db.AttendanceRecords
            .Include(r => r.ExcuseDocument)
            .Where(r => r.StudentId == studentId)
            .ToListAsync();

        var rows = new List<SubjectPoints>();
        foreach (var subject in subjects)
        {
            var mine = records.Where(r => r.SubjectId == subject.Id).ToList();

            // A record stops counting only after the professor ACCEPTED its excuse.
            var counted = mine.Where(r => r.ExcuseDocument?.Status != ExcuseStatus.Accepted).ToList();

            rows.Add(new SubjectPoints
            {
                Subject = subject,
                Absences = counted.Count(r => r.Type == AttendanceType.Absent),
                Lates = counted.Count(r => r.Type == AttendanceType.Late),
                PendingReview = mine.Count(r => r.ExcuseDocument?.Status == ExcuseStatus.Pending),
                Excusable = mine.Count(r => r.ExcuseDocument == null || r.ExcuseDocument.Status == ExcuseStatus.Rejected)
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

    // Every student in one subject with their totals (what professors see).
    public async Task<List<RosterRow>> GetRosterAsync(int subjectId)
    {
        var students = await _db.Enrollments
            .Where(e => e.SubjectId == subjectId)
            .Select(e => e.Student!)
            .ToListAsync();

        var records = await _db.AttendanceRecords
            .Include(r => r.ExcuseDocument)
            .Where(r => r.SubjectId == subjectId)
            .ToListAsync();

        return students
            .OrderBy(s => s.DisplayName)
            .Select(s =>
            {
                var counted = records
                    .Where(r => r.StudentId == s.Id && r.ExcuseDocument?.Status != ExcuseStatus.Accepted)
                    .ToList();

                return new RosterRow
                {
                    Student = s,
                    Absences = counted.Count(r => r.Type == AttendanceType.Absent),
                    Lates = counted.Count(r => r.Type == AttendanceType.Late)
                };
            })
            .ToList();
    }

    // All of one professor's subjects, each with its roster.
    public async Task<List<SubjectRoster>> GetRostersAsync(string professorId)
    {
        var subjects = await _db.Subjects
            .Where(s => s.ProfessorId == professorId)
            .OrderBy(s => s.Code)
            .ToListAsync();

        var result = new List<SubjectRoster>();
        foreach (var subject in subjects)
        {
            result.Add(new SubjectRoster { Subject = subject, Rows = await GetRosterAsync(subject.Id) });
        }
        return result;
    }
}
