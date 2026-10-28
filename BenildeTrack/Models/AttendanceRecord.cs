namespace BenildeTrack.Models;

// One absence OR one late for one student in one subject on one day.
public class AttendanceRecord
{
    // Dropout point values. Change them here if the rule changes.
    public const int AbsentPoints = 3;
    public const int LatePoints = 1;

    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public DateTime Date { get; set; }
    public AttendanceType Type { get; set; }

    // Set when the student attaches an excuse. The record only stops counting once the excuse is Accepted.
    public int? ExcuseDocumentId { get; set; }
    public ExcuseDocument? ExcuseDocument { get; set; }

    public int Points => Type == AttendanceType.Absent ? AbsentPoints : LatePoints;
}
