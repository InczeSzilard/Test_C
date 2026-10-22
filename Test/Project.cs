using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    // Egy programozó beosztása egy projektbe (ProjectAssignments tábla egy sora)
    public class ProjectAssignment
    {
        public Programmer Programmer { get; private set; }
        public decimal AllocatedHours { get; private set; }

        public int EmployeeId
        {
            get { return Programmer.Id; }
        }

        public decimal Cost
        {
            get { return Programmer.GetHourlyRate() * AllocatedHours; }
        }

        public ProjectAssignment(Programmer programmer, decimal allocatedHours)
        {
            if (programmer == null)
                throw new ArgumentNullException("programmer", "A programozó megadása kötelező.");
            if (allocatedHours <= 0)
                throw new ArgumentException("Az órák számának pozitívnak kell lennie.");

            Programmer = programmer;
            AllocatedHours = allocatedHours;
        }
    }

    public class Project
    {
        private string name;
        private decimal estimatedBudget;
        private decimal pmHours;
        private readonly List<ProjectAssignment> assignments = new List<ProjectAssignment>();

        public int Id { get; private set; }

        public string Name
        {
            get { return name; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("A projekt neve nem lehet üres.");
                name = value.Trim();
            }
        }

        // Az ügyfél által megadott becsült költségkeret (RON)
        public decimal EstimatedBudget
        {
            get { return estimatedBudget; }
            private set
            {
                if (value <= 0)
                    throw new ArgumentException("A becsült költségkeretnek pozitívnak kell lennie.");
                estimatedBudget = value;
            }
        }

        // Kötelező projektmenedzser
        public ProjectManager ProjectManager { get; private set; }

        public decimal PmHours
        {
            get { return pmHours; }
            private set
            {
                if (value <= 0)
                    throw new ArgumentException("A PM óráinak száma pozitív kell legyen.");
                pmHours = value;
            }
        }

        public IReadOnlyList<ProjectAssignment> Assignments
        {
            get { return assignments.AsReadOnly(); }
        }

        public Project(int id, string name, decimal estimatedBudget, ProjectManager projectManager, decimal pmHours)
        {
            if (projectManager == null)
                throw new ArgumentNullException("projectManager", "Projektmenedzser megadása kötelező.");

            Id = id;
            Name = name;
            EstimatedBudget = estimatedBudget;
            ProjectManager = projectManager;
            PmHours = pmHours;
        }

        public void AddAssignment(Programmer programmer, decimal allocatedHours)
        {
            if (programmer != null && assignments.Any(a => a.EmployeeId == programmer.Id))
                throw new InvalidOperationException("Ez a programozó már szerepel a projektben.");

            assignments.Add(new ProjectAssignment(programmer, allocatedHours));
        }

        // Tervezett személyi költség: PM órabér * PM órák + programozók órabér * órák
        public decimal CalculatedCost
        {
            get
            {
                decimal cost = ProjectManager.GetHourlyRate() * PmHours + assignments.Sum(a => a.Cost);
                return Math.Round(cost, 2);
            }
        }

        public bool IsWithinBudget
        {
            get { return CalculatedCost <= EstimatedBudget; }
        }

        // Csak az adatbázis-réteg használja mentés után
        internal void SetId(int id)
        {
            Id = id;
        }
    }
}