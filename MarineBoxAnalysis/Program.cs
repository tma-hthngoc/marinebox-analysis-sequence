using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using Microsoft.Win32;
using System.IO;

namespace PingTest
{
    public class PingNetwork
    {
        public static List<Ping> pingers = new List<Ping>();
        public static int instances = 0;

        public static object @lock = new object();

        public static int result = 0;
        public static int timeOut = 250;

        public static int ttl = 5;
        public static string totallines = "";

        public static void Ping_completed(object s, PingCompletedEventArgs e)
        {
            lock (@lock)
            {
                instances -= 1;
            }

            if (e.Reply.Status == IPStatus.Success)
            {
                Console.WriteLine(string.Concat("Active IP: ", e.Reply.Address.ToString()));
                totallines += string.Concat("Active IP: ", e.Reply.Address.ToString() + Environment.NewLine);
                result += 1;
            }
            else
            {
                //Console.WriteLine(String.Concat("Non-active IP: ", e.Reply.Address.ToString()))
            }
        }


        public static void CreatePingers(int cnt)
        {
            for (int i = 1; i <= cnt; i++)
            {
                Ping p = new Ping();
                p.PingCompleted += Ping_completed;
                pingers.Add(p);
            }
        }

        public static void DestroyPingers()
        {
            foreach (Ping p in pingers)
            {
                p.PingCompleted -= Ping_completed;
                p.Dispose();
            }

            pingers.Clear();

        }
    }

    class Program
    {

        public static void Main()
        {
            string fn = "ResultOfPing.txt";
            File.Delete(fn);
            String IPNetwork = "";
            List<string> activeIP = new List<string>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                Console.WriteLine(ni.Name);
                if (ni.OperationalStatus != OperationalStatus.Up) continue;

                IPNetwork += "MAC: " + ni.GetPhysicalAddress() + Environment.NewLine;
                IPNetwork += "Gateways:" + Environment.NewLine;

                Console.WriteLine("Operational? {0}", ni.OperationalStatus == OperationalStatus.Up);
                Console.WriteLine("MAC: {0}", ni.GetPhysicalAddress());
                Console.WriteLine("Gateways:");
                foreach (GatewayIPAddressInformation gipi in ni.GetIPProperties().GatewayAddresses)
                {
                    IPNetwork += gipi.Address + Environment.NewLine;
                    Console.WriteLine("\t{0}", gipi.Address);
                }
                IPNetwork += "IP Addresses: ";
                Console.WriteLine("IP Addresses:");
                String IPv4 = "";
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
                string baseIP = IPv4.Substring(0, IPv4.LastIndexOf(".") + 1);

                Console.WriteLine("Pinging 255 destinations of D-class in {0}*", baseIP);
                PingNetwork.totallines = "";

                //number of IP in subnet
                PingNetwork.CreatePingers(255);

                PingOptions po = new PingOptions(PingNetwork.ttl, true);
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] data = enc.GetBytes("abababababababababababababababab");

                //SpinWait wait = new SpinWait();
                //Number of start IP address
                int cnt = 1;
                int index = 1;
                // hthngoc - remove async running due to missing Active IP - to check RQA server.
                // must run slowly in sequence.
                foreach (Ping p in PingNetwork.pingers)
                {
                    PingReply reply = p.Send(string.Concat(baseIP, cnt.ToString()), PingNetwork.timeOut);
                    if (reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine(index.ToString() + string.Concat(". Active IP: ", reply.Address.ToString()));
                        activeIP.Add(reply.Address.ToString());
                        IPNetwork += Environment.NewLine + index.ToString() + ". " + reply.Address.ToString();
                        index += 1;
                    }
                    else {
                        IPNetwork += Environment.NewLine + "Skipped: " + string.Concat(baseIP, cnt.ToString());
                    }
                    cnt += 1;
                }
                PingNetwork.DestroyPingers();
                Console.WriteLine("Found {0} active IP-addresses.", activeIP.Count);
                IPNetwork += Environment.NewLine + "Found: " + activeIP.Count + Environment.NewLine;
            }
            //write to log file            
            File.AppendAllText(fn, IPNetwork);
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.WriteLine("Done, please Press Any Key to quit. \n\rPlease get the file \"ResultOfPing.txt\" for Wallem support");
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.WriteLine("=========");
            Console.ReadKey();
        }
    }
}
