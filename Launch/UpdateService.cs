using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;

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
                        ColorConsole.WriteLineWarning($" Доступна новая версия: {releaseName}");
                        ColorConsole.WriteLineInfo($"Что нового: {releaseNotes}");
                        Console.Write("Установить обновление? (y/n): ");
                        string answer = Console.ReadLine()?.ToLower();
                        if (answer == "y" || answer == "н")
                        {
                            DownloadAndApply(release);
                        }
                        else
                        {
                            ColorConsole.WriteLineInfo("Обновление отложено.");
                        }
                    }
                    else
                    {
                        ColorConsole.WriteSuccess("Установлена последняя версия.");
                    }
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
                    return;
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
                    ColorConsole.WriteLineWarning($"Папка {targetFolder} уже существует. Будет перезаписана.");
                    Directory.Delete(targetFolder, true);
                }
                Directory.CreateDirectory(targetFolder);

                ColorConsole.WriteLineInfo("Распаковка...");
                ZipFile.ExtractToDirectory(tempZip, targetFolder);

                string launcherExe = Path.Combine(targetFolder, "Launch.exe");
                if (!File.Exists(launcherExe))
                {
                    var subdirs = Directory.GetDirectories(targetFolder);
                    if (subdirs.Length > 0)
                    {
                        launcherExe = Path.Combine(subdirs[0], "Launch.exe");
                        if (File.Exists(launcherExe))
                        {
                            foreach (string file in Directory.GetFiles(subdirs[0]))
                                File.Move(file, Path.Combine(targetFolder, Path.GetFileName(file)));
                            foreach (string dir in Directory.GetDirectories(subdirs[0]))
                                Directory.Move(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
                            Directory.Delete(subdirs[0]);
                            launcherExe = Path.Combine(targetFolder, "Launch.exe");
                        }
                    }
                }

                if (File.Exists(launcherExe))
                {
                    ColorConsole.WriteSuccess($"Обновление установлено в {targetFolder}");
                    Console.Write("Запустить обновлённую версию сейчас? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        Process.Start(launcherExe);
                        Environment.Exit(0);
                    }
                    else
                    {
                        ColorConsole.WriteLineInfo("Обновление установлено. Вы можете запустить его позже.");
                    }
                }
                else
                {
                    ColorConsole.WriteLineError("Не найден Launch.exe в архиве. Проверьте содержимое ZIP.");
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteLineError($"Ошибка обновления: {ex.Message}");
            }
        }
    }
}