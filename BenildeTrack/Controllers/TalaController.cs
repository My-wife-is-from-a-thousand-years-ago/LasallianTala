using BenildeTrack.Models;
using BenildeTrack.Services.Tala;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

// "Ask Tala" is for everyone: students, professors and admins.
[Authorize]
public class TalaController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TalaService _tala;

    public TalaController(UserManager<ApplicationUser> userManager, TalaService tala)
    {
        _userManager = userManager;
        _tala = tala;
    }

    public IActionResult Index()
    {
        return View();
    }

    // The chat box calls this with fetch() and shows the JSON it gets back.
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] TalaRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 300)
            return BadRequest();

        // The user id comes from the login, never from the request, so nobody can ask about someone else.
        var userId = _userManager.GetUserId(User)!;
        var reply = await _tala.AskAsync(userId, User.IsInRole(AppRoles.Student), request.Question);
        return Json(reply);
    }
}
