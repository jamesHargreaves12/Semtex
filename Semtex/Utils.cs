using CliWrap;
using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex;

internal sealed class Utils
{
    private static readonly ILogger<Utils> Logger = SemtexLog.LoggerFactory.CreateLogger<Utils>();
    public static void EnsureDirectoryExistsAndEmpty(string directoryPath)
    {
        Logger.LogInformation("Using temporary path {DirectoryPath}", directoryPath);
        if (!Directory.Exists(directoryPath))
        {
            Logger.LogInformation("Directory doesn't exist, creating it");
            Directory.CreateDirectory(directoryPath);
        }
        else
        {
            Logger.LogInformation("Directory already exists, cleaning it up");
            var directoryInfo = new DirectoryInfo(directoryPath);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }
    
    internal static bool IsRepoUrl(string s)
    {
        return s.EndsWith(".git"); 
    }

}