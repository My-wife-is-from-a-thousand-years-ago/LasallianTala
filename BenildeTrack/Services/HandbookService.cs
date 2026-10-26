using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Services;

// Reads and searches the handbook. Used by the Handbook page and by Tala.
public class HandbookService
{
    // Common words that would match almost every section.
    private static readonly HashSet<string> IgnoredWords = new()
    {
        "the", "and", "for", "are", "does", "can", "what", "how", "with", "this", "that", "count", "counts"
    };

    private readonly ApplicationDbContext _db;

    public HandbookService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<HandbookSection>> GetAllAsync()
    {
        return _db.HandbookSections.OrderBy(s => s.Number).ToListAsync();
    }

    // Scores each section by how many of the question's words it contains. Best matches first.
    public async Task<List<HandbookSection>> SearchAsync(string query)
    {
        var cleaned = new string(query.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
        var words = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !IgnoredWords.Contains(w))
            .Distinct()
            .ToList();

        if (words.Count == 0) return new List<HandbookSection>();

        // A handbook is small, so loading every section and scoring in memory is fine.
        var sections = await _db.HandbookSections.ToListAsync();

        return sections
            .Select(s => new
            {
                Section = s,
                Score = words.Count(w => (s.Title + " " + s.Text).ToLowerInvariant().Contains(w))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => x.Section)
            .ToList();
    }
}
