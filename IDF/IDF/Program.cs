using Microsoft.Data.Sqlite;
using System;

namespace IDF
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var connection = new SqliteConnection("Data Source=hello.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
        SELECT name
        FROM user
        WHERE id = $id
    ";
                command.Parameters.AddWithValue("$id", "lol");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);

                        Console.WriteLine($"Hello, {name}!");
                    }
                }
            }
        }
    }
}
