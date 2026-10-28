using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// What the Campus page shows: everything the student still has to deal with.
public class CampusViewModel
{
    public List<Violation> Violations { get; set; } = new();
    public List<Requirement> MissingClearances { get; set; } = new();
    public List<Requirement> MissingObjectives { get; set; } = new();
}
