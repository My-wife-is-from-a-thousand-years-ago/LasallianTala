using Microsoft.AspNetCore.Identity;

namespace BenildeTrack.Models;

// The login account. Students, professors and admins are all ApplicationUsers; the role tells them apart.
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";

    // A student's degree program (for example BSIS). Empty for professors, admins, and new students.
    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    // Shown in the UI. Falls back to the email until the person fills in their name.
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? (UserName ?? "") : FullName;
}
