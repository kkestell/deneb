using Deneb.Models;

namespace Deneb.Services;

public class OpenLibraryService
{
    private readonly SearchClient _client = new();
    
    public async Task<Book?> FindBookById(string id)
    {
        // var result = await _client.FindById(id);
        // return result is null ? null : Book.FromSearchResult(result);

        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Book>> FindBooks(BookType type, string? title, string? author)
    {
        var results = await _client.FindBooks(type, title, author);
        
        if (results.Count == 0)
            return new List<Book>();
        
        return results.Where(x => x.Authors.Count > 0).Select(Book.FromSearchResult).ToList();
    }
}