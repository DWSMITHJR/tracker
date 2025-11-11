using System;
using System.IO;

class Program
{
    static void Main()
    {
        string outputFile = Path.Combine(Environment.CurrentDirectory, "dotnet_check_output.txt");
        
        using (StreamWriter writer = new StreamWriter(outputFile))
        {
            writer.WriteLine("=== .NET Installation Check ===");
            writer.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
            writer.WriteLine($"OS Version: {Environment.OSVersion}");
            writer.WriteLine($".NET Version: {Environment.Version}");
            writer.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            writer.WriteLine($"64-bit Process: {Environment.Is64BitProcess}");
            
            // Check for .NET Framework
            string frameworkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), 
                                             "Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll");
            writer.WriteLine($"\n=== .NET Framework Check ===");
            writer.WriteLine($"mscorlib.dll exists: {File.Exists(frameworkPath)}");
            
            if (File.Exists(frameworkPath))
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(frameworkPath);
                writer.WriteLine($"mscorlib.dll version: {versionInfo.FileVersion}");
            }
            
            // Check for .NET Core/.NET 5+
            writer.WriteLine("\n=== .NET Core/.NET 5+ Check ===");
            try
            {
                string dotnetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                                              "dotnet\dotnet.exe");
                writer.WriteLine($"dotnet.exe exists: {File.Exists(dotnetPath)}");
                
                if (File.Exists(dotnetPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dotnetPath,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        process.WaitForExit();
                        string output = process.StandardOutput.ReadToEnd();
                        writer.WriteLine($"dotnet --version: {output.Trim()}");
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error checking .NET Core: {ex.Message}");
            }
            
            writer.WriteLine("\n=== Environment Variables ===");
            writer.WriteLine($"PATH: {Environment.GetEnvironmentVariable("PATH")}");
            
            writer.WriteLine("\n=== Check completed ===");
        }
        
        Console.WriteLine($"Check complete. Results written to: {outputFile}");
    }
}
