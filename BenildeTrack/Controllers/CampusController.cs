using BenildeTrack.Models;
using BenildeTrack.Services;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

// The student's Campus page: violations, missing clearances, and missing objectives (with guides).
[Authorize(Roles = AppRoles.Student)]
public class CampusController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StandingService _standing;

    public CampusController(UserManager<ApplicationUser> userManager, StandingService standing)
    {
        _userManager = userManager;
        _standing = standing;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var model = new CampusViewModel
        {
            Violations = await _standing.GetOpenViolationsAsync(userId),
            MissingClearances = await _standing.GetMissingAsync(userId, RequirementKind.Clearance),
            MissingObjectives = await _standing.GetMissingAsync(userId, RequirementKind.Objective)
        };
        return View(model);
    }
}
