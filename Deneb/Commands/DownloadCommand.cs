using System.CommandLine;
using Deneb.Commands.Handlers;

namespace Deneb.Commands;

public class DownloadCommand : Command
{
    public static readonly Option<string> Title = new("--title", "Title")
    {
        IsRequired = true
    };

    public static readonly Option<string> Author = new("--author", "Author");

    public static readonly Option<bool> Interactive = new("--interactive", "Interactive mode");

    public DownloadCommand() : base("download", "Download one or more books")
    {
        AddOption(Title);
        AddOption(Author);
        AddOption(Interactive);
        
        Handler = new DownloadCommandHandler();
    }
}
