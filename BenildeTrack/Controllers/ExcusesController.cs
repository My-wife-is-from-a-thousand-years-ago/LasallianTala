using BenildeTrack.Models;
using BenildeTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

// Excuses: students submit them (picture and/or whiteboard signature), professors decide.
[Authorize]
public class ExcusesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ExcuseService _excuses;
    private readonly FileStorage _files;

    public ExcusesController(UserManager<ApplicationUser> userManager, ExcuseService excuses, FileStorage files)
    {
        _userManager = userManager;
        _excuses = excuses;
        _files = files;
    }

    // ---------- Student ----------

    // Form: pick which absences/lates in ONE subject this excuse covers.
    [Authorize(Roles = AppRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Create(int subjectId)
    {
        var records = await _excuses.GetExcusableAsync(_userManager.GetUserId(User)!, subjectId);
        if (records.Count == 0) return RedirectToAction("Index", "Dashboard");

        return View(records);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost]
    [RequestSizeLimit(8_000_000)]
    public async Task<IActionResult> Create(
        int subjectId, string reason = "", IFormFile? picture = null, string? signatureData = null, int[]? recordIds = null)
    {
        var error = await _excuses.SubmitAsync(_userManager.GetUserId(User)!, subjectId, reason, picture, signatureData, recordIds);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Create), new { subjectId });
        }

        TempData["Message"] = "Excuse submitted. Your professor will review it.";
        return RedirectToAction("Index", "Dashboard");
    }

    // ---------- Professor ----------

    [Authorize(Roles = AppRoles.Professor)]
    public async Task<IActionResult> Review(int id)
    {
        var doc = await _excuses.GetForReviewAsync(id, _userManager.GetUserId(User)!);
        if (doc == null) return NotFound();

        return View(doc);
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpPost]
    public async Task<IActionResult> Decide(int id, bool accept, string? remarks)
    {
        var error = await _excuses.DecideAsync(id, _userManager.GetUserId(User)!, accept, remarks);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Review), new { id });
        }

        TempData["Message"] = accept ? "Excuse accepted." : "Excuse rejected.";
        return RedirectToAction("Index", "Dashboard");
    }

    // ---------- Files (student who sent it, or that subject's professor) ----------

    public async Task<IActionResult> Picture(int id)
    {
        var doc = await _excuses.GetForFileAsync(id, _userManager.GetUserId(User)!);
        return ServeFile(doc?.PictureFileName);
    }

    public async Task<IActionResult> Signature(int id)
    {
        var doc = await _excuses.GetForFileAsync(id, _userManager.GetUserId(User)!);
        return ServeFile(doc?.SignatureFileName);
    }

    // NotFound (not Forbidden) so outsiders cannot learn which ids exist.
    private IActionResult ServeFile(string? storedName)
    {
        if (storedName == null) return NotFound();

        var path = _files.GetPath(storedName);
        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(path, FileStorage.ContentTypeOf(storedName));
    }
}
