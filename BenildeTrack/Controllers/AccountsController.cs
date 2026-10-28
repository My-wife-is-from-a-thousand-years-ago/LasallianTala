using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Admin only: create, edit and delete accounts. The list itself is on the admin dashboard.
[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // The course dropdown is not posted back, so refill it whenever the form is shown.
    private async Task<AccountFormViewModel> WithCoursesAsync(AccountFormViewModel model)
    {
        model.Courses = await _db.Courses.OrderBy(c => c.Code).ToListAsync();
        return model;
    }

    // ---------- Create ----------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await WithCoursesAsync(new AccountFormViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(AccountFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "A password is required.");
        if (!AppRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Pick a role.");
        if (!ModelState.IsValid)
            return View(await WithCoursesAsync(model));

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true,
            CourseId = model.Role == AppRoles.Student ? model.CourseId : null
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(await WithCoursesAsync(model));
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["Message"] = "Account created.";
        return RedirectToAction("Index", "Dashboard");
    }

    // ---------- Edit ----------

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var model = new AccountFormViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? AppRoles.Student,
            CourseId = user.CourseId
        };
        return View(await WithCoursesAsync(model));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(AccountFormViewModel model)
    {
        var user = model.Id == null ? null : await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        bool isMe = user.Id == _userManager.GetUserId(User);

        if (!AppRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Pick a role.");

        // Safety rules: an admin cannot demote themselves, and a professor with subjects cannot change role.
        if (isMe && model.Role != AppRoles.Admin)
            ModelState.AddModelError(nameof(model.Role), "You can't change your own role.");
        if (currentRoles.Contains(AppRoles.Professor) && model.Role != AppRoles.Professor
            && await _db.Subjects.AnyAsync(s => s.ProfessorId == user.Id))
            ModelState.AddModelError(nameof(model.Role), "This professor still teaches subjects.");

        if (!ModelState.IsValid)
            return View(await WithCoursesAsync(model));

        user.FullName = model.FullName.Trim();
        user.CourseId = model.Role == AppRoles.Student ? model.CourseId : null;
        await _userManager.UpdateAsync(user);

        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        // Optional password reset.
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!reset.Succeeded)
            {
                foreach (var error in reset.Errors) ModelState.AddModelError("", error.Description);
                return View(await WithCoursesAsync(model));
            }
        }

        TempData["Message"] = "Account updated.";
        return RedirectToAction("Index", "Dashboard");
    }

    // ---------- Delete ----------

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You can't delete your own account.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (await _db.Subjects.AnyAsync(s => s.ProfessorId == id))
        {
            TempData["Error"] = "This professor still teaches subjects. Reassign or remove those subjects first.";
            return RedirectToAction("Index", "Dashboard");
        }

        try
        {
            // A student's enrollments, attendance, violations and excuses are deleted with the account.
            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded ? "Account deleted." : "Could not delete the account.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "That account could not be deleted because other records depend on it.";
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
