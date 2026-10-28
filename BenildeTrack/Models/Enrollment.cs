namespace BenildeTrack.Models;

// Which student takes which subject.
public class Enrollment
{
    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
}
