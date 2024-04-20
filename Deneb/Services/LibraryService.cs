using Deneb.Ebooks;
using Deneb.Models;

namespace Deneb.Services;

public class LibraryService
{
    private readonly DirectoryInfo _libraryRoot;
    
    public LibraryService(DirectoryInfo libraryRoot)
    {
        _libraryRoot = libraryRoot;
    }

    public void Add(Book book, string filePath)
    {
        var bookDirectory = BookDirectory(book);
        bookDirectory.Create();

        var bookFile = BookFile(book);
        File.Copy(filePath, bookFile.FullName, true);

        var epub = Epub.Load(new FileInfo(filePath));
        
        epub.Title = book.Title;
        epub.Creators = [new() { Name = book.Author.Name, Role = Role.Author }];
        epub.Year = book.PublishedOn;
        epub.Save();
    }
    
    public bool Contains(Book book)
    {
        var bookFile = BookFile(book);
        return bookFile.Exists;
    }
    
    private DirectoryInfo BookDirectory(Book book)
    {
        var author = SanitizeForPath(book.Author.Name);

        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var title = SanitizeForPath(book.Title);

        if (string.IsNullOrEmpty(title))
            title = "Unknown Title";

        DirectoryInfo bookDirectory;

        // if (!string.IsNullOrEmpty(book.Series) && book.SeriesNumber.HasValue)
        // {
        //     var series = SanitizeForPath(book.Series);
        //     bookDirectory = new DirectoryInfo(Path.Combine(_libraryRoot.FullName, author,
        //         $"{series} {book.SeriesNumber} - {title}"));
        // }
        // else
        {
            bookDirectory = new DirectoryInfo(Path.Combine(_libraryRoot.FullName, author, title));
        }

        return bookDirectory;
    }

    private FileInfo BookFile(Book book)
    {
        var bookDirectory = BookDirectory(book);

        var author = SanitizeForPath(book.Author.Name);

        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var title = SanitizeForPath(book.Title);

        if (string.IsNullOrEmpty(title))
            title = "Unknown Title";

        string bookFileName;

        // if (!string.IsNullOrEmpty(book.Series) && book.SeriesNumber.HasValue)
        // {
        //     var series = SanitizeForPath(book.Series);
        //     bookFileName = $"{author} - {series} {book.SeriesNumber} - {title}.epub";
        // }
        // else
        {
            bookFileName = $"{author} - {title}.epub";
        }

        return new FileInfo(Path.Combine(bookDirectory.FullName, bookFileName));
    }

    private FileInfo CoverFile(Book book)
    {
        var bookDirectory = BookDirectory(book);
        return new FileInfo(Path.Combine(bookDirectory.FullName, "cover.jpg"));
    }

    private static string SanitizeForPath(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized =
            invalidChars.Aggregate(input, (current, invalidChar) => current.Replace(invalidChar.ToString(), ""));

        if (sanitized.Length > 32)
            sanitized = sanitized[..32];

        sanitized = sanitized.Trim('.');
        sanitized = sanitized.Trim();

        return sanitized;
    }
}