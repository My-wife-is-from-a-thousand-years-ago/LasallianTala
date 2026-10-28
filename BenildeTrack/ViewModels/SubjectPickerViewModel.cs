using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// The "My subjects" page: step 1 picks the course, step 2 ticks subjects of that course.
public class SubjectPickerViewModel
{
    public Course? Course { get; set; }
    public List<Course> Courses { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<int> EnrolledIds { get; set; } = new();
}
