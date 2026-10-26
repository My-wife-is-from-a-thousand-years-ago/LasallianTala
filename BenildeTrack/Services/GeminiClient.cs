using System.Text;
using System.Text.Json;

namespace BenildeTrack.Services;

// The ONLY class that knows Gemini exists. Returns null on any failure so callers can fall back.
public class GeminiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient http, IConfiguration config, ILogger<GeminiClient> logger)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"] ?? "";
        _model = config["Gemini:Model"] ?? "gemini-3.5-flash";
        _logger = logger;
    }

    public async Task<string?> AskAsync(string systemPrompt, string userPrompt)
    {
        if (_apiKey == "") return null;

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                // 400/403 = bad key, 404 = wrong model name, 429 = quota used up.
                _logger.LogWarning("Gemini returned HTTP {Status}", (int)response.StatusCode);
                return null;
            }

            // The answer can come in several "parts", so join all the text parts.
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var parts = json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
            var text = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var t)) text.Append(t.GetString());
            }

            var answer = text.ToString().Trim();
            return answer == "" ? null : answer;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini call failed");
            return null;
        }
    }
}