using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;

using Deneb.Commands;

namespace Deneb;

internal abstract class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;

            var rootCommand = new RootCommand
            {
                new DownloadCommand(),
                new ConfigCommand(),
                // new CoverCommand(),
                // new UpgradeCommand(),
                // new SuggestCommand(),
                // new StatsCommand()
            };

            var commandLine = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .Build();

            return await commandLine.InvokeAsync(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
        }
    }
}