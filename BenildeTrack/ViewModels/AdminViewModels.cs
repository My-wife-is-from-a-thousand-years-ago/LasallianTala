using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// One line of the admin's account list.
public class AccountRow
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = "";
}

// Everything on the admin's "student records" page.
public class StudentDetailViewModel
{
    public ApplicationUser Student { get; set; } = null!;
    public List<Subject> Enrolled { get; set; } = new();
    public List<Subject> NotEnrolled { get; set; } = new();
    public List<Violation> Violations { get; set; } = new();
    public List<RequirementRow> Requirements { get; set; } = new();
}

public class RequirementRow
{
    public Requirement Requirement { get; set; } = null!;
    public bool IsDone { get; set; }
}
