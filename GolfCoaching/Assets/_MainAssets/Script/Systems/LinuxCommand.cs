using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class LinuxCommand
{
    public static string Run(string command)
    {
        // Check if the system is running on Linux
        if (System.Environment.OSVersion.Platform != PlatformID.Unix)
        {
            UnityEngine.Debug.Log("Unsupported platform: This function is intended for Linux systems only.");
            return "";
        }

        // Create process start info
        ProcessStartInfo psi = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"");
        psi.WorkingDirectory = "/"; 
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        // Start the process
        Process process = new Process();
        process.StartInfo = psi;
        process.Start();

        // Read the output
        string output = process.StandardOutput.ReadToEnd();

        // Wait for the process to finish
        process.WaitForExit();

        string errors = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(errors))
        {
            UnityEngine.Debug.LogError("Command execution error: " + errors);
        }

        return output;
    }

    public static string RunDirect(string fileName, string arguments)
    {
        return RunDirect(fileName, arguments, 3000);
    }

    public static string RunDirect(string fileName, string arguments, int timeoutMilliseconds)
    {
        if (System.Environment.OSVersion.Platform != PlatformID.Unix)
        {
            UnityEngine.Debug.Log("Unsupported platform");
            return "";
        }

        ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.WorkingDirectory = "/";

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    UnityEngine.Debug.LogWarning($"Process timeout: {fileName} {arguments}");
                    return "";
                }

                string output = outputTask.GetAwaiter().GetResult();
                string errors = errorTask.GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(errors))
                {
                    UnityEngine.Debug.LogError(errors);
                }

                return output;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Process error: " + ex.Message);
            return "";
        }
    }


    public static async Task<string> RunAsync(string command)
    {
        // Check if the system is running on Linux
        if (System.Environment.OSVersion.Platform != PlatformID.Unix)
        {
            UnityEngine.Debug.Log("Unsupported platform: This function is intended for Linux systems only.");
            return "";
        }

        ProcessStartInfo psi = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"");
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        Process process = new Process();
        process.StartInfo = psi;
        process.Start();

        // Asynchronously read the output
        string output = await process.StandardOutput.ReadToEndAsync();

        // Wait for the process to finish
        await Task.Run(() => process.WaitForExit());

        string errors = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(errors))
        {
            UnityEngine.Debug.LogError("Command execution error: " + errors);
        }

        return output;
    }
}