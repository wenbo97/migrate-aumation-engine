using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Automation.DotnetUpgradeWorkflow.Tools;

public class ShellPlugin(PersistentShell shell)
{

    [DisplayName("ExecuteCommand")]
    [return: Description("The standard output (stdout) and standard error (stderr) of the command.")]
    public async Task<string> ExecuteCommandAsync(
        [Description("The command string to execute. RESTRICTED TO: 'bcc', 'build', or 'dir'.")]
        string command,
        [Description("The maximum duration to wait for the command to complete (format: 'hh:mm:ss', e.g., '00:15:00').")]
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(nameof(shell));
        
        var allowedCommands = new[] { "bcc", "build", "dir", "cd" };
        var cmdLower = command.Trim().ToLowerInvariant();

        bool isAllowed = allowedCommands.Any(c => cmdLower.StartsWith(c));

        if (!isAllowed)
        {
            throw new ArgumentException($"Command '{command}' is not allowed. Only 'bcc', 'build', and 'dir' are permitted.");
        }

        return await shell.RunAgentCommandAsync(command, timeout);
    }
}