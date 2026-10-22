using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Test
{
    // Trace naplózás a trace.txt fájlba (System.Diagnostics.Trace + TextWriterTraceListener).
    // A fájl a program mellé (bin\Debug\trace.txt) kerül, és hozzáfűzéssel bővül.
    public static class AppLogger
    {
        private const string FileName = "trace.txt";
        private static bool initialized;

        public static void Init()
        {
            if (initialized) return;

            Trace.Listeners.Add(new TextWriterTraceListener(FileName, "traceFile"));
            // Minden sor azonnal kiíródik, így a fájl bezárás után (vagy akár futás közben) is olvasható
            Trace.AutoFlush = true;
            initialized = true;
        }

        private static string Now()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string Num(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // DB_INSERT: tábla neve, rekord azonosítója, időbélyeg
        public static void DbInsert(string table, string recordId)
        {
            Trace.WriteLine(Now() + " | DB_INSERT | Table=" + table + " | Id=" + recordId);
        }

        // PROJECT_ACCEPTED: csak sikeres elfogadás és mentés után hívjuk
        public static void ProjectAccepted(Project p)
        {
            string developers = string.Join(", ",
                p.Assignments.Select(a => "EmployeeId=" + a.EmployeeId + " Hours=" + Num(a.AllocatedHours)));

            Trace.WriteLine(Now() + " | PROJECT_ACCEPTED"
                + " | ProjectId=" + p.Id
                + " | Name=" + p.Name
                + " | EstimatedBudget=" + Num(p.EstimatedBudget)
                + " | CalculatedCost=" + Num(p.CalculatedCost)
                + " | PmEmployeeId=" + p.ProjectManager.Id
                + " | PmHours=" + Num(p.PmHours)
                + " | Developers=[" + developers + "]");
        }
    }
}