

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
        private const string CurrentVersion = "0.0.0";
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
                    ColorConsole.WriteLineError("В релизе нет прикреплённых ZIP-файлов.");
                    if (AskRunOldOrExit())
                        return;
                    else
                        Environment.Exit(0);
                }

                string downloadUrl = assets[0]["browser_download_url"].ToString();
                string latestTag = release["tag_name"]?.ToString() ?? "unknown";
                string tagWithoutV = latestTag.TrimStart('v');

                string tempZip = Path.Combine(Path.GetTempPath(), $"update_{latestTag}.zip");
                ColorConsole.WriteLineInfo($"Загрузка обновления {latestTag}...");
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NotesLauncher");
                    client.DownloadFile(downloadUrl, tempZip);
                }
                ColorConsole.WriteLineSuccess($"Архив скачан: {tempZip}");

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string targetFolder = Path.Combine(desktop, $"NotesSystem_{tagWithoutV}");
                if (Directory.Exists(targetFolder))
                {
                    ColorConsole.WriteLineWarning($"Папка {targetFolder} уже существует. Она будет перезаписана.");
                    Directory.Delete(targetFolder, true);
                }
                Directory.CreateDirectory(targetFolder);

                ColorConsole.WriteLineInfo("Распаковка...");
                ZipFile.ExtractToDirectory(tempZip, targetFolder);

                string launcherPath = Path.Combine(targetFolder, "Launch.exe");
                if (!File.Exists(launcherPath))
                {
                    var subdirs = Directory.GetDirectories(targetFolder);
                    if (subdirs.Length > 0)
                    {
                        string innerFolder = subdirs[0];
                        launcherPath = Path.Combine(innerFolder, "Launch.exe");
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
                    ColorConsole.WriteLineSuccess($"Новая версия установлена в папку: {targetFolder}");
                    Console.Write("Запустить обновлённую версию сейчас? (y/n): ");
                    string answer = Console.ReadLine()?.ToLower();
                    if (answer == "y" || answer == "н")
                    {
                        ColorConsole.WriteLineInfo("Запуск новой версии...");
                        Process.Start(launcherPath);
                        Environment.Exit(0);
                    }
                    else
                    {
                        ColorConsole.WriteLineInfo("Обновление установлено, но не запущено.");
                        if (AskRunOldOrExit())
                            return;
                        else
                            Environment.Exit(0);
                    }
                }
                else
                {
                    ColorConsole.WriteLineError("Не удалось найти Launch.exe в архиве.");
                    if (AskRunOldOrExit())
                        return;
                    else
                        Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteLineError($"Ошибка при установке обновления: {ex.Message}");
                if (AskRunOldOrExit())
                    return;
                else
                    Environment.Exit(0);
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