using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace WebBrowser
{
    internal sealed class SQLiteHandler : IDisposable
    {
        private readonly SQLiteConnection connection;

        public SQLiteHandler()
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebBrowser");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "keywords.db");
            connection = new SQLiteConnection("Data Source=" + path + ";Version=3;");
            connection.Open();
            using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Keywords (id INTEGER PRIMARY KEY AUTOINCREMENT, keyword TEXT NOT NULL COLLATE NOCASE UNIQUE)", connection))
                command.ExecuteNonQuery();

            using (SQLiteCommand count = new SQLiteCommand("SELECT COUNT(*) FROM Keywords", connection))
            {
                if (Convert.ToInt32(count.ExecuteScalar()) == 0)
                {
                    InsertKeyword("facebook");
                    InsertKeyword("instagram");
                }
            }
        }

        public List<string> GetAllKeywords()
        {
            List<string> values = new List<string>();
            using (SQLiteCommand command = new SQLiteCommand("SELECT keyword FROM Keywords ORDER BY keyword COLLATE NOCASE", connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
                while (reader.Read()) values.Add(reader.GetString(0));
            return values;
        }

        public void InsertKeyword(string keyword)
        {
            using (SQLiteCommand command = new SQLiteCommand("INSERT OR IGNORE INTO Keywords (keyword) VALUES (@keyword)", connection))
            {
                command.Parameters.AddWithValue("@keyword", keyword.Trim());
                command.ExecuteNonQuery();
            }
        }

        public void DeleteKeyword(string keyword)
        {
            using (SQLiteCommand command = new SQLiteCommand("DELETE FROM Keywords WHERE keyword = @keyword COLLATE NOCASE", connection))
            {
                command.Parameters.AddWithValue("@keyword", keyword);
                command.ExecuteNonQuery();
            }
        }

        public void Dispose() { connection.Dispose(); }
    }
}
