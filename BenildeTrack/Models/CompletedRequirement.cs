namespace BenildeTrack.Models;

// A row here means "this student finished this requirement". No row = still missing.
public class CompletedRequirement
{
    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }

    public int RequirementId { get; set; }
    public Requirement? Requirement { get; set; }

    public DateTime DateCompleted { get; set; }
}
