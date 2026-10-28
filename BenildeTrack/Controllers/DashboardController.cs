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
//   Student   -> their own records
//   Professor -> their students' records
//   Admin     -> account management
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly StandingService _standing;
    private readonly ExcuseService _excuses;

    public DashboardController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        StandingService standing,
        ExcuseService excuses)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _standing = standing;
        _excuses = excuses;
    }

    public async Task<IActionResult> Index()
    {
        // Someone who just registered has no role yet. Make them a Student (the safest role),
        // then refresh their login cookie so the new role takes effect.
        if (!User.IsInRole(AppRoles.Student) && !User.IsInRole(AppRoles.Professor) && !User.IsInRole(AppRoles.Admin))
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
            return View("Admin", await BuildAccountRowsAsync());

        if (User.IsInRole(AppRoles.Professor))
        {
            var professorModel = new ProfessorDashboardViewModel
            {
                Subjects = await _standing.GetRostersAsync(userId),
                PendingExcuses = await _excuses.GetPendingForProfessorAsync(userId)
            };
            return View("Professor", professorModel);
        }

        var student = await _db.Users.Include(u => u.Course).FirstAsync(u => u.Id == userId);
        var studentModel = new DashboardViewModel
        {
            HasCourse = student.CourseId != null,
            CourseName = student.Course?.Name ?? "",
            Subjects = await _standing.GetSubjectPointsAsync(userId),
            RecentExcuses = await _excuses.GetForStudentAsync(userId, 5),
            OpenViolations = (await _standing.GetOpenViolationsAsync(userId)).Count,
            MissingClearances = (await _standing.GetMissingAsync(userId, RequirementKind.Clearance)).Count,
            MissingObjectives = (await _standing.GetMissingAsync(userId, RequirementKind.Objective)).Count
        };
        return View("Student", studentModel);
    }

    // The admin's account list: every user with their role.
    private async Task<List<AccountRow>> BuildAccountRowsAsync()
    {
        var users = await _db.Users.Include(u => u.Course).OrderBy(u => u.Email).ToListAsync();

        var userRoles = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync();
        var roleByUser = userRoles.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First().RoleName ?? "");

        return users
            .Select(u => new AccountRow { User = u, Role = roleByUser.GetValueOrDefault(u.Id, "") })
            .ToList();
    }
}
