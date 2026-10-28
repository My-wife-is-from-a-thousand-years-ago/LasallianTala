using System.ComponentModel.DataAnnotations;
using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// The create/edit account form. The attributes below are the validation rules.
public class AccountFormViewModel
{
    // Empty when creating a new account.
    public string? Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100)]
    public string FullName { get; set; } = "";

    [Required]
    public string Role { get; set; } = AppRoles.Student;

    // Only used for students.
    public int? CourseId { get; set; }

    // Required when creating. When editing, leave it empty to keep the current password.
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    // Fills the course dropdown. Not part of the form.
    public List<Course> Courses { get; set; } = new();
}
