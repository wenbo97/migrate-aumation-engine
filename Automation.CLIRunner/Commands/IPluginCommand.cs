using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Automation.CliRunner.Commands;

public interface IPluginCommand
{
    /// <summary>
    /// Root command name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 
    /// </summary>
    string PluginTaskWorkRoot { get; }
    
    /// <summary>
    /// 
    /// </summary>
    string PluginMarkdownName { get; }
    

    ILogger Logger { get; set; }

    protected const string TaskMarkdownTopRootPath = "TaskMarkdown";

    protected const int SucceededCode = 1;

    protected const int FailedCode = 0;

    /// <summary>
    /// Create and build root command.
    /// </summary>
    /// <param name="kernel">Kernel.</param>
    /// <param name="loggerFactory">loggerFactory.</param>
    /// <returns></returns>
    public Command BuildPluginCommand(Kernel kernel, ILoggerFactory loggerFactory);
}