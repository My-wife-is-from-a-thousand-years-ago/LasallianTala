using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// Everything on the teacher's attendance page for one course.
public class AttendanceViewModel
{
    public Course Course { get; set; } = null!;
    public List<ApplicationUser> Students { get; set; } = new();
    public List<AttendanceRecord> Recent { get; set; } = new();
    public List<RosterRow> Roster { get; set; } = new();
}

// One student's totals in this course.
public class RosterRow
{
    public ApplicationUser Student { get; set; } = null!;
    public int Absences { get; set; }
    public int Lates { get; set; }
    public int Points => Absences * AttendanceRecord.AbsentPoints + Lates * AttendanceRecord.LatePoints;
}
