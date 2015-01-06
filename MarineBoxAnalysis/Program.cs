using System;
using System.Threading;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.IO;

public class MonitorSample
{

    public static string IPNetwork = ""; 
    public static string baseIP = "";
    public static List<string> activeIP = new List<string>();
    
    public static void Main(String[] args)
    {
        int result = 0;   // Result initialized to say there is no error
        string fn = "ResultOfPing.txt";
        File.Delete(fn);
        
        // Threads producer and consumer have been created, 
        // but not started at this point.
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            Console.WriteLine();
            Console.WriteLine(ni.Name);
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            String IPv4 = "";
            Console.WriteLine("Operational? {0}", ni.OperationalStatus == OperationalStatus.Up);
            Console.WriteLine("MAC: {0}", ni.GetPhysicalAddress());
            Console.WriteLine("Gateways:");
            foreach (GatewayIPAddressInformation gipi in ni.GetIPProperties().GatewayAddresses)
            {
                IPNetwork += gipi.Address + Environment.NewLine;
                Console.WriteLine("\t{0}", gipi.Address);
            }
            IPNetwork += Environment.NewLine +  "IP Addresses: ";
            Console.WriteLine("IP Addresses:");
            foreach (UnicastIPAddressInformation uipi in ni.GetIPProperties().UnicastAddresses)
            {
                if (uipi.Address.AddressFamily.ToString() != "InterNetworkV6")
                {
                    if (uipi.Address.ToString() == "127.0.0.1") continue;
                    IPv4 = uipi.Address.ToString();
                    IPNetwork += uipi.Address.ToString() + " Subnet Mask: " + uipi.IPv4Mask.ToString() + Environment.NewLine;
                    Console.WriteLine("\t{0} / {1}", uipi.Address, uipi.IPv4Mask);
                }
            }

            if (IPv4 == "" || IPv4 == "127.0.0.1") continue;

            //ping entire network of NICs

            baseIP = IPv4.Substring(0, IPv4.LastIndexOf(".") + 1);

            Thread producer = new Thread(new ThreadStart(MonitorSample.TryPinging));
            Thread consumer = new Thread(new ThreadStart(MonitorSample.TryPinging2));
            Thread thread3 = new Thread(new ThreadStart(MonitorSample.TryPinging3));
            try
            {
                producer.Start();
                consumer.Start();
                thread3.Start();
                producer.Join();   // Join both threads with no timeout
                // Run both until done.
                consumer.Join();
                thread3.Join();
                // threads producer and consumer have finished at this point.
            }
            catch (ThreadStateException e)
            {
                Console.WriteLine(e);  // Display text of exception
                result = 1;            // Result says there was an error
            }
            catch (ThreadInterruptedException e)
            {
                Console.WriteLine(e);  // This exception means that the thread
                // was interrupted during a Wait
                result = 1;            // Result says there was an error
            }
            Console.WriteLine("Found " + activeIP.Count.ToString());
           
        }
        // Even though Main returns void, this provides a return code to 
        // the parent process.
        Environment.ExitCode = result;


        File.AppendAllText(fn, IPNetwork);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("==================     FINISH    ===========================");
        Console.WriteLine("Please send the file \"ResultOfPing.txt\" for Wallem support");
        Console.WriteLine("Please Press Any Key to quit");
        Console.ReadLine();
    }

    public static void TryPinging()
    {
        for (int i = 1; i < 75; i++)
        {
            string ip = baseIP +i.ToString();
            TryPing0(i,ip);
        }
    }
    public static void TryPinging2()
    {
        for (int i = 76; i < 154; i++)
        {
            string ip = baseIP + i.ToString();
            TryPing0(i, ip);
        }
    }
    public static void TryPinging3()
    {
        for (int i = 155; i < 256; i++)
        {
            string ip = baseIP + i.ToString();
            TryPing0(i, ip);
        }
    }
    public static void TryPing0(int index, string destinationIP)
    {
        Ping p = new Ping();
        PingReply reply = p.Send(destinationIP,250);
        if (reply.Status == IPStatus.Success)
        {
            Console.WriteLine(string.Concat("Active IP: ", reply.Address.ToString()));
            object sync = new Object();
            lock (sync)
            {
                activeIP.Add(reply.Address.ToString());
                IPNetwork += Environment.NewLine + reply.Address.ToString();
            }
        }
        else
        {
            IPNetwork += Environment.NewLine + "Skipped: " + destinationIP;
        }    
    }

}
