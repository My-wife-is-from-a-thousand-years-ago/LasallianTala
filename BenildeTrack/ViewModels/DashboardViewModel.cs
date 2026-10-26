namespace BenildeTrack.ViewModels;

// What the student dashboard needs.
public class DashboardViewModel
{
    public List<CoursePoints> Courses { get; set; } = new();
    public int OpenViolations { get; set; }
    public int MissingClearances { get; set; }
    public int MissingObjectives { get; set; }
}
