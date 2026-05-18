using System;
using System.Diagnostics;
using System.IO;

namespace Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Notes System Launcher";
            ColorConsole.WriteLineInfo("=== Инициализация системы ===");

            var updater = new UpdateService();
            updater.CheckForUpdates();

            if (File.Exists("StatsAgent.exe"))
            {
                ColorConsole.WriteLineInfo("Запуск агента мониторинга (StatsAgent)...");
                Process.Start("StatsAgent.exe");
            }
            else
                ColorConsole.WriteLineWarning("StatsAgent.exe не найден. Пропуск.");
            if (File.Exists("NotesApp.exe"))
            {
                ColorConsole.WriteLineInfo("Запуск приложения заметок (NotesApp)...");
                Process.Start("NotesApp.exe");
            }
            else
                ColorConsole.WriteLineError("Критическая ошибка: NotesApp.exe не найден!");

            ColorConsole.WriteLineSuccess("Лаунчер завершил работу.");
            for (int i = 3; i > 0; i--)
            {
                Console.WriteLine($"\rЛаунчер закроется через {i} секунд...");
                System.Threading.Thread.Sleep(1000);
            }
            Environment.Exit(0);
        }
    }
}