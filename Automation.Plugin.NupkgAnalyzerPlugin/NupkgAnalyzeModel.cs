using System.Collections.ObjectModel;

using Newtonsoft.Json;

namespace Automation.Plugin.NupkgAnalyzerPlugin;

/// <summary>
/// NupkgAnalyzeMode.
/// </summary>
public class NupkgAnalyzeModel
{
    /// <summary>
    /// Gets or sets assembly name.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Nupkg content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Nupkg file name.
    /// </summary>
    public string NupkgFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Nupkg file path.
    /// </summary>
    public string NupkgFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating involved projects.
    /// </summary>
    public Collection<string> Projects { get; } = new();

    /// <summary>
    /// Gets a value indicating the nupkg packaged projects.
    /// </summary>
    public Collection<string> PackagedInternalProjects { get; } = new();

    /// <summary>
    /// Gets a value indicating the nupkg packaged dlls.
    /// </summary>
    public Collection<string> PackagedInternalDlls { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether support Net Framework Version.
    /// </summary>
    [JsonProperty("IsSupportNetFramework")]
    public bool IsSupportFrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether support Net Core Version.
    /// </summary>
    [JsonProperty("IsSupportNetCore")]
    public bool IsSupportNetCoreVersion { get; set; }
}


internal class NupkgAnalyzeResultModel
{
    /// <summary>
    /// Gets or sets Nupkg file name.
    /// </summary>
    public string NupkgFileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the packaged projects count.
    /// </summary>
    public int PackagedInternalProjectsCount { get; set; }
    
    /// <summary>
    /// Gets or sets the packaged assembly count.
    /// </summary>
    public int PackagedInternalDllsCount { get; set; }
}
