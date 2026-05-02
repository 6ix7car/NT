using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace Launcher
{
    public class UpdateService
    {
        private const string CurrentVersion = "1.0.0";
        private const string RepoOwner = "6ix7car";
        private const string RepoName = "NotesSystem";

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
                            Console.Write("Установить обновление? (y/n): ");
                            string answer = Console.ReadLine()?.ToLower().Trim();

                            if (answer == "y" || answer == "н")
                            {
                                DownloadUpdate(release);
                                break; 
                            }
                            else if (answer == "n" || answer == "т" || answer == "нет")
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

        private void DownloadUpdate(JObject release)
        {
            var assets = release["assets"] as JArray;
            if (assets == null || assets.Count == 0)
            {
                ColorConsole.WriteLineError("Ошибка");
                return;
            }
            string downloadUrl = assets[0]["browser_download_url"].ToString();
            string latestTag = release["tag_name"]?.ToString();

            ColorConsole.WriteLineInfo($"Загрузка обновления {latestTag}...");
            string tempZip = Path.Combine(Path.GetTempPath(), $"update_{latestTag}.zip");
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "NotesLauncher");
                client.DownloadFile(downloadUrl, tempZip);
            }
            ColorConsole.WriteLineSuccess($"Архив скачан: {tempZip}");

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string targetFolder = Path.Combine(desktopPath, $"NotesSystem_{latestTag}");

            if (Directory.Exists(targetFolder))
            {
                ColorConsole.WriteLineWarning($"Папка {targetFolder} уже существует. Она будет перезаписана.");
                Directory.Delete(targetFolder, true);
            }
            Directory.CreateDirectory(targetFolder);

            ZipFile.ExtractToDirectory(tempZip, targetFolder);
            ColorConsole.WriteLineSuccess($"Обновление успешно установлено в {targetFolder}");

            string newLauncher = Path.Combine(targetFolder, "Launcher.exe");
            if (File.Exists(newLauncher))
            {
                ColorConsole.WriteInfo("Запуск новой версии лаунчера...");
                System.Diagnostics.Process.Start(newLauncher);
                Environment.Exit(0);
            }
            else
            {
                ColorConsole.WriteLineError("В архиве не найден Launcher.exe. Возможно, архив собран неправильно.");
            }
        }
    }
}