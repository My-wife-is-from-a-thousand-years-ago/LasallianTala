using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// One line of the admin's student list.
public class StudentRow
{
    public ApplicationUser Student { get; set; } = null!;
    public int OpenViolations { get; set; }
    public int MissingRequirements { get; set; }
}

// Everything on the admin's "manage one student" page.
public class StudentDetailViewModel
{
    public ApplicationUser Student { get; set; } = null!;
    public List<Course> Enrolled { get; set; } = new();
    public List<Course> NotEnrolled { get; set; } = new();
    public List<Violation> Violations { get; set; } = new();
    public List<RequirementRow> Requirements { get; set; } = new();
}

public class RequirementRow
{
    public Requirement Requirement { get; set; } = null!;
    public bool IsDone { get; set; }
}
