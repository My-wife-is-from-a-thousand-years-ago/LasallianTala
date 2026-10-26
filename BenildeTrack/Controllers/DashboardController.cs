using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.Services;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// One URL, three dashboards: the logged-in role decides which view is shown.
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly StandingService _standing;

    public DashboardController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        StandingService standing)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _standing = standing;
    }

    public async Task<IActionResult> Index()
    {
        // Someone who just registered has no role yet. Make them a Student (the safest role),
        // then refresh their login cookie so the new role takes effect.
        if (!User.IsInRole(AppRoles.Student) && !User.IsInRole(AppRoles.Teacher) && !User.IsInRole(AppRoles.Admin))
        {
            var newUser = await _userManager.GetUserAsync(User);
            if (newUser == null) return Challenge();

            var result = await _userManager.AddToRoleAsync(newUser, AppRoles.Student);
            if (!result.Succeeded) return StatusCode(500, "Could not assign a role to this account.");

            await _signInManager.RefreshSignInAsync(newUser);
            return RedirectToAction(nameof(Index));
        }

        var userId = _userManager.GetUserId(User)!;

        if (User.IsInRole(AppRoles.Admin))
            return View("Admin", await BuildStudentRowsAsync());

        if (User.IsInRole(AppRoles.Teacher))
        {
            var myCourses = await _db.Courses.Where(c => c.TeacherId == userId).OrderBy(c => c.Code).ToListAsync();
            return View("Teacher", myCourses);
        }

        var model = new DashboardViewModel
        {
            Courses = await _standing.GetCoursePointsAsync(userId),
            OpenViolations = (await _standing.GetOpenViolationsAsync(userId)).Count,
            MissingClearances = (await _standing.GetMissingAsync(userId, RequirementKind.Clearance)).Count,
            MissingObjectives = (await _standing.GetMissingAsync(userId, RequirementKind.Objective)).Count
        };
        return View("Student", model);
    }

    // The admin's student list, with counts of open violations and missing requirements.
    private async Task<List<StudentRow>> BuildStudentRowsAsync()
    {
        var students = await _userManager.GetUsersInRoleAsync(AppRoles.Student);

        var violationCounts = await _db.Violations
            .Where(v => !v.IsResolved)
            .GroupBy(v => v.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count);

        var doneCounts = await _db.CompletedRequirements
            .GroupBy(c => c.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count);

        int totalRequirements = await _db.Requirements.CountAsync();

        return students
            .OrderBy(s => s.DisplayName)
            .Select(s => new StudentRow
            {
                Student = s,
                OpenViolations = violationCounts.GetValueOrDefault(s.Id),
                MissingRequirements = totalRequirements - doneCounts.GetValueOrDefault(s.Id)
            })
            .ToList();
    }
}
