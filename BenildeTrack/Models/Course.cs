namespace BenildeTrack.Models;

// A degree program, for example "BSIS". A student belongs to one course and picks their subjects from it.
public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
