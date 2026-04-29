using Npgsql;

namespace NotesApp
{
    public static class SecurityLogger
    {
        public static void Log(string eventType, string username, string ip, string description, string severity)
        {
            string q = "INSERT INTO security_logs (event_type, username, user_ip, description, severity, created_at) VALUES (@et, @u, @ip, @desc, @sev, @now)";
            var p = new[]
            {
                new NpgsqlParameter("et", eventType),
                new NpgsqlParameter("u", username),
                new NpgsqlParameter("ip", ip),
                new NpgsqlParameter("desc", description),
                new NpgsqlParameter("sev", severity),
                new NpgsqlParameter("now", System.DateTime.Now)
            };
            DbHelper.ExecuteNonQuery(q, p);
        }
    }
}