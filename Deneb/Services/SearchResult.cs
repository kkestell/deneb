namespace Deneb.Services;

public class SearchResult
{
    public string Identifier { get; set; }
    public string? Isbn { get; set; }
    public string Title { get; set; }
    public List<string> Authors { get; set; }
    public int? PublishedOn { get; set; }
    public int? FirstPublishedOn { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }
    public SearchResultCover? Cover { get; set; }
}