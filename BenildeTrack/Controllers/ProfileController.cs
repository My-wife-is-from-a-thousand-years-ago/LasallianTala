using BenildeTrack.Models;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

// Shows the account and lets the person set their display name.
// Password and email changes use the built-in page at /Identity/Account/Manage.
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var roles = await _userManager.GetRolesAsync(user);
        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? AppRoles.Student
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update(string fullName = "")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        fullName = fullName.Trim();
        if (fullName.Length == 0 || fullName.Length > 100)
        {
            TempData["Error"] = "Name must be between 1 and 100 characters.";
            return RedirectToAction(nameof(Index));
        }

        user.FullName = fullName;
        await _userManager.UpdateAsync(user);

        TempData["Message"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }
}
