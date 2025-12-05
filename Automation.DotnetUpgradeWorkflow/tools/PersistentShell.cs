using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

public class PersistentShell : IDisposable
{
    private const string workDirectory = "c:/src/ControlPlane";
    private Process? _process;
    private readonly ILogger _logger;
    private readonly StringBuilder _outputBuffer = new();
    private readonly Dictionary<string, string> _environmentVariables;

    // Semaphore: Used to notify ExecuteCommandAsync that the command has finished
    private TaskCompletionSource<bool>? _commandCompletionSource;
    private string? _currentSentinel;

    public PersistentShell(ILogger logger, Dictionary<string, string>? envVars = null)
    {
        _logger = logger;
        _environmentVariables = envVars ?? new Dictionary<string, string>();
    }

    public void Start()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/q",
            WorkingDirectory = workDirectory,
            UseShellExecute = false, // Must be false for I/O redirection
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true // Keep it headless
        };

        foreach (var kvp in _environmentVariables)
        {
            startInfo.Environment[kvp.Key] = kvp.Value;
        }

        _process = new Process { StartInfo = startInfo };

        // --- [Critical Logic] Listen for the sentinel in the output stream ---
        _process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                _logger.LogDebug($"[CMD]: {e.Data}");
                _outputBuffer.AppendLine(e.Data);

                // Check if it contains the "end marker" we are waiting for
                if (_currentSentinel != null && e.Data.Contains(_currentSentinel))
                {
                    _commandCompletionSource?.TrySetResult(true);
                }
            }
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) _logger.LogWarning($"[CMD ERROR]: {e.Data}");
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException("Could not start cmd.exe process.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Warm up
        _process.StandardInput.WriteLine("echo Shell Initialized");
        _process.StandardInput.WriteLine("@echo off");
    }

    public async Task ExecuteCommandAsync(string command, TimeSpan timeout)
    {
        if (_process == null || _process.HasExited)
        {
            throw new InvalidOperationException("Shell process is not running.");
        }

        // 1. Create a new wait task
        _commandCompletionSource = new TaskCompletionSource<bool>();
        // 2. Generate a unique end marker (Sentinel)
        _currentSentinel = $"__CMD_DONE_{Guid.NewGuid()}__";

        _logger.LogInformation($"Executing: {command}");

        // 3. Send command.
        // Use '&' operator: means "execute command, then (regardless of success or failure) execute echo sentinel"
        // This ensures the sentinel is printed even if the previous command errors, avoiding C# deadlock waiting.
        await _process.StandardInput.WriteLineAsync($"{command} & echo {_currentSentinel}");

        // 4. Wait for the sentinel to appear (or timeout)
        var completedTask = await Task.WhenAny(_commandCompletionSource.Task, Task.Delay(timeout));

        if (completedTask != _commandCompletionSource.Task)
        {
            // Handle timeout
            throw new TimeoutException($"Command '{command}' timed out after {timeout.TotalSeconds} seconds.");
        }

        // 5. Clean up state
        _commandCompletionSource = null;
        _currentSentinel = null;
    }

    public async Task<string> RunAgentCommandAsync(string command, TimeSpan timeSpan)
    {
        _outputBuffer.Clear();
        await ExecuteCommandAsync(command, timeSpan);
        return _outputBuffer.ToString();
    }

    public void Dispose()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited) _process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing shell.");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }
    }
}