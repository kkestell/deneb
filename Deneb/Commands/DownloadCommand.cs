using System.CommandLine;
using Deneb.Commands.Handlers;
using Deneb.Models;

namespace Deneb.Commands;

public class DownloadCommand : Command
{
    public static readonly Option<string> Title = new("--title", "Title")
    {
        IsRequired = true
    };

    public static readonly Option<string> Author = new("--author", "Author");

    public static readonly Option<BookType> Type = new("--type", "Type of book to download");

    public static readonly Option<bool> Interactive = new("--interactive", "Interactive mode");

    public DownloadCommand() : base("download", "Download a book")
    {
        Type.SetDefaultValue(BookType.Fiction);
        
        AddOption(Title);
        AddOption(Author);
        AddOption(Type);
        AddOption(Interactive);
        
        Handler = new DownloadCommandHandler();
    }
}
