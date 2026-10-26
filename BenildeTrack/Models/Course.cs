namespace BenildeTrack.Models;

// One subject taught by one teacher.
public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Subject { get; set; } = "";

    // Dropout limit in points: 9 or 18, depending on the subject.
    public int MaxPoints { get; set; } = 9;

    public string TeacherId { get; set; } = "";
    public ApplicationUser? Teacher { get; set; }
}
