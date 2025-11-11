using System;
using System.IO;

class Program
{
    static void Main()
    {
        string outputFile = Path.Combine(Environment.CurrentDirectory, "env_test.txt");
        
        using (StreamWriter writer = new StreamWriter(outputFile))
        {
            writer.WriteLine("=== Environment Test ===");
            writer.WriteLine($"Test Time: {DateTime.Now}");
            writer.WriteLine($"OS: {Environment.OSVersion}");
            writer.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            writer.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
            writer.WriteLine($"CLR Version: {Environment.Version}");
            
            try
            {
                writer.WriteLine("\n=== .NET Information ===");
                writer.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
                writer.WriteLine($"Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
                
                // Test file system access
                writer.WriteLine("\n=== File System Test ===");
                string testFile = Path.Combine(Environment.CurrentDirectory, "test_write.txt");
                File.WriteAllText(testFile, "Test write successful");
                writer.WriteLine($"File write test: {(File.Exists(testFile) ? "SUCCESS" : "FAILED")}");
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
            catch (Exception ex)
            {
                writer.WriteLine($"\n=== ERROR ===\n{ex}");
            }
        }
        
        Console.WriteLine($"Test complete. Results saved to: {outputFile}");
    }
}
