namespace BenildeTrack.Models;

// One absence OR one late for one student in one course on one day.
public class AttendanceRecord
{
    // Dropout point values. Change them here if the rule changes.
    public const int AbsentPoints = 3;
    public const int LatePoints = 1;

    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public DateTime Date { get; set; }
    public AttendanceType Type { get; set; }

    public int Points => Type == AttendanceType.Absent ? AbsentPoints : LatePoints;
}
