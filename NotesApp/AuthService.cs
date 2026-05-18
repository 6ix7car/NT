

using Launcher;
using Npgsql;
using System;
using System.Net;
using System.Net.Sockets;

namespace NotesApp
{
    public static class AuthService
    {
        private static string adminConn = "Server=localhost;Port=5432;User ID=postgres;Password=3455;Database=NoteSystem;";

        public static string GetMd5Hash(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public static bool Login(string username, string password)
        {
            using (var conn = new NpgsqlConnection(adminConn))
            {
                conn.Open();
                string query = "SELECT id, role, password_md5 FROM users WHERE username = @u";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string role = reader.GetString(1);
                            string storedHash = reader.GetString(2);
                            if (storedHash == GetMd5Hash(password))
                            {
                                reader.Close();
                                string upd = "UPDATE users SET last_login_at = @now WHERE id = @id";
                                using (var updCmd = new NpgsqlCommand(upd, conn))
                                {
                                    updCmd.Parameters.AddWithValue("@now", DateTime.Now);
                                    updCmd.Parameters.AddWithValue("@id", id);
                                    updCmd.ExecuteNonQuery();
                                }
                                Program.SetCurrentUser(id, username, role);
                                ColorConsole.WriteLineSuccess($"Добро пожаловать, {username}! Ваша роль: {role}");
                                SecurityLogger.Log("LOGIN_SUCCESS", username, GetLocalIP(), "Успешный вход", "INFO");
                                return true;
                            }
                            else
                            {
                                ColorConsole.WriteLineError("Неверный пароль.");
                                SecurityLogger.Log("LOGIN_FAIL", username, GetLocalIP(), "Неверный пароль", "WARNING");
                                return false;
                            }
                        }
                        else
                        {
                            ColorConsole.WriteLineError("Пользователь не найден.");
                            SecurityLogger.Log("LOGIN_FAIL", username, GetLocalIP(), "Не найден", "WARNING");
                            return false;
                        }
                    }
                }
            }
        }

        public static bool Register(string username, string password, string role)
        {
            if (role != "admin" && role != "user" && role != "readonly") role = "user";
            using (var conn = new NpgsqlConnection(adminConn))
            {
                conn.Open();
                string check = "SELECT COUNT(*) FROM users WHERE username = @u";
                using (var chk = new NpgsqlCommand(check, conn))
                {
                    chk.Parameters.AddWithValue("@u", username);
                    if ((long)chk.ExecuteScalar() > 0)
                    {
                        ColorConsole.WriteLineError($"Пользователь {username} уже существует.");
                        return false;
                    }
                }
                string ins = "INSERT INTO users (username, password_md5, role, created_at) VALUES (@u, @p, @r, @now)";
                using (var cmd = new NpgsqlCommand(ins, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", GetMd5Hash(password));
                    cmd.Parameters.AddWithValue("@r", role);
                    cmd.Parameters.AddWithValue("@now", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
                ColorConsole.WriteLineSuccess($"Регистрация {username} (роль {role}) успешна.");
                SecurityLogger.Log("REGISTER", username, GetLocalIP(), $"Роль {role}", "INFO");
                return true;
            }
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