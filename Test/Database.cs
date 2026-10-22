using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Test
{
    public class Database
    {
        private readonly string connectionString;

        // A fájl a program mellé (bin\Debug) kerül, így újraindítás után is megmarad
        public Database(string path = "projektek.db")
        {
            connectionString = "Data Source=" + path + ";Version=3;";
            CreateTables();
        }

        private SQLiteConnection Open()
        {
            var conn = new SQLiteConnection(connectionString);
            conn.Open();
            return conn;
        }

        private void CreateTables()
        {
            using (var conn = Open())
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Employees (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Name          TEXT NOT NULL,
    Role          TEXT NOT NULL CHECK (Role IN ('Programmer','ProjectManager','Director','HrDirector')),
    Seniority     TEXT NULL     CHECK (Seniority IN ('Junior','Mid','Senior')),
    MonthlySalary REAL NOT NULL CHECK (MonthlySalary > 0),
    CHECK ((Role = 'Programmer') = (Seniority IS NOT NULL))
);";
                cmd.ExecuteNonQuery();
            }
        }

        // Kulcs pl. "Programmer:Junior", "ProjectManager:"
        public static string Key(string role, string seniority)
        {
            return role + ":" + (seniority ?? "");
        }

        public Dictionary<string, int> CountByRole()
        {
            var result = new Dictionary<string, int>();
            using (var conn = Open())
            using (var cmd = new SQLiteCommand(
                "SELECT Role, Seniority, COUNT(*) FROM Employees GROUP BY Role, Seniority", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string seniority = r.IsDBNull(1) ? null : r.GetString(1);
                    result[Key(r.GetString(0), seniority)] = Convert.ToInt32(r.GetValue(2));
                }
            }
            return result;
        }

        public void InsertEmployees(List<Employee> employees)
        {
            if (employees.Count == 0) return;

            using (var conn = Open())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var e in employees)
                {
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO Employees (Name, Role, Seniority, MonthlySalary)
                                            VALUES (@name, @role, @seniority, @salary);
                                            SELECT last_insert_rowid();";
                        var programmer = e as Programmer;
                        cmd.Parameters.AddWithValue("@name", e.Name);
                        cmd.Parameters.AddWithValue("@role", e.GetType().Name);
                        cmd.Parameters.AddWithValue("@seniority",
                            programmer != null ? (object)programmer.Seniority.ToString() : DBNull.Value);
                        cmd.Parameters.AddWithValue("@salary", (double)e.MonthlySalary);

                        e.SetId(Convert.ToInt32(cmd.ExecuteScalar()));
                    }
                }
                tx.Commit();
            }
        }

        public List<Employee> GetAllEmployees()
        {
            var list = new List<Employee>();
            using (var conn = Open())
            using (var cmd = new SQLiteCommand(
                "SELECT Id, Name, Role, Seniority, MonthlySalary FROM Employees ORDER BY Id", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(Map(r));
            }
            return list;
        }

        private static Employee Map(SQLiteDataReader r)
        {
            int id = Convert.ToInt32(r.GetValue(0));
            string name = r.GetString(1);
            string role = r.GetString(2);
            decimal salary = Convert.ToDecimal(r.GetValue(4));

            switch (role)
            {
                case "Programmer":
                    var seniority = (Seniority)Enum.Parse(typeof(Seniority), r.GetString(3));
                    return new Programmer(id, name, salary, seniority);
                case "ProjectManager":
                    return new ProjectManager(id, name, salary);
                case "Director":
                    return new Director(id, name, salary);
                case "HrDirector":
                    return new HrDirector(id, name, salary);
                default:
                    throw new InvalidOperationException("Ismeretlen munkakör: " + role);
            }
        }
    }
}