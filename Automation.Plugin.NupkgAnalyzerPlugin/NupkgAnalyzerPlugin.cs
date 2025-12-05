using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;

namespace Automation.Plugin.NupkgAnalyzerPlugin;

/// <summary>
/// Automation.Plugin.NupkgAnalyzerPlugin.
/// </summary>
public class NupkgAnalyzerPlugin
{
    private static XNamespace MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

    /// <summary>
    /// 
    /// </summary>
    private readonly ILogger<NupkgAnalyzerPlugin> logger;

    private readonly ILoggerFactory loggerFactory;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerFactory"></param>
    public NupkgAnalyzerPlugin(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        this.logger = this.loggerFactory.CreateLogger<NupkgAnalyzerPlugin>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath">Nupkg log file Path</param>
    [KernelFunction]
    [Description("Nupkg analyzer - A function that analyzes Nupkg config files.")]
    public async Task<string> NupkgAnalyzerAsync(
        [Description("The kernel.")] Kernel kernel,
        [Description("A log file contains the the paths to nupkg config files")]
        string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(filePath));
        ArgumentException.ThrowIfNullOrEmpty(nameof(kernel));

        string[] filePathsList = await File.ReadAllLinesAsync(filePath);

        this.logger.LogInformation("Read all nupkg file paths");

        List<NupkgAnalyzeModel> nupkgAnalysisModelResult = new();
        List<NupkgAnalyzeResultModel> nupkgAnalysisCountResult = new();

        foreach (string nupkgFilePath in filePathsList)
        {
            NupkgAnalyzeModel nupkgAnalyzeModel = new NupkgAnalyzeModel();
            NupkgAnalyzeResultModel resultModel = new NupkgAnalyzeResultModel();
            nupkgAnalyzeModel.NupkgFilePath = nupkgFilePath;
            string nupkgFileName = Path.GetFileName(nupkgFilePath);
            nupkgAnalyzeModel.NupkgFileName = nupkgFileName;
            resultModel.NupkgFileName = nupkgFileName;

            this.logger.LogInformation("Analyzing projects and assemblies reference in path: [{NupkgFilePath}]",
                nupkgFilePath);

            var content = await File.ReadAllTextAsync(nupkgFilePath);
            nupkgAnalyzeModel.AssemblyName = this.GetAssemblyNameFromNupkgContent(content);
            this.logger.LogInformation("Nupkg file name: [{NupkgFilePath}], Nupkg assembly name: [{AssemblyName}]",
                nupkgFileName, nupkgAnalyzeModel.AssemblyName);

            KernelFunction function = kernel.Plugins.GetFunction(pluginName: "NupkgAnalyzerPluginSemanticFunctions",
                functionName: "ExtDllAndProject");

            string keyXmlSectionContent = this.CheckAndGetKeyXmlSections(content);
            if (string.IsNullOrEmpty(keyXmlSectionContent))
            {
                this.logger.LogWarning("Cannot find any Content or ProjectReference labels. Skip to save token.");
                continue;
            }

            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
            KernelArguments arguments = new KernelArguments(settings)
            {
                ["nupkgContent"] = keyXmlSectionContent,
            };
            
            string result = await kernel.InvokeAsync<string>(function, arguments) ?? string.Empty;

            NupkgDllAndProjectReferenceRecord resultRecord =
                JsonConvert.DeserializeObject<NupkgDllAndProjectReferenceRecord>(result) ??
                new NupkgDllAndProjectReferenceRecord();

            if (resultRecord.PackagedAssemblies.Any() || resultRecord.PackagedProjects.Any())
            {
                foreach (var resultPackagedDll in resultRecord.PackagedAssemblies)
                {
                    nupkgAnalyzeModel.PackagedInternalDlls.Add(resultPackagedDll);
                }

                resultModel.PackagedInternalDllsCount = resultRecord.PackagedAssemblies.Count();
                this.logger.LogInformation("Nupkg found the packaged assemblies count: [{PackagedDllCount}]",
                    resultModel.PackagedInternalDllsCount);

                foreach (var resultPackagedProject in resultRecord.PackagedProjects)
                {
                    nupkgAnalyzeModel.PackagedInternalProjects.Add(resultPackagedProject);
                }

                resultModel.PackagedInternalProjectsCount = resultRecord.PackagedProjects.Count();
                this.logger.LogInformation("Nupkg found the packaged projects count: [{PackagedProjCount}]",
                    resultModel.PackagedInternalProjectsCount);
            }
            else
            {
                this.logger.LogInformation("No package assemblies or projects are found.");
                continue;
            }

            nupkgAnalysisModelResult.Add(nupkgAnalyzeModel);
            nupkgAnalysisCountResult.Add(resultModel);
        }

        this.logger.LogInformation("Nupkg analysis model done.");
        await File.WriteAllTextAsync($"nupkg_packageInfo_{DateTime.UtcNow:yyyy-MM-dd_HH_mm_ss}.json", JsonConvert.SerializeObject(nupkgAnalysisModelResult));

        return JsonConvert.SerializeObject(nupkgAnalysisCountResult);
    }

