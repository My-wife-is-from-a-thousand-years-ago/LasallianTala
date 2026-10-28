namespace BenildeTrack.Models;

// One excuse that covers one or more attendance records in ONE subject.
// SubjectId is what routes it to the right professor.
public class ExcuseDocument
{
    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public string Reason { get; set; } = "";

    // Files live outside wwwroot under random names. A student may send a picture, a signature, or both.
    public string? PictureFileName { get; set; }
    public string? SignatureFileName { get; set; }

    public DateTime DateSubmitted { get; set; }
    public ExcuseStatus Status { get; set; } = ExcuseStatus.Pending;
    public string? Remarks { get; set; }

    public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
}
