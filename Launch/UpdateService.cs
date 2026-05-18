

using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Launcher
{
    public class UpdateService
    {
        private const string CurrentVersion = "1.0.0";
        private const string RepoOwner = "6ix7car";
        private const string RepoName = "NT";

        public void CheckForUpdates()
        {
            string apiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "NotesLauncher");
                try
                {
                    string json = client.DownloadString(apiUrl);
                    var release = JObject.Parse(json);
                    string latestTag = release["tag_name"]?.ToString().TrimStart('v') ?? "0.0.0";
                    string releaseName = release["name"]?.ToString() ?? "Новый релиз";
                    string releaseNotes = release["body"]?.ToString() ?? "нет описания";

                    ColorConsole.WriteLineInfo($"Ваша версия: {CurrentVersion}");
                    ColorConsole.WriteLineInfo($"Последняя версия: {latestTag}");

                    if (latestTag != CurrentVersion)
                    {
                        ColorConsole.WriteLineWarning($"Доступна новая версия: {releaseName}");
                        ColorConsole.WriteLineInfo($"Что нового: {releaseNotes}");

                        while (true)
                        {
                            Console.Write("Установить обновление? (");
                            ColorConsole.WriteSuccess("'y (yes)'");
                            Console.Write("/");
                            ColorConsole.WriteError("'n (no)'): ");
                            string answer = Console.ReadLine()?.ToLower().Trim();

                            if (answer == "y" || answer == "yes" || answer == "н")
                            {
                                DownloadAndApply(release);
                                break;
                            }
                            else if (answer == "n" || answer == "no" || answer == "т")
                            {
                                ColorConsole.WriteLineInfo("Обновление отложено.");
                                break;
                            }
                            else
                            {
                                ColorConsole.WriteLineWarning("Пожалуйста, введите 'y' (да) или 'n' (нет).");
                            }
                        }
                    }
                    else
                        ColorConsole.WriteLineSuccess("Установлена последняя версия.");
                }
                catch (WebException ex)
                {
                    ColorConsole.WriteLineError($"Ошибка подключения к GitHub: {ex.Message}");
                }
            }
        }

        private void DownloadAndApply(JObject release)
        {
            try
            {
                var assets = release["assets"] as JArray;
                if (assets == null || assets.Count == 0)
                {
                    ColorConsole.WriteError("В релизе нет прикреплённых ZIP-файлов.");
                    AskRunOldOrExit();
                    return;
                }

                string downloadUrl = assets[0]["browser_download_url"].ToString();
                string latestTag = release["tag_name"]?.ToString() ?? "unknown";
                string tagWithoutV = latestTag.TrimStart('v');

                string tempZip = Path.Combine(Path.GetTempPath(), $"update_{latestTag}.zip");
                ColorConsole.WriteInfo($"Загрузка обновления {latestTag}...");
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NotesLauncher");
                    client.DownloadFile(downloadUrl, tempZip);
                }
                ColorConsole.WriteSuccess($"Архив скачан: {tempZip}");

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string targetFolder = Path.Combine(desktop, $"NotesSystem_{tagWithoutV}");

                // Завершаем процесс старой версии, если он запущен
                foreach (var p in Process.GetProcessesByName("Launch"))
                {
                    try
                    {
                        ColorConsole.WriteInfo($"Завершение процесса {p.ProcessName} (PID: {p.Id})...");
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                    catch { }
                }

                if (Directory.Exists(targetFolder))
                {
                    ColorConsole.WriteWarning($"Папка {targetFolder} уже существует. Удаление...");
                    Directory.Delete(targetFolder, true);
                }
                Directory.CreateDirectory(targetFolder);

                ColorConsole.WriteInfo("Распаковка...");
                ZipFile.ExtractToDirectory(tempZip, targetFolder);

                // Поиск Launcher.exe в распакованных файлах
                string launcherPath = Path.Combine(targetFolder, "Launcher.exe");
                if (!File.Exists(launcherPath))
                {
                    var subdirs = Directory.GetDirectories(targetFolder);
                    if (subdirs.Length > 0)
                    {
                        string innerFolder = subdirs[0];
                        launcherPath = Path.Combine(innerFolder, "Launcher.exe");
                        if (File.Exists(launcherPath))
                        {
                            foreach (string file in Directory.GetFiles(innerFolder))
                                File.Move(file, Path.Combine(targetFolder, Path.GetFileName(file)));
                            foreach (string dir in Directory.GetDirectories(innerFolder))
                                Directory.Move(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
                            Directory.Delete(innerFolder);
                        }
                    }
                }

                if (File.Exists(launcherPath))
                {
                    ColorConsole.WriteSuccess($"Новая версия установлена в папку: {targetFolder}");
                    Console.Write("Запустить обновлённую версию сейчас? (y/n): ");
                    string answer = Console.ReadLine()?.ToLower();
                    if (answer == "y" || answer == "н")
                    {
                        ColorConsole.WriteInfo("Запуск новой версии...");
                        Process.Start(launcherPath);
                        Environment.Exit(0);
                    }
                    else
                    {
                        ColorConsole.WriteInfo("Обновление установлено, но не запущено.");
                        AskRunOldOrExit();
                    }
                }
                else
                {
                    ColorConsole.WriteError("Не удалось найти Launcher.exe в архиве.");
                    AskRunOldOrExit();
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteError($"Ошибка при установке обновления: {ex.Message}");
                AskRunOldOrExit();
            }
        }

        private bool AskRunOldOrExit()
        {
            ColorConsole.WriteLineWarning($"Запустить текущую версию (v{CurrentVersion})? (y/n): ");
            string answer = Console.ReadLine()?.ToLower().Trim();
            if (answer == "y" || answer == "yes" || answer == "да")
            {
                return true;
            }
            else
            {
                ColorConsole.WriteLineInfo("Выход.");
                return false;
            }
        }
    }
}