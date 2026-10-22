using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Test
{
    public class Database
    {
        private readonly string connectionString;

        // A fájl a program mellé (bin\Debug) kerül, így újraindítás után is megmarad
        public Database(string path = "agency.db")
        {
            connectionString = "Data Source=" + path + ";Version=3;";
            CreateTables();
        }

        private SQLiteConnection Open()
        {
            var conn = new SQLiteConnection(connectionString);
            conn.Open();
            // SQLite-ban a külső kulcsok ellenőrzését kapcsolatonként be kell kapcsolni
            using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                cmd.ExecuteNonQuery();
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
    Name          TEXT    NOT NULL,
    Role          TEXT    NOT NULL CHECK (Role IN ('Programmer','ProjectManager','Director','HrDirector')),
    Seniority     TEXT    NULL     CHECK (Seniority IN ('Junior','Mid','Senior')),
    MonthlySalary REAL    NOT NULL CHECK (MonthlySalary > 0),
    CHECK ((Role = 'Programmer') = (Seniority IS NOT NULL))
);

CREATE TABLE IF NOT EXISTS Projects (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT    NOT NULL,
    EstimatedBudget REAL    NOT NULL CHECK (EstimatedBudget > 0),
    CalculatedCost  REAL    NOT NULL CHECK (CalculatedCost >= 0),
    PmEmployeeId    INTEGER NOT NULL REFERENCES Employees(Id),
    PmHours         REAL    NOT NULL CHECK (PmHours > 0)
);

CREATE TABLE IF NOT EXISTS ProjectAssignments (
    ProjectId      INTEGER NOT NULL REFERENCES Projects(Id) ON DELETE CASCADE,
    EmployeeId     INTEGER NOT NULL REFERENCES Employees(Id),
    AllocatedHours REAL    NOT NULL CHECK (AllocatedHours > 0),
    PRIMARY KEY (ProjectId, EmployeeId)
);";
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------- Munkatársak ----------------

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

            // Naplózás csak a sikeres mentés (Commit) UTÁN
            foreach (var e in employees)
                AppLogger.DbInsert("Employees", e.Id.ToString());
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
                    list.Add(MapEmployee(r));
            }
            return list;
        }

        private static Employee MapEmployee(SQLiteDataReader r)
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

        // ---------------- Projektek ----------------

        /// <summary>Elfogadott projekt mentése a beosztásaival együtt, egy tranzakcióban.</summary>
        public void SaveProject(Project project)
        {
            using (var conn = Open())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO Projects (Name, EstimatedBudget, CalculatedCost, PmEmployeeId, PmHours)
                                        VALUES (@name, @budget, @cost, @pm, @pmHours);
                                        SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@name", project.Name);
                    cmd.Parameters.AddWithValue("@budget", (double)project.EstimatedBudget);
                    cmd.Parameters.AddWithValue("@cost", (double)project.CalculatedCost);
                    cmd.Parameters.AddWithValue("@pm", project.ProjectManager.Id);
                    cmd.Parameters.AddWithValue("@pmHours", (double)project.PmHours);

                    project.SetId(Convert.ToInt32(cmd.ExecuteScalar()));
                }

                foreach (var a in project.Assignments)
                {
                    using (var cmd = new SQLiteCommand(
                        @"INSERT INTO ProjectAssignments (ProjectId, EmployeeId, AllocatedHours)
                          VALUES (@projectId, @employeeId, @hours);", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@projectId", project.Id);
                        cmd.Parameters.AddWithValue("@employeeId", a.EmployeeId);
                        cmd.Parameters.AddWithValue("@hours", (double)a.AllocatedHours);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }

            // Naplózás csak a sikeres mentés (Commit) UTÁN
            AppLogger.DbInsert("Projects", project.Id.ToString());
            foreach (var a in project.Assignments)
                AppLogger.DbInsert("ProjectAssignments", "ProjectId=" + project.Id + ",EmployeeId=" + a.EmployeeId);
        }

        /// <summary>Az elfogadott projektek visszatöltése; a munkatársakat a már betöltött listából keresi ki.</summary>
        public List<Project> GetAllProjects(List<Employee> employees)
        {
            var byId = employees.ToDictionary(e => e.Id);
            var projects = new Dictionary<int, Project>();

            using (var conn = Open())
            {
                using (var cmd = new SQLiteCommand(
                    "SELECT Id, Name, EstimatedBudget, PmEmployeeId FROM Projects ORDER BY Id", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int id = Convert.ToInt32(r.GetValue(0));
                        int pmId = Convert.ToInt32(r.GetValue(3));

                        Employee pmEmployee;
                        byId.TryGetValue(pmId, out pmEmployee);
                        var pm = pmEmployee as ProjectManager;
                        if (pm == null)
                            throw new InvalidOperationException("A(z) " + id + ". projekt PM-je nem található.");

                        // A PmHours a beosztásokból számolódik (TotalDeveloperHours × 0.20)
                        projects.Add(id, new Project(
                            id,
                            r.GetString(1),
                            Convert.ToDecimal(r.GetValue(2)),
                            pm));
                    }
                }

                using (var cmd = new SQLiteCommand(
                    "SELECT ProjectId, EmployeeId, AllocatedHours FROM ProjectAssignments", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Project project;
                        Employee emp;
                        if (projects.TryGetValue(Convert.ToInt32(r.GetValue(0)), out project) &&
                            byId.TryGetValue(Convert.ToInt32(r.GetValue(1)), out emp) &&
                            emp is Programmer)
                        {
                            project.AddAssignment((Programmer)emp, Convert.ToDecimal(r.GetValue(2)));
                        }
                    }
                }
            }

            return projects.Values.ToList();
        }
    }
}