namespace BenildeTrack.Models;

// Something every student must complete: an objective (Benire 1, BAWC interview...) or a clearance.
// This is the shared list. Who has finished what is stored in CompletedRequirement.
public class Requirement
{
    public int Id { get; set; }
    public RequirementKind Kind { get; set; }
    public string Name { get; set; } = "";

    // Short instruction shown in the "Guide" column.
    public string Guide { get; set; } = "";
}
