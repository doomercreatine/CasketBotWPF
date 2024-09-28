using NSwag.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace CasketChatBot.Core
{
    class DataBaseManager
    {
        public static void InsertGuesses(ObservableDictionary<string, long> guesses, long casketValue, string winnerName)
        {
            //DateTime now = DateTime.Now;
            //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            using (var connection = new SQLiteConnection("Data Source=db.db"))
            {   
                connection.Open();             
                string sql = @"SELECT MAX(casketId) FROM guesses;";
                using var command = new SQLiteCommand(sql, connection);
                int lastCasketId = 0;
                using (var reader = command.ExecuteReader())
                {                    
                    while (reader.Read())
                    {
                        lastCasketId = reader.GetInt32(0);
                    }
                }
                string sql2 = @"INSERT INTO guesses (datetime, name, guess, casket, win, casketId) VALUES (@datetime, @name, @guess, @casket, @win, @casketId)";
                using var command2 = new SQLiteCommand(sql2, connection);
                foreach (KeyValuePair<string, long> kv in guesses)
                {
                    string win = "no";
                    if (kv.Key == winnerName)
                    {
                        win = "yes";
                    }                    
                    command2.Parameters.AddWithValue("@datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command2.Parameters.AddWithValue("@name", kv.Key);
                    command2.Parameters.AddWithValue("@guess", Convert.ToInt32(kv.Value));
                    command2.Parameters.AddWithValue("@casket", Convert.ToInt32(casketValue));
                    command2.Parameters.AddWithValue("@win", win);
                    command2.Parameters.AddWithValue("@casketId", lastCasketId+1);                    
                }
                command2.ExecuteNonQuery();


            }
        }

        public static int GetWinCounts(string username)
        {
            using (var connection = new SQLiteConnection("Data Source=db.db"))
            {
                connection.Open();
                string sql = $@"SELECT COUNT(name) FROM guesses WHERE win=='yes' AND name=='{username}';";
                using var command = new SQLiteCommand(sql, connection);                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetInt32(0)+1;
                    }
                }
            }
            return 1;
        }

    }
}
