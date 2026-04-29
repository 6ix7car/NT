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
            ColorConsole.WriteInfo("=== Инициализация системы ===");

            var updater = new UpdateService();
            updater.CheckForUpdates();

            // Запуск агента
            if (File.Exists("StatsAgent.exe"))
            {
                ColorConsole.WriteInfo("Запуск агента мониторинга (StatsAgent)...");
                Process.Start("StatsAgent.exe");
            }
            else
                ColorConsole.WriteWarning("StatsAgent.exe не найден. Пропуск.");

            // Запуск основного приложения
            if (File.Exists("NotesApp.exe"))
            {
                ColorConsole.WriteInfo("Запуск приложения заметок (NotesApp)...");
                Process.Start("NotesApp.exe");
            }
            else
                ColorConsole.WriteError("Критическая ошибка: NotesApp.exe не найден!");

            ColorConsole.WriteSuccess("Лаунчер завершил работу.");
            for (int i = 3; i > 0; i--)
            {
                Console.Write($"\rЛаунчер закроется через {i} секунд...");
                System.Threading.Thread.Sleep(1000);
            }
            Environment.Exit(0);
        }
    }
}