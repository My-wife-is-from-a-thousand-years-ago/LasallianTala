using BenildeTrack.Models;
using BenildeTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

// Handbook page for every role: search box, or the full list when nothing is typed.
[Authorize]
public class HandbookController : Controller
{
    private readonly HandbookService _handbook;

    public HandbookController(HandbookService handbook)
    {
        _handbook = handbook;
    }

    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Query = q;

        List<HandbookSection> sections = string.IsNullOrWhiteSpace(q)
            ? await _handbook.GetAllAsync()
            : await _handbook.SearchAsync(q);

        return View(sections);
    }
}
