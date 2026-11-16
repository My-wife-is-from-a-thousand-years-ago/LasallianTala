using System.Text.RegularExpressions;
using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Services;

// Reads and searches the handbook. Used by the Handbook page and by Tala.
public class HandbookService
{
    // Words that appear in almost every question and tell us nothing about WHICH rule is meant.
    private static readonly HashSet<string> IgnoredWords = new()
    {
        "the", "and", "for", "are", "does", "can", "what", "how", "with", "this", "that", "count", "counts",
        "was", "were", "has", "had", "have", "not", "but", "you", "your", "him", "her", "his", "she", "they",
        "them", "who", "get", "got", "did", "its", "from", "into", "about", "would", "could", "should",
        "there", "their", "very", "during", "inside", "school", "premises", "campus", "student", "students",
        "violation", "violations", "rule", "rules", "allowed", "wore", "just", "only", "also", "when", "then",
        "today", "yesterday", "hour", "hours", "time", "day", "days"
    };

    // Short words that ARE meaningful (a 2-letter word is normally dropped).
    private static readonly HashSet<string> ShortWordsToKeep = new() { "id" };

    private readonly ApplicationDbContext _db;

    public HandbookService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<HandbookSection>> GetAllAsync()
    {
        var sections = await _db.HandbookSections.ToListAsync();
        return sections.OrderBy(s => NaturalKey(s.Number), StringComparer.Ordinal).ToList();
    }

    // Sorts "7.8.1.2" before "7.8.1.10" (plain text sorting would put 10 first), and "Appendix ..." last.
    private static string NaturalKey(string number)
    {
        var parts = number.Replace("Appendix ", "~").Split('.');
        return string.Join(".", parts.Select(p => int.TryParse(p, out var n) ? n.ToString("D4") : p));
    }

    // Scores each section by how many of the question's words appear in it.
    // A hit in the title or keywords is worth more than a hit in the body text.
    public async Task<List<HandbookSection>> SearchAsync(string query, int take = 5)
    {
        var candidates = Tokenize(query)
            .Where(w => w.Length > 2 || ShortWordsToKeep.Contains(w))
            .Distinct()
            .ToList();

        var words = candidates.Where(w => !IgnoredWords.Contains(w)).ToList();

        // Someone who types only "violation" still deserves results, so fall back to every word.
        if (words.Count == 0) words = candidates;
        if (words.Count == 0) return new List<HandbookSection>();

        // "bullied" and "bullying" should both find "bully".
        words = words.Select(Stem).Distinct().ToList();

        // A handbook is small, so loading every section and scoring in memory is fine.
        var sections = await _db.HandbookSections.ToListAsync();

        var indexed = sections
            .Select(s => new
            {
                Section = s,
                Important = Tokenize(s.Title + " " + s.Keywords).Select(Stem).ToHashSet(),
                Body = Tokenize(s.Text).Select(Stem).ToHashSet()
            })
            .ToList();

        // A word found in only a few sections ("bully") says much more than one found in most ("class"),
        // so rare words get a bigger weight.
        var weight = words.ToDictionary(
            w => w,
            w => Math.Log(1.0 + (double)indexed.Count / (1 + indexed.Count(x => WordMatches(w, x.Important) || WordMatches(w, x.Body)))));

        return indexed
            .Select(x =>
            {
                double score = 0;
                foreach (var w in words)
                {
                    if (WordMatches(w, x.Important)) score += 3 * weight[w];
                    else if (WordMatches(w, x.Body)) score += 1 * weight[w];
                }
                return new { x.Section, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(take)
            .Select(x => x.Section)
            .ToList();
    }

    // Whole words only ("id" must not match "provide"), but a prefix counts either way,
    // so "skirt" finds "skirts" and "shorts" finds "short".
    private static bool WordMatches(string word, HashSet<string> sectionWords)
    {
        if (sectionWords.Contains(word)) return true;
        if (word.Length < 4) return false;

        // Only close relatives count ("disrupt" / "disruption"), not "class" / "classmate".
        foreach (var s in sectionWords)
        {
            if (s.Length < 4) continue;
            var shorter = Math.Min(s.Length, word.Length);
            var longer = Math.Max(s.Length, word.Length);
            if ((s.StartsWith(word) || word.StartsWith(s)) && shorter >= 0.7 * longer) return true;
        }
        return false;
    }

    // Very light stemming: cuts common endings so different forms of a word match each other.
    private static string Stem(string w)
    {
        if (w.Length > 4 && (w.EndsWith("ied") || w.EndsWith("ies"))) return w[..^3] + "y";
        foreach (var suffix in new[] { "ing", "ed", "es", "s" })
        {
            if (w.Length > suffix.Length + 2 && w.EndsWith(suffix)) return w[..^suffix.Length];
        }
        return w;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+").Select(m => m.Value);
    }
}