using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

class Program
{
    static void Main(string[] args)
    {
        int port = 5132; // The port we're interested in
        
        Console.WriteLine($"Checking for processes using port {port}...");
        
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = properties.GetActiveTcpListeners();
            
            bool found = false;
            
            foreach (var listener in listeners)
            {
                if (listener.Port == port)
                {
                    found = true;
                    Console.WriteLine($"Port {port} is in use by a listener.");
                    break;
                }
            }
            
            if (!found)
            {
                Console.WriteLine($"Port {port} is not in use by any listener.");
            }
            
            // Check for active connections as well
            var connections = properties.GetActiveTcpConnections();
            found = false;
            
            foreach (var conn in connections)
            {
                if (conn.LocalEndPoint.Port == port)
                {
                    found = true;
                    Console.WriteLine($"Port {port} is in use by a connection (State: {conn.State}).");
                    
                    try
                    {
                        var process = Process.GetProcessById(conn.OwningPid);
                        Console.WriteLine($"Process ID: {process.Id}, Name: {process.ProcessName}, Path: {process.MainModule?.FileName}");
                        
                        Console.Write("Do you want to terminate this process? (Y/N): ");
                        var key = Console.ReadKey();
                        Console.WriteLine();
                        
                        if (key.Key == ConsoleKey.Y)
                        {
                            process.Kill();
                            Console.WriteLine("Process terminated.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not get process information: {ex.Message}");
                    }
                }
            }
            
            if (!found)
            {
                Console.WriteLine($"Port {port} is not in use by any connections.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
