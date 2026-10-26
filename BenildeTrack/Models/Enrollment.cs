namespace BenildeTrack.Models;

// Which student is in which course.
public class Enrollment
{
    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }
}
