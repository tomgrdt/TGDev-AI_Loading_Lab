namespace TGDev.Step02.DatabaseStorage.Models;

public class NewsItemModel
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? Category { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public string? SourceName { get; set; }
    public string?  SourceUrl { get; set; }
    public bool IsVectorized { get; set; } = false;
}
