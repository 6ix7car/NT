using System.Data;
using Npgsql;

namespace NotesApp
{
    public static class DbHelper
    {
        private static string _conn = "Server=localhost;Port=5432;User ID=postgres;Password=3455;Database=NoteSystem;";
        private static string _appUserConn = "Server=localhost;Port=5432;User ID=app_user;Password=secure_password;Database=NoteSystem;";

        public static DataTable ExecuteQuery(string query, NpgsqlParameter[] parameters = null)
        {
            using (var conn = new NpgsqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    using (var da = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static int ExecuteNonQuery(string query, NpgsqlParameter[] parameters = null)
        {
            using (var conn = new NpgsqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        public static (bool success, int userId, string role) CallLoginProcedure(string username, string password)
        {
            using (var conn = new NpgsqlConnection(_appUserConn))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT * FROM app.login_user(@u, @p)", conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int uid = reader.GetInt32(0);
                            string role = reader.GetString(1);
                            return (true, uid, role);
                        }
                        return (false, -1, null);
                    }
                }
            }
        }
    }
}