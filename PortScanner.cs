using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int[] portsToCheck = { 5132, 5002, 5003, 5000, 5001, 7060, 7229 };
        
        Console.WriteLine("Port Scanner - Checking for processes using specific ports");
        Console.WriteLine("=======================================================\n");
        
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            
            // Check TCP listeners
            Console.WriteLine("Active TCP Listeners:");
            Console.WriteLine("--------------------");
            var listeners = properties.GetActiveTcpListeners();
            
            bool foundAny = false;
            
            foreach (var port in portsToCheck)
            {
                var listener = listeners.FirstOrDefault(l => l.Port == port);
                if (listener != null)
                {
                    foundAny = true;
                    Console.WriteLine($"Port {port} is in use by a TCP listener.");
                    
                    // Try to find the process using this port
                    FindAndDisplayProcess(port);
                }
            }
            
            if (!foundAny)
            {
                Console.WriteLine("No active TCP listeners found on the specified ports.");
            }
            
            Console.WriteLine("\nActive TCP Connections:");
            Console.WriteLine("----------------------");
            var connections = properties.GetActiveTcpConnections();
            foundAny = false;
            
            foreach (var port in portsToCheck)
            {
                var connection = connections.FirstOrDefault(c => c.LocalEndPoint.Port == port);
                if (connection != null)
                {
                    foundAny = true;
                    Console.WriteLine($"Port {port} is in use by a TCP connection (State: {connection.State}).");
                    
                    // Try to find the process using this port
                    FindAndDisplayProcess(port);
                }
            }
            
            if (!foundAny)
            {
                Console.WriteLine("No active TCP connections found on the specified ports.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static void FindAndDisplayProcess(int port)
    {
        try
        {
            // Use netstat to find the process ID
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = $"-ano | findstr :{port}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                // Parse the output to get the PID
                string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                    {
                        try
                        {
                            var processInfo = Process.GetProcessById(pid);
                            Console.WriteLine($"  Process ID: {pid}, Name: {processInfo.ProcessName}");
                            Console.WriteLine($"  Path: {processInfo.MainModule?.FileName}");
                            
                            Console.Write("  Do you want to terminate this process? (Y/N): ");
                            var key = Console.ReadKey();
                            Console.WriteLine();
                            
                            if (key.Key == ConsoleKey.Y)
                            {
                                processInfo.Kill();
                                Console.WriteLine($"  Process {pid} terminated.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  Could not get process information for PID {pid}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error finding process: {ex.Message}");
        }
    }
}
