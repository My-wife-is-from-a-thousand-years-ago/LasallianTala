using Microsoft.AspNetCore.Identity;

namespace BenildeTrack.Models;

// The login account, plus a real name. Students, teachers and admins are all ApplicationUsers.
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";

    // Shown in the UI. Falls back to the email until the person fills in their name.
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? (UserName ?? "") : FullName;
}
