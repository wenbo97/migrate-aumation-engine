using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;

public class FileSystemTools
{
    [KernelFunction]
    [Description("Read the content of a file or list a directory.")]
    public async Task<string> ReadFile(
        [Description("Path to the file or directory")] string path)
    {
        try
        {
            if (File.Exists(path))
            {
                // It's a file → read content
                return await File.ReadAllTextAsync(path);
            }
            else if (Directory.Exists(path))
            {
                // It's a directory → list files & subdirectories
                var dirs = Directory.GetDirectories(path);
                var files = Directory.GetFiles(path);

                var sb = new StringBuilder();
                sb.AppendLine($"Directory: {path}");
                sb.AppendLine();
                sb.AppendLine("Subdirectories:");
                foreach (var d in dirs)
                {
                    sb.AppendLine("  [D] " + Path.GetFileName(d));
                }

                sb.AppendLine();
                sb.AppendLine("Files:");
                foreach (var f in files)
                {
                    sb.AppendLine("  [F] " + Path.GetFileName(f));
                }

                return sb.ToString();
            }

            return "Path not found.";
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    [KernelFunction]
    [Description("Overwrite the content of a file with new content.")]
    public async Task<string> WriteFile(
        [Description("Path to the file")] string path,
        [Description("The new content")] string content)
    {
        try
        {
            await File.WriteAllTextAsync(path, content);
            return "File written.";
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }
}