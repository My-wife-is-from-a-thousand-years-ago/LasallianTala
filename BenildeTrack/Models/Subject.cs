namespace BenildeTrack.Models;

// One class a student can take. This is what the Course Panel shows (the old v2 "Course").
// One professor per subject: if two professors teach the same subject, add it twice (for example DEMO101-A and DEMO101-B).
public class Subject
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    // Dropout limit in points: 9 or 18, depending on the subject.
    public int MaxPoints { get; set; } = 9;

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public string ProfessorId { get; set; } = "";
    public ApplicationUser? Professor { get; set; }
}
