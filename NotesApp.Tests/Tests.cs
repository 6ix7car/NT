using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;

namespace NotesApp.Tests
{
    [TestClass]
    public class Tests
    {
        private string testUser;
        private readonly string testPass = "testpass123";

        [TestInitialize]
        public void Setup()
        {
            testUser = $"testuser_{System.DateTime.Now.Ticks}";
        }

        #region Тесты хеширования
        [TestMethod]
        public void GetMd5Hash_Admin_ReturnsCorrect()
        {
            string actual = AuthService.GetMd5Hash("admin");
            Assert.AreEqual("21232f297a57a5a743894a0e4a801fc3", actual);
        }

        [TestMethod]
        public void GetMd5Hash_Password_ReturnsCorrect()
        {
            string actual = AuthService.GetMd5Hash("password");
            Assert.AreEqual("5f4dcc3b5aa765d61d8327deb882cf99", actual);
        }

        [TestMethod]
        public void GetMd5Hash_EmptyString_ReturnsCorrect()
        {
            string actual = AuthService.GetMd5Hash("");
            Assert.AreEqual("d41d8cd98f00b204e9800998ecf8427e", actual);
        }

        [TestMethod]
        public void GetMd5Hash_IsDeterministic()
        {
            string input = "test_deterministic";
            string first = AuthService.GetMd5Hash(input);
            string second = AuthService.GetMd5Hash(input);
            Assert.AreEqual(first, second);
        }
        #endregion

        #region  тесты
        [TestMethod]
        public void Register_NewUser_ReturnsTrue()
        {
            bool result = AuthService.Register(testUser, testPass, "user");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Register_ExistingUser_ReturnsFalse()
        {
            // Предполагаем, что пользователь 'existinguser' с паролем 'pass' уже существует в БД.
            bool result = AuthService.Register("existinguser", "pass", "user");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Login_ValidCredentials_ReturnsTrue()
        {
            AuthService.Register(testUser, testPass, "user");
            bool result = AuthService.Login(testUser, testPass);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Login_WrongPassword_ReturnsFalse()
        {
            AuthService.Register(testUser, testPass, "user");
            bool result = AuthService.Login(testUser, "wrongpassword");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Login_NonexistentUser_ReturnsFalse()
        {
            bool result = AuthService.Login("nonexistent_" + System.DateTime.Now.Ticks, "any");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddNote_ShouldCreateNote()
        {
            AuthService.Register(testUser, testPass, "user");
            AuthService.Login(testUser, testPass);
            int userId = GetUserId(testUser);
            NoteService.AddNote(userId, "Тестовая заметка");
            var parameters = new NpgsqlParameter[] { new NpgsqlParameter("uid", userId) };
            var dt = DbHelper.ExecuteQuery("SELECT COUNT(*) FROM notes WHERE user_id = @uid AND is_deleted = false", parameters);
            long count = (long)dt.Rows[0][0];
            Assert.IsTrue(count > 0);
        }

        [TestMethod]
        public void DeleteNote_SoftDelete_MarksDeleted()
        {
            AuthService.Register(testUser, testPass, "user");
            AuthService.Login(testUser, testPass);
            int userId = GetUserId(testUser);
            NoteService.AddNote(userId, "Удаляемая заметка");
            var selectParams = new NpgsqlParameter[] { new NpgsqlParameter("uid", userId) };
            var dt = DbHelper.ExecuteQuery("SELECT id FROM notes WHERE user_id = @uid AND is_deleted = false ORDER BY id DESC LIMIT 1", selectParams);
            int noteId = (int)dt.Rows[0][0];
            NoteService.DeleteNote(noteId, userId);
            var checkParams = new NpgsqlParameter[] { new NpgsqlParameter("id", noteId) };
            dt = DbHelper.ExecuteQuery("SELECT is_deleted FROM notes WHERE id = @id", checkParams);
            Assert.IsTrue((bool)dt.Rows[0][0]);
        }
        #endregion

        // ========== Хранимая процедура ==========
        [TestMethod]
        public void StoredProcedure_LoginUser_ReturnsUserIdAndRole_CreatesNote()
        {
            // Сначала регистрируем пользователя и получаем его ID
            AuthService.Register(testUser, testPass, "user");
            AuthService.Login(testUser, testPass);
            int userId = GetUserId(testUser);

            var procParams = new NpgsqlParameter[]
            {
                new NpgsqlParameter("p_username", testUser),
                new NpgsqlParameter("p_password", testPass)
            };
            var dt = DbHelper.ExecuteQuery("SELECT * FROM app.login_user(@p_username, @p_password)", procParams);

            Assert.AreEqual(1, dt.Rows.Count);
            int returnedUserId = Convert.ToInt32(dt.Rows[0]["out_user_id"]);
            string returnedRole = dt.Rows[0]["out_role"].ToString();

            Assert.AreEqual(userId, returnedUserId);
            Assert.AreEqual("user", returnedRole);

            var checkNote = DbHelper.ExecuteQuery(
                "SELECT COUNT(*) FROM notes WHERE user_id = @uid AND content LIKE '%хранимой процедурой%'",
                new NpgsqlParameter[] { new NpgsqlParameter("uid", userId) });
            long count = (long)checkNote.Rows[0][0];
            Assert.IsTrue(count > 0, "Заметка о входе через хранимую процедуру не создана");
        }

        // ========== Статистика ==========
        [TestMethod]
        public void Stats_SaveLocalStatsToDb_InsertsRecord()
        {
            long before = GetStatsCount();
            Stats.SaveLocalStatsToDb();
            long after = GetStatsCount();
            Assert.IsTrue(after > before, "Статистика не сохранилась в БД");
        }

        // ========== Вспомогательные методы ==========
        private int GetUserId(string username)
        {
            var p = new NpgsqlParameter[] { new NpgsqlParameter("u", username) };
            var dt = DbHelper.ExecuteQuery("SELECT id FROM users WHERE username = @u", p);
            return (int)dt.Rows[0][0];
        }

        private long GetStatsCount()
        {
            var dt = DbHelper.ExecuteQuery("SELECT COUNT(*) FROM system_stats");
            return (long)dt.Rows[0][0];
        }
    }
}