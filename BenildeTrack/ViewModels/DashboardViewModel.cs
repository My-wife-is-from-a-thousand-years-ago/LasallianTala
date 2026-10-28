using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// What the student dashboard needs.
public class DashboardViewModel
{
    public bool HasCourse { get; set; }
    public string CourseName { get; set; } = "";
    public List<SubjectPoints> Subjects { get; set; } = new();
    public List<ExcuseDocument> RecentExcuses { get; set; } = new();
    public int OpenViolations { get; set; }
    public int MissingClearances { get; set; }
    public int MissingObjectives { get; set; }
}
