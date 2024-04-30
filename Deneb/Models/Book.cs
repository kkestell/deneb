using Deneb.Commands;
using Deneb.Services;

namespace Deneb.Models;

public class Book
{
    public string Id { get; }

    public Author Author { get; }
    
    public string Title { get; }
    
    public int? PublishedOn { get; }
    
    public BookType Type { get; }
    
    private Book(string id, Author author, string title, int? publishedOn, BookType type)
    {
        Id = id;
        Author = author;
        Title = title;
        PublishedOn = publishedOn;
        Type = type;
    }
    
    public static Book FromSearchResult(SearchResult result)
    {
        return new Book(result.Identifier, new Author(result.Authors.First()), result.Title, result.FirstPublishedOn, result.Type);
    }
}
