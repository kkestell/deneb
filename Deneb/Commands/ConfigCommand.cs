using System.CommandLine;
using Deneb.Commands.Handlers;

namespace Deneb.Commands;

public class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Display configuration file path and values")
    {
        Handler = new ConfigCommandHandler();
    }
}