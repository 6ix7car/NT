using NotesApp;
using System;
using System.Threading;

namespace StatsAgent
{
    class Program
    {
        static void Main()
        {
            Console.Title = "Stats Agent";
            Console.WriteLine("Агент мониторинга запущен. Сбор статистики каждые 60 секунд.");

            while (true)
            {
                try
                {
                    Stats.SaveLocalStatsToDb();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Статистика сохранена в БД.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Ошибка: {ex.Message}");
                }
                Thread.Sleep(60000);
            }
        }
    }
}