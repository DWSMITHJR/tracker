using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string logFile = Path.Combine(Environment.CurrentDirectory, "env_check.log");
        var sb = new StringBuilder();
        
        try
        {
            sb.AppendLine("=== Environment Check ===");
            sb.AppendLine($"Time: {DateTime.Now}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"Current Directory: {Environment.CurrentDirectory}");
            sb.AppendLine($"CLR Version: {Environment.Version}");
            
            // Test file system access
            string testFile = Path.Combine(Environment.CurrentDirectory, "test_write.txt");
            File.WriteAllText(testFile, "Test write successful");
            bool fileWriteSuccess = File.Exists(testFile);
            if (fileWriteSuccess) File.Delete(testFile);
            
            sb.AppendLine($"\nFile System Test: {(fileWriteSuccess ? "SUCCESS" : "FAILED")}");
            
            // Check .NET installation
            string[] dotnetPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet\dotnet.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet\dotnet.exe"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? "", "dotnet\dotnet.exe"),
                "dotnet.exe"
            };
            
            sb.AppendLine("\n=== .NET Check ===");
            bool dotnetFound = false;
            foreach (var path in dotnetPaths)
            {
                if (File.Exists(path))
                {
                    sb.AppendLine($"Found dotnet at: {path}");
                    dotnetFound = true;
                    break;
                }
            }
            
            if (!dotnetFound)
                sb.AppendLine("ERROR: Could not find dotnet.exe in common locations");
                
            // Check environment variables
            sb.AppendLine("\n=== Environment Variables ===");
            sb.AppendLine($"PATH: {Environment.GetEnvironmentVariable("PATH")}");
            sb.AppendLine($"DOTNET_ROOT: {Environment.GetEnvironmentVariable("DOTNET_ROOT")}");
            
            // Write results to file
            File.WriteAllText(logFile, sb.ToString());
            Console.WriteLine($"Check complete. Results saved to: {logFile}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"\n=== ERROR ===\n{ex}");
            File.WriteAllText(logFile, sb.ToString());
            Console.WriteLine($"Error during environment check. See {logFile} for details.");
        }
    }
}
