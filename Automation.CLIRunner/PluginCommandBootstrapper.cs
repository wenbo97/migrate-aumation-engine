using System.CommandLine;
using Automation.CliRunner.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Automation.CliRunner;

public sealed class PluginCommandBootstrapper
{
    /// <summary>
    /// 
    /// </summary>
    public static ILogger<PluginCommandBootstrapper> logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> InitPluginCommandConfiguer(Kernel kernel, ILoggerFactory loggerFactory, string[] args)
    {
        logger = loggerFactory.CreateLogger<PluginCommandBootstrapper>();

        var root = new RootCommand("Automation CLI");

        var pluginCommands = new IPluginCommand[]
        {
            new NupkgPluginCommand(),
            new DotnetUpgradePluginCommand()
        };

        foreach (var pluginCommand in pluginCommands)
        {
            root.Add(pluginCommand.BuildPluginCommand(kernel, loggerFactory));
        }

        return await root.Parse(args).InvokeAsync();
    }
}