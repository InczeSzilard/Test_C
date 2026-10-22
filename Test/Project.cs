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

        // HourlyRate × AllocatedHours
        public decimal Cost
        {
            get { return Programmer.GetHourlyRate() * AllocatedHours; }
        }

        public ProjectAssignment(Programmer programmer, decimal allocatedHours)
        {
            if (programmer == null)
                throw new ArgumentNullException("programmer", "A programozó megadása kötelező.");
            if (allocatedHours <= 0)
                throw new ArgumentException("A tervezett óraszámnak pozitívnak kell lennie.");

            Programmer = programmer;
            AllocatedHours = allocatedHours;
        }
    }

    public class Project
    {
        // PmHours = TotalDeveloperHours × 0.20
        public const decimal PmHourRatio = 0.20m;

        private string name;
        private decimal estimatedBudget;
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

        public IReadOnlyList<ProjectAssignment> Assignments
        {
            get { return assignments.AsReadOnly(); }
        }

        // TotalDeveloperHours = a programozók AllocatedHours értékeinek összege
        public decimal TotalDeveloperHours
        {
            get { return assignments.Sum(a => a.AllocatedHours); }
        }

        // PmHours = TotalDeveloperHours × 0.20
        public decimal PmHours
        {
            get { return TotalDeveloperHours * PmHourRatio; }
        }

        // CalculatedCost = Σ(HourlyRate × AllocatedHours) + PM HourlyRate × PmHours
        public decimal CalculatedCost
        {
            get
            {
                decimal developerCost = assignments.Sum(a => a.Cost);
                decimal pmCost = ProjectManager.GetHourlyRate() * PmHours;
                return Math.Round(developerCost + pmCost, 2);
            }
        }

        public bool IsWithinBudget
        {
            get { return CalculatedCost <= EstimatedBudget; }
        }

        // Elfogadhatóság: CalculatedCost ≤ EstimatedBudget, van PM és legalább egy fejlesztő
        public bool IsAcceptable
        {
            get { return ProjectManager != null && assignments.Count > 0 && IsWithinBudget; }
        }

        public Project(int id, string name, decimal estimatedBudget, ProjectManager projectManager)
        {
            if (projectManager == null)
                throw new ArgumentNullException("projectManager", "Projektmenedzser megadása kötelező.");

            Id = id;
            Name = name;
            EstimatedBudget = estimatedBudget;
            ProjectManager = projectManager;
        }

        public void AddAssignment(Programmer programmer, decimal allocatedHours)
        {
            if (programmer != null && assignments.Any(a => a.EmployeeId == programmer.Id))
                throw new InvalidOperationException("Ez a fejlesztő már szerepel a projektben.");

            assignments.Add(new ProjectAssignment(programmer, allocatedHours));
        }

        // Csak az adatbázis-réteg használja mentés után
        internal void SetId(int id)
        {
            Id = id;
        }
    }
}