    /// <summary>
    ///  Extract AssemblyName from xml content.
    /// </summary>
    /// <param name="nupkgContent">Nupkg xml Content</param>
    /// <returns></returns>
    private string GetAssemblyNameFromNupkgContent(string nupkgContent)
    {
        XDocument root = XDocument.Parse(nupkgContent);
        string assemblyName = string.Empty;
        try
        {
            assemblyName = root.Descendants(MsBuildNamespace + "AssemblyName").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(assemblyName))
            {
                assemblyName = root.Descendants("AssemblyName").FirstOrDefault()?.Value;
                if (string.IsNullOrEmpty(assemblyName))
                {
                    this.logger.LogWarning("Cannot find assembly name from Nupkg content.");
                    return string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Extract Assembly Name failed.");
        }


        return assemblyName;
    }

    /// <summary>
    ///  Get XML string contains ProjectReference and Content sections.
    /// </summary>
    /// <param name="content">content.</param>
    /// <returns></returns>
    private string CheckAndGetKeyXmlSections(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            this.logger.LogInformation("Empty nupkg content.");
            return string.Empty;
        }

        XDocument filteredXml = null;
        try
        {
            XDocument nupkgXml = XDocument.Parse(content);

            var itemGroupsWithNamespace = nupkgXml.Descendants(MsBuildNamespace + "ItemGroup")
                .Where(group => group.Descendants(MsBuildNamespace + "ProjectReference").Any() ||
                                group.Descendants(MsBuildNamespace + "Content").Any())
                .ToList();

            var itemGroupsWithoutNamespace = nupkgXml.Descendants("ItemGroup")
                .Where(group => group.Descendants("ProjectReference").Any() || group.Descendants("Content").Any())
                .ToList();

            var allItemGroups = itemGroupsWithNamespace.Concat(itemGroupsWithoutNamespace).ToList();

            if (!allItemGroups.Any())
            {
                this.logger.LogWarning("No ItemGroup containing ProjectReference or Content found.");
                return string.Empty;
            }

            filteredXml = new XDocument(new XElement("Root", allItemGroups));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Extract ItemGroup which should have ProjectReference or Content failed.");
        }

        return filteredXml?.ToString(SaveOptions.DisableFormatting) ?? string.Empty;
    }

    /// <summary>
    /// NupkgSupportVersionRecord.
    /// </summary>
    private sealed record NupkgSupportVersionRecord
    {
        /// <summary>
        /// Gets or sets a value indicating whether support Net Framework Version.
        /// </summary>
        [JsonPropertyName("IsSupportNetFramework")]
        public bool IsSupportFrameworkVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether support Net Core Version.
        /// </summary>
        [JsonPropertyName("IsSupportNetCore")]
        public bool IsSupportNetCoreVersion { get; set; }
    }

    /// <summary>
    /// NupkgDllAndProjectReferenceRecord.
    /// </summary>
    private sealed record NupkgDllAndProjectReferenceRecord
    {
        /// <summary>
        /// Gets PackagedProjects.
        /// </summary>
        [JsonPropertyName("packagedProjects")]
        public IEnumerable<string> PackagedProjects { get; init; } = [];

        /// <summary>
        /// Gets PackagedDlls.
        /// </summary>
        [JsonPropertyName("packagedAssemblies")]
        public IEnumerable<string> PackagedAssemblies { get; init; } = [];
    }
}