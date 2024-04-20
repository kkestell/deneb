using System.CommandLine.Invocation;
using Deneb.Configuration;
using Deneb.Services;

namespace Deneb.Commands.Handlers;

public class ConfigCommandHandler : ICommandHandler
{
    public Task<int> InvokeAsync(InvocationContext context)
    {
        var configPath = DenebConfig.ConfigPath;
        var config = DenebConfig.Load();

        Log.Information($"Configuration loaded from {configPath}");
        Log.Information($"Library path: {config.LibraryPath}");

        return Task.FromResult(0);
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }
}