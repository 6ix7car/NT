using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

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

                    ColorConsole.WriteInfo($"Ваша версия: {CurrentVersion}");
                    ColorConsole.WriteInfo($"Последняя версия: {latestTag}");

                    if (latestTag != CurrentVersion)
                    {
                        ColorConsole.WriteWarning($"🆕 Доступна новая версия: {releaseName}");
                        ColorConsole.WriteInfo($"Что нового: {releaseNotes}");
                        Console.Write("Установить обновление? (y/n): ");
                        if (Console.ReadLine()?.ToLower() == "y")
                            DownloadUpdate(release);
                        else
                            ColorConsole.WriteInfo("Обновление отложено.");
                    }
                    else
                        ColorConsole.WriteSuccess("Установлена последняя версия.");
                }
                catch (WebException ex)
                {
                    ColorConsole.WriteError($"Ошибка подключения к GitHub: {ex.Message}");
                }
            }
        }

        private void DownloadUpdate(JObject release)
        {
            string downloadUrl = release["zipball_url"]?.ToString();
            string latestTag = release["tag_name"]?.ToString();
            if (string.IsNullOrEmpty(downloadUrl)) return;

            ColorConsole.WriteInfo($"Загрузка обновления {latestTag}...");
            string tempZip = Path.Combine(Path.GetTempPath(), $"update_{latestTag}.zip");
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "NotesLauncher");
                client.DownloadFile(downloadUrl, tempZip);
            }
            ColorConsole.WriteSuccess($"Архив скачан: {tempZip}");
            ColorConsole.WriteInfo("Для установки обновления замените файлы вручную из архива.");
        }
    }
}