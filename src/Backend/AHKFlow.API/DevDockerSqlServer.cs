using System.Diagnostics;
using Serilog;

namespace AHKFlow.API;

/// <summary>
/// Development helper that starts the SQL Server Docker container
/// when the AHKFLOW_START_DOCKER_SQL environment variable is set to "true".
/// Used by the "https + Docker SQL (Recommended)" launch profile.
/// </summary>
internal static class DevDockerSqlServer
{
    internal static void EnsureStarted(string contentRootPath)
    {
        string? composeDir = FindComposeDirectory(contentRootPath);
        if (composeDir is null)
        {
            Log.Warning("docker-compose.yml not found. Cannot start SQL Server container.");
            return;
        }

        Log.Information("Starting SQL Server Docker container from {ComposeDir}...", composeDir);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose up sqlserver -d --wait",
                WorkingDirectory = composeDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                Log.Information("[Docker] {Data}", e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                Log.Information("[Docker] {Data}", e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            Log.Information("SQL Server Docker container is ready.");
        }
        else
        {
            Log.Error("Failed to start SQL Server Docker container (exit code: {ExitCode}).", process.ExitCode);
        }
    }

    private static string? FindComposeDirectory(string startingDirectory)
    {
        string? dir = startingDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "docker-compose.yml")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }
}
