using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Services;

// Everything about excuses: submitting, reviewing, and who may open the files.
// Methods that can fail return an error message (string) or null when everything worked.
public class ExcuseService
{
    private readonly ApplicationDbContext _db;
    private readonly FileStorage _files;

    public ExcuseService(ApplicationDbContext db, FileStorage files)
    {
        _db = db;
        _files = files;
    }

    // ---------- Student side ----------

    // Records in one subject that still need an excuse (never excused, or the last attempt was rejected).
    public Task<List<AttendanceRecord>> GetExcusableAsync(string studentId, int subjectId)
    {
        return _db.AttendanceRecords
            .Include(r => r.Subject)
            .Where(r => r.StudentId == studentId
                     && r.SubjectId == subjectId
                     && (r.ExcuseDocument == null || r.ExcuseDocument.Status == ExcuseStatus.Rejected))
            .OrderBy(r => r.Date)
            .ToListAsync();
    }

    public Task<List<ExcuseDocument>> GetForStudentAsync(string studentId, int take)
    {
        return _db.ExcuseDocuments
            .Include(d => d.Subject)
            .Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.DateSubmitted)
            .Take(take)
            .ToListAsync();
    }

    public async Task<string?> SubmitAsync(
        string studentId, int subjectId, string reason, IFormFile? picture, string? signatureData, int[]? recordIds)
    {
        reason = reason.Trim();
        if (reason.Length == 0 || reason.Length > 200) return "Write a short reason (up to 200 characters).";

        var ids = (recordIds ?? Array.Empty<int>()).Distinct().ToList();
        if (ids.Count == 0) return "Pick at least one date to excuse.";

        bool hasPicture = picture != null && picture.Length > 0;
        bool hasSignature = !string.IsNullOrWhiteSpace(signatureData);
        if (!hasPicture && !hasSignature) return "Attach a picture or sign on the whiteboard.";

        // Check everything BEFORE saving any file, so a failed submit leaves nothing behind.
        if (hasPicture)
        {
            var pictureError = FileStorage.ValidatePicture(picture!);
            if (pictureError != null) return pictureError;
        }
        if (hasSignature)
        {
            var signatureError = FileStorage.ValidateSignature(signatureData!);
            if (signatureError != null) return signatureError;
        }

        // Never trust the posted ids: re-check they belong to THIS student, THIS subject, and still need an excuse.
        var records = await _db.AttendanceRecords
            .Where(r => ids.Contains(r.Id)
                     && r.StudentId == studentId
                     && r.SubjectId == subjectId
                     && (r.ExcuseDocument == null || r.ExcuseDocument.Status == ExcuseStatus.Rejected))
            .ToListAsync();
        if (records.Count != ids.Count) return "One of those dates can't be excused.";

        var doc = new ExcuseDocument
        {
            StudentId = studentId,
            SubjectId = subjectId,
            Reason = reason,
            PictureFileName = hasPicture ? await _files.SavePictureAsync(picture!) : null,
            SignatureFileName = hasSignature ? await _files.SaveSignatureAsync(signatureData!) : null,
            DateSubmitted = DateTime.Now,
            Status = ExcuseStatus.Pending
        };

        // Attach the excuse to every selected record, then save everything in one go.
        foreach (var record in records) record.ExcuseDocument = doc;
        _db.ExcuseDocuments.Add(doc);
        await _db.SaveChangesAsync();

        return null;
    }

    // ---------- Professor side ----------

    public Task<List<ExcuseDocument>> GetPendingForProfessorAsync(string professorId)
    {
        return _db.ExcuseDocuments
            .Include(d => d.Student)
            .Include(d => d.Subject)
            .Where(d => d.Status == ExcuseStatus.Pending && d.Subject!.ProfessorId == professorId)
            .OrderBy(d => d.DateSubmitted)
            .ToListAsync();
    }

    // Only returns the excuse if it belongs to one of THIS professor's subjects.
    public Task<ExcuseDocument?> GetForReviewAsync(int id, string professorId)
    {
        return _db.ExcuseDocuments
            .Include(d => d.Student)
            .Include(d => d.Subject)
            .Include(d => d.AttendanceRecords)
            .FirstOrDefaultAsync(d => d.Id == id && d.Subject!.ProfessorId == professorId);
    }

    public async Task<string?> DecideAsync(int id, string professorId, bool accept, string? remarks)
    {
        var doc = await GetForReviewAsync(id, professorId);
        if (doc == null || doc.Status != ExcuseStatus.Pending) return "That excuse can't be reviewed.";
        if (!accept && string.IsNullOrWhiteSpace(remarks)) return "Please add remarks when rejecting.";

        doc.Status = accept ? ExcuseStatus.Accepted : ExcuseStatus.Rejected;
        doc.Remarks = remarks?.Trim();
        await _db.SaveChangesAsync();

        return null;
    }

    // ---------- File access ----------

    // Excuses can contain medical papers, so only the student who sent it and that subject's professor may open them.
    public async Task<ExcuseDocument?> GetForFileAsync(int id, string userId)
    {
        var doc = await _db.ExcuseDocuments
            .Include(d => d.Subject)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null) return null;
        bool allowed = doc.StudentId == userId || doc.Subject!.ProfessorId == userId;
        return allowed ? doc : null;
    }
}
