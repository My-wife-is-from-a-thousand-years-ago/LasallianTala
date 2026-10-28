namespace BenildeTrack.Models;

// The three kinds of accounts. Constants avoid typos in role names.
public static class AppRoles
{
    public const string Student = "Student";
    public const string Professor = "Professor";
    public const string Admin = "Admin";

    public static readonly string[] All = { Student, Professor, Admin };
}
