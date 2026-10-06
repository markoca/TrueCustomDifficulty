using System.IO;

namespace TrueCustomDifficulty.Services;

public sealed class ExternalToolsService
{
    public string ToolsDirectory => Path.Combine(AppContext.BaseDirectory, "tools");

    public string Require(string name)
    {
        Directory.CreateDirectory(ToolsDirectory);

        var candidates = new[]
        {
            Path.Combine(ToolsDirectory, name),
            Path.Combine(ToolsDirectory, name + ".exe")
        };

        var found = candidates.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(found))
            return found;

        throw new InvalidDataException(
            $"Required external tool '{name}' was not found in the tools folder." + Environment.NewLine +
            $"Download it separately and place '{name}.exe' in:" + Environment.NewLine +
            $"  {ToolsDirectory}");
    }
}
