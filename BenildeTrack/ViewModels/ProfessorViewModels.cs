using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// One student's totals in one subject. Excused (accepted) records are not counted.
public class RosterRow
{
    public ApplicationUser Student { get; set; } = null!;
    public int Absences { get; set; }
    public int Lates { get; set; }
    public int Points => Absences * AttendanceRecord.AbsentPoints + Lates * AttendanceRecord.LatePoints;
}

// A subject with all of its students.
public class SubjectRoster
{
    public Subject Subject { get; set; } = null!;
    public List<RosterRow> Rows { get; set; } = new();
}

// What the professor dashboard needs: every subject's roster, plus excuses waiting for a decision.
public class ProfessorDashboardViewModel
{
    public List<SubjectRoster> Subjects { get; set; } = new();
    public List<ExcuseDocument> PendingExcuses { get; set; } = new();
}

// The professor's attendance page for one subject.
public class AttendanceViewModel
{
    public Subject Subject { get; set; } = null!;
    public List<RosterRow> Roster { get; set; } = new();
    public List<AttendanceRecord> Recent { get; set; } = new();
}
