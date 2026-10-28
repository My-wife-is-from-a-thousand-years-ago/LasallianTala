namespace BenildeTrack.Models;

public class HandbookSection
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";

    // Extra words people might type, for example "yell, shouting, loud". Search checks these too (free and fast).
    public string Keywords { get; set; } = "";

    public string Text { get; set; } = "";
}
