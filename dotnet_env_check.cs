using System;
using System.IO;

class Program
{
    static void Main()
    {
        string outputFile = Path.Combine(Environment.CurrentDirectory, "dotnet_env_check.txt");
        
        using (StreamWriter writer = new StreamWriter(outputFile))
        {
            writer.WriteLine("=== .NET Environment Check ===");
            writer.WriteLine("Current Time: " + DateTime.Now);
            writer.WriteLine("OS Version: " + Environment.OSVersion);
            writer.WriteLine("64-bit OS: " + Environment.Is64BitOperatingSystem);
            writer.WriteLine("Current Directory: " + Environment.CurrentDirectory);
            writer.WriteLine("CLR Version: " + Environment.Version);
            
            // Check .NET Framework
            string frameworkPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), 
                @"Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll");
                
            writer.WriteLine("\n=== .NET Framework Check ===");
            writer.WriteLine("mscorlib.dll exists: " + File.Exists(frameworkPath));
            
            if (File.Exists(frameworkPath))
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(frameworkPath);
                writer.WriteLine("mscorlib.dll version: " + versionInfo.FileVersion);
            }
            
            // Check .NET Core/5+
            writer.WriteLine("\n=== .NET Core/5+ Check ===");
            string[] dotnetPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"dotnet\dotnet.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"dotnet\dotnet.exe"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? "", @"dotnet\dotnet.exe"),
                "dotnet.exe"
            };
            
            bool dotnetFound = false;
            foreach (var path in dotnetPaths)
            {
                if (File.Exists(path))
                {
                    writer.WriteLine("Found dotnet at: " + path);
                    dotnetFound = true;
                    break;
                }
            }
            
            if (!dotnetFound)
            {
                writer.WriteLine("dotnet.exe not found in common locations");
            }
            
            // Environment variables
            writer.WriteLine("\n=== Environment Variables ===");
            writer.WriteLine("PATH: " + (Environment.GetEnvironmentVariable("PATH") ?? "Not set"));
            writer.WriteLine("DOTNET_ROOT: " + (Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "Not set"));
            
            // File system test
            writer.WriteLine("\n=== File System Test ===");
            string testFile = Path.Combine(Environment.CurrentDirectory, "test_file.txt");
            try 
            {
                File.WriteAllText(testFile, "Test content");
                bool fileWriteSuccess = File.Exists(testFile);
                if (fileWriteSuccess) 
                {
                    File.Delete(testFile);
                    writer.WriteLine("File system write test: SUCCESS");
                }
                else
                {
                    writer.WriteLine("File system write test: FAILED (File not created)");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine("File system write test: ERROR - " + ex.Message);
            }
            
            writer.WriteLine("\n=== Check Complete ===");
        }
        
        Console.WriteLine("Check complete. Results saved to: " + outputFile);
    }
}
