using System;

namespace Test
{
    public enum Seniority
    {
        Junior,
        Mid,
        Senior
    }

    // Absztrakt alaposztály: nem példányosítható
    public abstract class Person
    {
        private string name;

        public int Id { get; protected set; }

        public string Name
        {
            get { return name; }
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("A név nem lehet üres.");
                name = value.Trim();
            }
        }

        protected Person(int id, string name)
        {
            Id = id;
            Name = name;
        }

        // Polimorfizmus: minden leszármazott felülírja
        public abstract string GetRoleName();

        public override string ToString()
        {
            return Name + " (" + GetRoleName() + ")";
        }
    }

    public abstract class Employee : Person
    {
        public const decimal MonthlyWorkHours = 168m;

        private decimal monthlySalary;

        public decimal MonthlySalary
        {
            get { return monthlySalary; }
            protected set
            {
                if (value <= 0)
                    throw new ArgumentException("A havi bruttó bérnek pozitívnak kell lennie.");
                monthlySalary = value;
            }
        }

        protected Employee(int id, string name, decimal monthlySalary) : base(id, name)
        {
            MonthlySalary = monthlySalary;
        }

        public virtual decimal GetHourlyRate()
        {
            return Math.Round(MonthlySalary / MonthlyWorkHours, 2);
        }

        // Csak az adatbázis-réteg használja mentés után
        internal void SetId(int id)
        {
            Id = id;
        }
    }

    public class Programmer : Employee
    {
        public Seniority Seniority { get; private set; }

        public Programmer(int id, string name, decimal monthlySalary, Seniority seniority)
            : base(id, name, monthlySalary)
        {
            Seniority = seniority;
        }

        public override string GetRoleName()
        {
            return Seniority + " programmer";
        }
    }

    public class ProjectManager : Employee
    {
        public ProjectManager(int id, string name, decimal monthlySalary)
            : base(id, name, monthlySalary) { }

        public override string GetRoleName()
        {
            return "Project manager";
        }
    }

    public class Director : Employee
    {
        public Director(int id, string name, decimal monthlySalary)
            : base(id, name, monthlySalary) { }

        public override string GetRoleName()
        {
            return "Director";
        }
    }

    public class HrDirector : Director
    {
        public HrDirector(int id, string name, decimal monthlySalary)
            : base(id, name, monthlySalary) { }

        public override string GetRoleName()
        {
            return "HR director";
        }
    }
}