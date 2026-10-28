using Microsoft.AspNetCore.Http;

namespace BenildeTrack.Services;

// Checks and saves excuse pictures and signatures. If you move to cloud storage later, only this class changes.
public class FileStorage
{
    private const long MaxPictureBytes = 5 * 1024 * 1024;
    private const int MaxSignatureChars = 700_000;
    private const string SignaturePrefix = "data:image/png;base64,";

    private readonly string _folder;

    public FileStorage(IWebHostEnvironment env)
    {
        // App_Data is NOT inside wwwroot, so no URL can reach these files directly.
        // They are only served through ExcusesController, which checks who is asking.
        _folder = Path.Combine(env.ContentRootPath, "App_Data", "excuses");
        Directory.CreateDirectory(_folder);
    }

    // ---------- Checks (return an error message, or null when the file is fine) ----------

    public static string? ValidatePicture(IFormFile file)
    {
        if (file.Length == 0) return "That picture is empty.";
        if (file.Length > MaxPictureBytes) return "That picture is too big (max 5 MB).";

        using var stream = file.OpenReadStream();
        if (DetectImageExtension(stream) == null) return "Only real JPG or PNG pictures are allowed.";

        return null;
    }

    public static string? ValidateSignature(string dataUrl)
    {
        if (dataUrl.Length > MaxSignatureChars) return "The signature is too big.";
        if (!dataUrl.StartsWith(SignaturePrefix, StringComparison.Ordinal)) return "The signature is not a valid image.";

        var bytes = TryDecode(dataUrl);
        if (bytes == null || DetectImageExtension(new MemoryStream(bytes)) != ".png") return "The signature is not a valid image.";

        return null;
    }

    // ---------- Saving (call the checks first) ----------

    public async Task<string> SavePictureAsync(IFormFile file)
    {
        string extension;
        using (var peek = file.OpenReadStream())
        {
            extension = DetectImageExtension(peek) ?? ".jpg";
        }

        // Random name, so nobody can guess or overwrite another person's file.
        var storedName = Guid.NewGuid().ToString("N") + extension;
        using var stream = File.Create(Path.Combine(_folder, storedName));
        await file.CopyToAsync(stream);
        return storedName;
    }

    public async Task<string> SaveSignatureAsync(string dataUrl)
    {
        var bytes = TryDecode(dataUrl) ?? Array.Empty<byte>();
        var storedName = Guid.NewGuid().ToString("N") + ".png";
        await File.WriteAllBytesAsync(Path.Combine(_folder, storedName), bytes);
        return storedName;
    }

    // Path.GetFileName strips any folder parts, so a bad value cannot escape the uploads folder.
    public string GetPath(string storedName) => Path.Combine(_folder, Path.GetFileName(storedName));

    public static string ContentTypeOf(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";
    }

    // ---------- Helpers ----------

    // Looks at the first bytes to tell a real PNG or JPEG from a renamed file. Returns null for anything else.
    private static string? DetectImageExtension(Stream stream)
    {
        var header = new byte[4];
        int read = stream.Read(header, 0, header.Length);

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
        if (read == 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return ".png";
        return null;
    }

    private static byte[]? TryDecode(string dataUrl)
    {
        try
        {
            return Convert.FromBase64String(dataUrl[SignaturePrefix.Length..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
