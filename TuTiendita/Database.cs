using System;
using System.Data.SQLite;
using System.IO;

namespace TuTiendita
{
    public static class Database
    {
        private static string dbPath = "Data Source=productos.db";

        static Database()
        {
            if (!File.Exists("./productos.db"))
            {
                SQLiteConnection.CreateFile("productos.db");
                using (var connection = new SQLiteConnection(dbPath))
                {
                    connection.Open();

                    string createTableQuery = @"CREATE TABLE IF NOT EXISTS [Productos] (
                                                [Codigo] TEXT NOT NULL PRIMARY KEY,
                                                [Nombre] TEXT NOT NULL,
                                                [Precio] REAL NOT NULL,
                                                [Stock] INTEGER NOT NULL
                                                )";
                    using (var cmd = new SQLiteCommand(createTableQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    // Crear tabla de Usuarios
                    string createUserTableQuery = @"CREATE TABLE IF NOT EXISTS [Usuarios] (
                                            [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                            [Nombre] TEXT NOT NULL,
                                            [Contrasena] TEXT NOT NULL,
                                            [NivelAcceso] TEXT NOT NULL
                                        );";

                    using (var cmd = new SQLiteCommand(createUserTableQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(dbPath);
        }
    }
}