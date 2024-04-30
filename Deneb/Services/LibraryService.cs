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

    public void Add(Book book, FileInfo fileInfo)
    {
        var bookDirectory = BookDirectory(book);
        bookDirectory.Create();

        var bookFile = BookFile(book, fileInfo.Extension);
        File.Copy(fileInfo.FullName, bookFile.FullName, true);

        if (fileInfo.Extension != ".epub") 
            return;
        
        var epub = Epub.Load(bookFile);
        epub.Title = book.Title;
        epub.Creators = [new() { Name = book.Author.Name, Role = Role.Author }];
        epub.Year = book.PublishedOn;
        epub.Save();
    }
    
    public bool Contains(Book book)
    {
        return BookFile(book, ".epub").Exists || BookFile(book, ".pdf").Exists;
    }
    
    private DirectoryInfo BookDirectory(Book book)
    {
        var author = SanitizeForPath(book.Author.Name);

        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var title = SanitizeForPath(book.Title);

        if (string.IsNullOrEmpty(title))
            title = "Unknown Title";

        return new DirectoryInfo(Path.Combine(_libraryRoot.FullName, author, title));
    }

    private FileInfo BookFile(Book book, string extension)
    {
        var bookDirectory = BookDirectory(book);

        var author = SanitizeForPath(book.Author.Name);

        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var title = SanitizeForPath(book.Title);

        if (string.IsNullOrEmpty(title))
            title = "Unknown Title";

        var bookFileName = $"{author} - {title}{extension}";

        return new FileInfo(Path.Combine(bookDirectory.FullName, bookFileName));
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