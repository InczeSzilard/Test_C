using System;
using System.Collections.Generic;
using System.IO;

namespace Test
{
    public class EmployeeGenerater
    {
        private static readonly string[] LastNames =
        {
            "Kovács", "Szabó", "Nagy", "Tóth", "Horváth", "Varga", "Kiss", "Molnár",
            "Németh", "Farkas", "Balogh", "Papp", "Lakatos", "Takács", "Juhász", "Fekete"
        };

        private static readonly string[] FirstNames =
        {
            "Anna", "Péter", "Gábor", "Eszter", "László", "Zsófia", "Bence", "Réka",
            "Tamás", "Katalin", "Dávid", "Júlia", "Attila", "Noémi", "Levente", "Csilla"
        };

        // Célszámok: a kötelező minimum felett, összesen pontosan 100 fő
        private static readonly Dictionary<string, int> Targets = new Dictionary<string, int>
        {
            { Database.Key("Programmer", "Junior"), 36 },  // min 30
            { Database.Key("Programmer", "Mid"),    30 },  // min 20
            { Database.Key("Programmer", "Senior"), 15 },  // min 10
            { Database.Key("ProjectManager", null), 13 },  // min 10
            { Database.Key("Director", null),        3 },  // min 2
            { Database.Key("HrDirector", null),      3 },  // min 2
        };

        private readonly Random rnd = new Random();
        private readonly Database db;

        public EmployeeGenerater(Database db)
        {
            this.db = db;
        }

        /// <summary>
        /// Csak a hiányzó munkatársakat hozza létre, így ismételt kattintásra
        /// nem keletkeznek duplikátumok, és semmit nem töröl.
        /// </summary>
        /// <returns>Az újonnan mentett munkatársak száma.</returns>
        public int Generate()
        {
            Dictionary<string, int> existing = db.CountByRole();
            var newEmployees = new List<Employee>();

            foreach (var target in Targets)
            {
                int current;
                existing.TryGetValue(target.Key, out current);

                for (int i = current; i < target.Value; i++)
                    newEmployees.Add(Create(target.Key));
            }

            db.InsertEmployees(newEmployees);
            return newEmployees.Count;
        }

        private Employee Create(string key)
        {
            string name = LastNames[rnd.Next(LastNames.Length)] + " " + FirstNames[rnd.Next(FirstNames.Length)];

            switch (key)
            {
                case "Programmer:Junior": return new Programmer(0, name, Salary(4500, 7000), Seniority.Junior);
                case "Programmer:Mid": return new Programmer(0, name, Salary(7000, 11000), Seniority.Mid);
                case "Programmer:Senior": return new Programmer(0, name, Salary(11000, 17000), Seniority.Senior);
                case "ProjectManager:": return new ProjectManager(0, name, Salary(10000, 16000));
                case "Director:": return new Director(0, name, Salary(18000, 25000));
                case "HrDirector:": return new HrDirector(0, name, Salary(14000, 20000));
                default: throw new ArgumentException("Ismeretlen kulcs: " + key);
            }
        }

        // Kerek, 100 RON-ra végződő bér a megadott tartományban (RON)
        private decimal Salary(int min, int max)
        {
            return rnd.Next(min / 100, max / 100 + 1) * 100m;
        }
    }
}