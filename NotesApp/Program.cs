using System;
using System.Collections.Generic;
using System.Linq;
using Launcher;
using NotesApp;

namespace NotesApp
{
    class Program
    {
        private static int currentUserId = -1;
        private static string currentUsername = "";
        private static string currentUserRole = "";

        public static void SetCurrentUser(int id, string name, string role)
        {
            currentUserId = id;
            currentUsername = name;
            currentUserRole = role;
        }

        static void Main(string[] args)
        {
            Console.Title = "Notes System";
            ColorConsole.WriteLineInfo(@"
╔══════════════════════════════════════════════════════════════╗
║                 СИСТЕМА ЗАМЕТОК v1.0                         ║
║                 Введите --help для списка команд             ║
╚══════════════════════════════════════════════════════════════╝
");

            // Регистрация обработчика закрытия – убить агент
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                try
                {
                    var agents = System.Diagnostics.Process.GetProcessesByName("StatsAgent");
                    foreach (var a in agents) a.Kill();
                }
                catch { }
            };

            while (true)
            {
                if (currentUserId == -1)
                    Console.Write("\n> ");
                else
                    Console.Write($"\n{currentUsername}@{currentUserRole}> ");

                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input == "exit") break;
                ProcessCommand(input);
            }
        }

        private static readonly Dictionary<string, string> ShortToLong = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "-a", "--addnewnote" }, { "-add", "--addnewnote" },
            { "-l", "--listnotes" }, { "-list", "--listnotes" },
            { "-g", "--getnote" },
            { "-e", "--editnote" }, { "-edit", "--editnote" },
            { "-d", "--deletenote" }, { "-del", "--deletenote" }, { "-rm", "--deletenote" },
            { "-r", "--restorenote" }, { "-restore", "--restorenote" },
            { "-stats", "--systemstats" }, { "-logs", "--securitylogs" },
            { "-lgn", "--login" }, { "-reg", "--register" },
            { "-role", "--myrole" }, { "-out", "--logout" },
            { "-h", "--help" }, { "?", "--help" }, { "/?", "--help" },
            { "-proclogin", "--proclogin" },
            { "--proc-login", "--proclogin" },
        };

        static void ProcessCommand(string input)
        {
            string[] parts = input.Split(' ');
            string raw = parts[0].ToLower();
            if (ShortToLong.TryGetValue(raw, out string mapped)) raw = mapped;
            string cmd = raw;

            switch (cmd)
            {
                case "--help": ShowHelp(); break;
                case "--login":
                    if (parts.Length >= 3) AuthService.Login(parts[1], parts[2]);
                    else Console.WriteLine("Использование: --login <username> <password>");
                    break;
                case "--register":
                    if (parts.Length >= 3)
                    {
                        string role = parts.Length >= 4 ? parts[3].ToLower() : "user";
                        AuthService.Register(parts[1], parts[2], role);
                    }
                    else Console.WriteLine("Использование: --register <username> <password> [role]");
                    break;
                case "--logout":
                    if (currentUserId == -1) ColorConsole.WriteLineWarning("Вы не вошли.");
                    else
                    {
                        currentUserId = -1; currentUsername = ""; currentUserRole = "";
                        ColorConsole.WriteLineSuccess("Вы вышли из системы.");
                    }
                    break;
                case "--myrole":
                    if (currentUserId == -1) ColorConsole.WriteLineWarning("Необходимо войти.");
                    else Console.WriteLine($"Ваша роль: {currentUserRole}");
                    break;
                case "--addnewnote":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (currentUserRole == "readonly") { ColorConsole.WriteLineWarning("Нет прав."); break; }
                    if (parts.Length < 2) { Console.WriteLine("Использование: --addNewNote \"текст\""); break; }
                    string text = string.Join(" ", parts.Skip(1)).Trim('"');
                    NoteService.AddNote(currentUserId, text);
                    ColorConsole.WriteLineSuccess("Заметка добавлена.");
                    break;
                case "--listnotes":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    NoteService.ShowNotes(currentUserId);
                    break;
                case "--getnote":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int gid))
                        Console.WriteLine("Использование: --getNote <id>");
                    else
                    {
                        string cnt = NoteService.GetNoteContent(gid, currentUserId);
                        if (cnt == null) ColorConsole.WriteLineWarning($"Заметка {gid} не найдена.");
                        else Console.WriteLine($"Заметка {gid}: {cnt}");
                    }
                    break;
                case "--editnote":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (currentUserRole == "readonly") { ColorConsole.WriteLineWarning("Нет прав."); break; }
                    if (parts.Length < 3 || !int.TryParse(parts[1], out int eid))
                        Console.WriteLine("Использование: --editNote <id> \"текст\"");
                    else
                    {
                        string newText = string.Join(" ", parts.Skip(2)).Trim('"');
                        if (NoteService.UpdateNote(eid, currentUserId, newText))
                            ColorConsole.WriteLineSuccess("Заметка обновлена.");
                        else ColorConsole.WriteLineError("Ошибка обновления.");
                    }
                    break;
                case "--deletenote":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (currentUserRole == "readonly") { ColorConsole.WriteLineWarning("Нет прав."); break; }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int did))
                        Console.WriteLine("Использование: --deleteNote <id>");
                    else
                    {
                        ColorConsole.WriteWarning($"Удалить заметку {did}? (y/n): ");
                        if (Console.ReadLine()?.ToLower() == "y")
                        {
                            if (NoteService.DeleteNote(did, currentUserId))
                                ColorConsole.WriteLineSuccess("Заметка удалена.");
                            else ColorConsole.WriteLineError("Ошибка удаления.");
                        }
                    }
                    break;
                case "--restorenote":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (currentUserRole == "readonly") { ColorConsole.WriteLineWarning("Нет прав."); break; }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int rid))
                        Console.WriteLine("Использование: --restoreNote <id>");
                    else
                    {
                        if (NoteService.RestoreNote(rid, currentUserId))
                            ColorConsole.WriteLineSuccess("Заметка восстановлена.");
                        else ColorConsole.WriteLineError("Ошибка восстановления.");
                    }
                    break;
                case "--systemstats":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (currentUserRole != "admin") { ColorConsole.WriteLineError("Доступ только админу."); break; }
                    if (parts.Length < 2 || parts[1].ToLower() != "local")
                        Console.WriteLine("Использование: --systemStats local");
                    else
                    {
                        Stats.ShowLocalStats();
                        Stats.SaveLocalStatsToDb();
                        ColorConsole.WriteLineSuccess("Статистика сохранена в БД.");
                    }
                    break;
                case "--securitylogs":
                    if (currentUserId == -1) { ColorConsole.WriteLineWarning("Войдите."); break; }
                    if (currentUserRole != "admin") { ColorConsole.WriteLineError("Доступ только админу."); break; }
                    ShowSecurityLogs();
                    break;
                case "--proclogin":
                    if (parts.Length >= 3)
                    {
                        string username = parts[1];
                        string password = parts[2];
                        var (success, userId, role) = DbHelper.CallLoginProcedure(username, password);
                        if (success)
                        {
                            ColorConsole.WriteLineSuccess($"Хранимая процедура выполнена. Пользователь {username} (id={userId}, role={role}) авторизован. Заметка о входе создана.");
                        }
                        else
                        {
                            ColorConsole.WriteLineError("Хранимая процедура не вернула результат. Неверный логин или пароль.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Использование: --proc-login <username> <password>");
                    }
                    break;
                default:
                    ColorConsole.WriteLineWarning("Неизвестная команда. Введите --help.");
                    break;
            }
        }

        static void ShowHelp()
        {
            if (currentUserId == -1)
            {
                Console.WriteLine(@"
                Доступные команды (не авторизован):
                  --login (-lgn) <username> <password>     - Вход в систему (обычный)
                  --proc-login <user> <pass>                 - Проверить хранимую процедуру (создаст заметку)
                  --register (-reg) <user> <pass> [role]     - Регистрация (admin/user/readonly)
                  --help (-h, ?, /?)                         - Справка
                  exit                                       - Выход");
                return;
            }
            Console.WriteLine("=== ДОСТУПНЫЕ КОМАНДЫ ===");
            Console.WriteLine("  --logout (-out)                    - Выйти");
            Console.WriteLine("  --myrole (-role)                   - Моя роль");
            Console.WriteLine("  --listNotes (-l, -list)            - Список заметок");
            Console.WriteLine("  --getNote (-g) <id>                - Показать заметку");
            if (currentUserRole != "readonly")
            {
                Console.WriteLine("  --addNewNote (-a, -add) \"текст\"    - Добавить заметку");
                Console.WriteLine("  --editNote (-e, -edit) <id> \"текст\" - Редактировать");
                Console.WriteLine("  --deleteNote (-d, -del, -rm) <id>     - Удалить");
                Console.WriteLine("  --restoreNote (-r, -restore) <id>     - Восстановить");
            }
            if (currentUserRole == "admin")
            {
                Console.WriteLine("  --systemStats (-stats) local       - Статистика сервера (CPU/RAM/HDD)");
                Console.WriteLine("  --securityLogs (-logs) list        - Логи безопасности");
            }
            Console.WriteLine("  --help (-h, ?, /?)                 - Справка");
            Console.WriteLine("  exit                               - Выход");
        }

        static void ShowSecurityLogs()
        {
            var dt = DbHelper.ExecuteQuery("SELECT * FROM security_logs ORDER BY created_at DESC LIMIT 20");
            if (dt.Rows.Count == 0) { ColorConsole.WriteLineWarning("Логов нет."); return; }
            Console.WriteLine("Последние события безопасности:");
            foreach (System.Data.DataRow row in dt.Rows)
                Console.WriteLine($"{row["created_at"]} | {row["event_type"]} | {row["username"]} | {row["user_ip"]} | {row["description"]} | {row["severity"]}");
        }
    }
}