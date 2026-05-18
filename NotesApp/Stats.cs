using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using Npgsql;

namespace NotesApp
{
    public static class Stats
    {
        public static void ShowLocalStats()
        {
            var (cpu, ramUsage, ramTotal, ramAvailable, hddUsage, hddTotal, hddFree) = GetCurrentStats();
            Console.WriteLine($"\n📊 Статистика {Environment.MachineName} ({GetLocalIP()}):");
            Console.WriteLine($"  CPU: {cpu}%");
            Console.WriteLine($"  RAM: {ramUsage}% ({ramAvailable} MB / {ramTotal} MB)");
            Console.WriteLine($"  HDD: {hddUsage}% ({hddFree} GB / {hddTotal} GB)");
        }

        public static void SaveLocalStatsToDb()
        {
            var (cpu, ramUsage, ramTotal, ramAvailable, hddUsage, hddTotal, hddFree) = GetCurrentStats();
            string query = @"
                INSERT INTO system_stats (device_name, device_ip, cpu_usage, ram_usage, ram_total, ram_available, hdd_usage, hdd_total, hdd_free, collected_at)
                VALUES (@name, @ip, @cpu, @ru, @rt, @ra, @hu, @ht, @hf, @now)";
            var p = new[]
            {
                new NpgsqlParameter("name", Environment.MachineName),
                new NpgsqlParameter("ip", GetLocalIP()),
                new NpgsqlParameter("cpu", cpu),
                new NpgsqlParameter("ru", ramUsage),
                new NpgsqlParameter("rt", ramTotal),
                new NpgsqlParameter("ra", ramAvailable),
                new NpgsqlParameter("hu", hddUsage),
                new NpgsqlParameter("ht", hddTotal),
                new NpgsqlParameter("hf", hddFree),
                new NpgsqlParameter("now", DateTime.Now)
            };
            DbHelper.ExecuteNonQuery(query, p);
        }

        public static (double cpuUsage, double ramUsage, long ramTotal, long ramAvailable, double hddUsage, long hddTotal, long hddFree) GetCurrentStats()
        {
            double cpu = GetCpuUsage();
            (long total, long available) = GetRamInfo();
            double ramUsage = total > 0 ? Math.Round((double)(total - available) / total * 100, 2) : 0;
            (long totalGb, long freeGb) = GetDiskInfo();
            double hddUsage = totalGb > 0 ? Math.Round((double)(totalGb - freeGb) / totalGb * 100, 2) : 0;
            return (cpu, ramUsage, total, available, hddUsage, totalGb, freeGb);
        }

        private static double GetCpuUsage()
        {
            try
            {
                using (var pc = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    pc.NextValue();
                    System.Threading.Thread.Sleep(500);
                    return Math.Round(pc.NextValue(), 2);
                }
            }
            catch { return 0; }
        }

        private static (long totalMb, long availableMb) GetRamInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        long total = long.Parse(mo["TotalVisibleMemorySize"].ToString()) / 1024;
                        long free = long.Parse(mo["FreePhysicalMemory"].ToString()) / 1024;
                        return (total, free);
                    }
                }
            }
            catch { }
            return (8192, 4096);
        }

        private static (long totalGb, long freeGb) GetDiskInfo()
        {
            string drive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C";
            try
            {
                var d = new DriveInfo(drive);
                return (d.TotalSize / 1073741824, d.AvailableFreeSpace / 1073741824);
            }
            catch { return (0, 0); }
        }

        private static string GetLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }
}