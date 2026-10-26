using BenildeTrack.Models;
using BenildeTrack.Services;
using BenildeTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

// The chat box on the student dashboard calls this with fetch() and shows the JSON it gets back.
[Authorize(Roles = AppRoles.Student)]
public class TalaController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TalaService _tala;

    public TalaController(UserManager<ApplicationUser> userManager, TalaService tala)
    {
        _userManager = userManager;
        _tala = tala;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] TalaRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 300)
            return BadRequest();

        // The student id comes from the login, never from the request, so nobody can ask about someone else.
        var userId = _userManager.GetUserId(User)!;
        var reply = await _tala.AskAsync(userId, request.Question);
        return Json(reply);
    }
}
