using System;
using System.Data;
using Npgsql;

namespace NotesApp
{
    public static class NoteService
    {
        public static void AddNote(int userId, string text)
        {
            string q = "INSERT INTO notes (user_id, content, created_at) VALUES (@uid, @txt, @now)";
            DbHelper.ExecuteNonQuery(q, new[] { new NpgsqlParameter("uid", userId), new NpgsqlParameter("txt", text), new NpgsqlParameter("now", DateTime.Now) });
            SecurityLogger.Log("ADD_NOTE", GetUsername(userId), "127.0.0.1", $"Заметка: {text}", "INFO");
        }

        public static void ShowNotes(int userId)
        {
            var dt = DbHelper.ExecuteQuery("SELECT id, content, created_at FROM notes WHERE user_id=@uid AND is_deleted=false ORDER BY created_at DESC",
                new[] { new NpgsqlParameter("uid", userId) });
            Console.WriteLine("Ваши заметки:");
            foreach (DataRow r in dt.Rows)
                Console.WriteLine($"[{r["created_at"]}] {r["content"]} (id={r["id"]})");
        }

        public static string GetNoteContent(int noteId, int userId)
        {
            var dt = DbHelper.ExecuteQuery("SELECT content FROM notes WHERE id=@id AND user_id=@uid AND is_deleted=false",
                new[] { new NpgsqlParameter("id", noteId), new NpgsqlParameter("uid", userId) });
            return dt.Rows.Count > 0 ? dt.Rows[0]["content"].ToString() : null;
        }

        public static bool UpdateNote(int noteId, int userId, string newText)
        {
            int rows = DbHelper.ExecuteNonQuery("UPDATE notes SET content=@txt, updated_at=@now WHERE id=@id AND user_id=@uid AND is_deleted=false",
                new[] { new NpgsqlParameter("txt", newText), new NpgsqlParameter("now", DateTime.Now), new NpgsqlParameter("id", noteId), new NpgsqlParameter("uid", userId) });
            if (rows > 0) SecurityLogger.Log("EDIT_NOTE", GetUsername(userId), "127.0.0.1", $"Заметка {noteId}", "INFO");
            return rows > 0;
        }

        public static bool DeleteNote(int noteId, int userId)
        {
            int rows = DbHelper.ExecuteNonQuery("UPDATE notes SET is_deleted=true, deleted_at=@now WHERE id=@id AND user_id=@uid AND is_deleted=false",
                new[] { new NpgsqlParameter("now", DateTime.Now), new NpgsqlParameter("id", noteId), new NpgsqlParameter("uid", userId) });
            if (rows > 0) SecurityLogger.Log("DELETE_NOTE", GetUsername(userId), "127.0.0.1", $"Заметка {noteId}", "WARNING");
            return rows > 0;
        }

        public static bool RestoreNote(int noteId, int userId)
        {
            int rows = DbHelper.ExecuteNonQuery("UPDATE notes SET is_deleted=false, deleted_at=NULL, updated_at=@now WHERE id=@id AND user_id=@uid AND is_deleted=true",
                new[] { new NpgsqlParameter("now", DateTime.Now), new NpgsqlParameter("id", noteId), new NpgsqlParameter("uid", userId) });
            if (rows > 0) SecurityLogger.Log("RESTORE_NOTE", GetUsername(userId), "127.0.0.1", $"Заметка {noteId}", "INFO");
            return rows > 0;
        }

        private static string GetUsername(int userId)
        {
            var dt = DbHelper.ExecuteQuery("SELECT username FROM users WHERE id=@id", new[] { new NpgsqlParameter("id", userId) });
            return dt.Rows.Count > 0 ? dt.Rows[0]["username"].ToString() : "unknown";
        }
    }
}