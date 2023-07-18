using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex;

internal sealed class Utils
{
    private static readonly ILogger<Utils> Logger = SemtexLog.LoggerFactory.CreateLogger<Utils>();
    public static void EnsureDirectoryExistsAndEmpty(AbsolutePath directoryPath)
    {
        Logger.LogInformation("Using temporary path {DirectoryPath}", directoryPath.Path);
        if (!Directory.Exists(directoryPath.Path))
        {
            Logger.LogInformation("Directory doesn't exist, creating it");
            Directory.CreateDirectory(directoryPath.Path);
        }
        else
        {
            Logger.LogInformation("Directory already exists, cleaning it up");
            var directoryInfo = new DirectoryInfo(directoryPath.Path);
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