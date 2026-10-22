using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    // A programmer's assignment to a project (one row of the ProjectAssignments table)
    public class ProjectAssignment
    {
        // The assigned programmer (only a Programmer can be a developer on a project)
        public Programmer Programmer { get; private set; }

        // Planned hours of this programmer on the project
        public decimal AllocatedHours { get; private set; }

        // Shortcut to the programmer's id (stored as EmployeeId in the database)
        public int EmployeeId
        {
            get { return Programmer.Id; }
        }

        // Cost of this assignment: HourlyRate × AllocatedHours
        public decimal Cost
        {
            get { return Programmer.GetHourlyRate() * AllocatedHours; }
        }

        public ProjectAssignment(Programmer programmer, decimal allocatedHours)
        {
            // Validation: a programmer is required and the hours must be positive
            if (programmer == null)
                throw new ArgumentNullException("programmer", "A programozó megadása kötelező.");
            if (allocatedHours <= 0)
                throw new ArgumentException("A tervezett óraszámnak pozitívnak kell lennie.");

            Programmer = programmer;
            AllocatedHours = allocatedHours;
        }
    }

    // A project with its budget, PM and developer team (one row of the Projects table)
    public class Project
    {
        // PmHours = TotalDeveloperHours × 0.20 (the PM works 20% of the developer hours)
        public const decimal PmHourRatio = 0.20m;

        // Private fields behind the validated properties (encapsulation)
        private string name;
        private decimal estimatedBudget;

        // The developer team; only changed through AddAssignment, so the rules are always checked
        private readonly List<ProjectAssignment> assignments = new List<ProjectAssignment>();

        // Database id (0 until the project is saved)
        public int Id { get; private set; }

        // Project name; cannot be empty
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

        // Estimated budget given by the client (RON); must be positive
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

        // Required project manager (the type guarantees it is a ProjectManager, not a Director)
        public ProjectManager ProjectManager { get; private set; }

        // Read-only view of the team: other classes can read it but cannot add or remove items
        public IReadOnlyList<ProjectAssignment> Assignments
        {
            get { return assignments.AsReadOnly(); }
        }

        // TotalDeveloperHours = sum of the programmers' AllocatedHours (LINQ Sum)
        public decimal TotalDeveloperHours
        {
            get { return assignments.Sum(a => a.AllocatedHours); }
        }

        // PmHours = TotalDeveloperHours × 0.20 (calculated, never typed in)
        public decimal PmHours
        {
            get { return TotalDeveloperHours * PmHourRatio; }
        }

        // CalculatedCost = Σ(HourlyRate × AllocatedHours) + PM HourlyRate × PmHours
        public decimal CalculatedCost
        {
            get
            {
                decimal developerCost = assignments.Sum(a => a.Cost);        // all developers together
                decimal pmCost = ProjectManager.GetHourlyRate() * PmHours;   // project manager
                return Math.Round(developerCost + pmCost, 2);                // rounded to 2 decimals (RON)
            }
        }

        // True if the cost does not exceed the client's budget
        public bool IsWithinBudget
        {
            get { return CalculatedCost <= EstimatedBudget; }
        }

        // Acceptable: CalculatedCost ≤ EstimatedBudget, has a PM and at least one developer
        public bool IsAcceptable
        {
            get { return ProjectManager != null && assignments.Count > 0 && IsWithinBudget; }
        }

        public Project(int id, string name, decimal estimatedBudget, ProjectManager projectManager)
        {
            // A project cannot exist without a project manager
            if (projectManager == null)
                throw new ArgumentNullException("projectManager", "Projektmenedzser megadása kötelező.");

            Id = id;
            Name = name;                        // validated by the property setter
            EstimatedBudget = estimatedBudget;  // validated by the property setter
            ProjectManager = projectManager;
        }

        // Adds a developer to the team; the same developer can be added only once
        public void AddAssignment(Programmer programmer, decimal allocatedHours)
        {
            if (programmer != null && assignments.Any(a => a.EmployeeId == programmer.Id))
                throw new InvalidOperationException("Ez a fejlesztő már szerepel a projektben.");

            // ProjectAssignment's constructor checks for null and for positive hours
            assignments.Add(new ProjectAssignment(programmer, allocatedHours));
        }

        // Used only by the database layer after saving (sets the id generated by SQLite)
        internal void SetId(int id)
        {
            Id = id;
        }
    }
}