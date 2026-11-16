using System.Text.Json;
using BenildeTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Data;

// Keeps the HandbookSections table in sync with Data/handbook.json on every startup.
// The JSON file is the source of truth: to change what Tala knows, edit the file and restart the app.
public static class HandbookLoader
{
    private class Entry
    {
        public string Number { get; set; } = "";
        public string Title { get; set; } = "";
        public string Keywords { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public static async Task SyncAsync(ApplicationDbContext db, string contentRoot, ILogger logger)
    {
        var path = Path.Combine(contentRoot, "Data", "handbook.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("Handbook file not found at {Path}. Leaving the handbook table as it is.", path);
            return;
        }

        List<Entry>? entries;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            entries = JsonSerializer.Deserialize<List<Entry>>(await File.ReadAllTextAsync(path), options);
        }
        catch (JsonException ex)
        {
            // A typo in the JSON must not crash the app: keep whatever is already in the database.
            logger.LogError(ex, "handbook.json is not valid JSON. Leaving the handbook table as it is.");
            return;
        }

        if (entries == null || entries.Count == 0) return;

        var existing = await db.HandbookSections.ToListAsync();

        foreach (var entry in entries.Where(e => e.Number != ""))
        {
            var row = existing.FirstOrDefault(s => s.Number == entry.Number);
            if (row == null)
            {
                db.HandbookSections.Add(new HandbookSection
                {
                    Number = entry.Number, Title = entry.Title, Keywords = entry.Keywords, Text = entry.Text
                });
            }
            else
            {
                row.Title = entry.Title;
                row.Keywords = entry.Keywords;
                row.Text = entry.Text;
            }
        }

        // Sections removed from the file (like the old placeholders) are removed from the database too.
        var numbersInFile = entries.Select(e => e.Number).ToHashSet();
        db.HandbookSections.RemoveRange(existing.Where(s => !numbersInFile.Contains(s.Number)));

        await db.SaveChangesAsync();
    }
}
