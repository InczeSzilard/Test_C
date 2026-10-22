using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Test
{
    // 2. ablak: projekt összeállítása, költség, elfogadás, elfogadott projektek.
    // Teljesen kódból épül fel, ezért a Visual Studio kódnézetben nyitja meg.
    [DesignerCategory("Code")]
    public class ProjectsForm : Form
    {
        private readonly Database db;
        private List<Employee> employees = new List<Employee>();

        // Az éppen összeállított (még el nem fogadott) csapat
        private readonly List<ProjectAssignment> draftTeam = new List<ProjectAssignment>();

        private TextBox projectNameBox;
        private NumericUpDown budgetInput;
        private ComboBox pmCombo;
        private Label hoursLabel;
        private ComboBox programmerCombo;
        private NumericUpDown programmerHoursInput;
        private Button addMemberButton;
        private Button removeMemberButton;
        private DataGridView teamGrid;
        private Label costLabel;
        private Label budgetStatusLabel;
        private Button acceptButton;
        private DataGridView projectsGrid;

        public ProjectsForm(Database db)
        {
            this.db = db;

            BuildUi();

            try
            {
                LoadEmployees();
                LoadProjects();
            }
            catch (Exception ex)
            {
                UiHelpers.ShowError("Hiba az adatok betöltésekor: " + ex.Message);
            }

            RefreshTeam();
        }

        // =====================================================================
        //  Felület
        // =====================================================================

        private void BuildUi()
        {
            Text = "Projektcsapat-tervező – Projektek";
            MinimumSize = new Size(1000, 650);
            Size = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterScreen;
            Padding = new Padding(8);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 470));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var editorBox = new GroupBox { Text = "Új projekt", Dock = DockStyle.Fill, Padding = new Padding(8) };
            var acceptedBox = new GroupBox { Text = "Elfogadott projektek", Dock = DockStyle.Fill, Padding = new Padding(8) };
            layout.Controls.Add(editorBox, 0, 0);
            layout.Controls.Add(acceptedBox, 1, 0);

            projectsGrid = UiHelpers.CreateGrid();
            acceptedBox.Controls.Add(projectsGrid);

            var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            editorBox.Controls.Add(form);

            projectNameBox = new TextBox();
            budgetInput = UiHelpers.CreateNumber(0, 100000000, 1000, 0);
            pmCombo = UiHelpers.CreateEmployeeCombo();
            programmerCombo = UiHelpers.CreateEmployeeCombo();
            programmerHoursInput = UiHelpers.CreateNumber(1, 10000, 1, 80);
            programmerHoursInput.Width = 90;
            addMemberButton = new Button { Text = "Hozzáadás", AutoSize = true };
            removeMemberButton = new Button { Text = "Kijelölt eltávolítása", AutoSize = true };
            teamGrid = UiHelpers.CreateGrid();
            hoursLabel = new Label { AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
            costLabel = new Label { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(3, 8, 3, 3) };
            budgetStatusLabel = new Label { AutoSize = true, Margin = new Padding(3, 3, 3, 8) };
            acceptButton = new Button { Text = "Accept project", AutoSize = true, MinimumSize = new Size(140, 32) };

            var hoursRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0), WrapContents = false };
            hoursRow.Controls.Add(programmerHoursInput);
            hoursRow.Controls.Add(addMemberButton);

            UiHelpers.AddRow(form, "Projekt neve", projectNameBox);
            UiHelpers.AddRow(form, "Becsült keret (RON)", budgetInput);
            UiHelpers.AddRow(form, "Projektmenedzser", pmCombo);
            UiHelpers.AddWide(form, new Label
            {
                Text = "Fejlesztőcsapat",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 12, 3, 3)
            }, false);
            UiHelpers.AddRow(form, "Programozó", programmerCombo);
            UiHelpers.AddRow(form, "AllocatedHours", hoursRow);
            UiHelpers.AddWide(form, teamGrid, true);
            UiHelpers.AddWide(form, removeMemberButton, false);
            UiHelpers.AddWide(form, hoursLabel, false);
            UiHelpers.AddWide(form, costLabel, false);
            UiHelpers.AddWide(form, budgetStatusLabel, false);
            UiHelpers.AddWide(form, acceptButton, false);

            // Alsó sáv: vissza a munkatársakhoz
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };
            var backButton = new Button { Text = "← Vissza a munkatársakhoz", AutoSize = true, MinimumSize = new Size(180, 30) };
            backButton.Click += (s, e) => Close();
            bottom.Controls.Add(backButton);

            Controls.Add(layout);
            Controls.Add(bottom);

            // Események
            projectNameBox.TextChanged += (s, e) => UpdateCost();
            budgetInput.ValueChanged += (s, e) => UpdateCost();
            pmCombo.SelectedIndexChanged += (s, e) => UpdateCost();
            addMemberButton.Click += AddMemberButton_Click;
            removeMemberButton.Click += RemoveMemberButton_Click;
            acceptButton.Click += AcceptButton_Click;
        }

        // =====================================================================
        //  Adatok
        // =====================================================================

        private void LoadEmployees()
        {
            employees = db.GetAllEmployees();

            // PM csak ProjectManager, a csapatba csak Programmer választható.
            // Director és HrDirector a munkatársak ablakban látszik, de ide nem kerül be.
            FillCombo(pmCombo, employees.OfType<ProjectManager>().OrderBy(p => p.Name));
            FillCombo(programmerCombo, employees.OfType<Programmer>()
                                                .OrderBy(p => p.Seniority)
                                                .ThenBy(p => p.Name));
        }

        private void LoadProjects()
        {
            var projects = db.GetAllProjects(employees);

            projectsGrid.DataSource = projects
                .Select(p => new
                {
                    p.Id,
                    Projekt = p.Name,
                    PM = p.ProjectManager.Name,
                    PM_órák = p.PmHours,
                    Programozók = p.Assignments.Count,
                    Keret_RON = p.EstimatedBudget,
                    Költség_RON = p.CalculatedCost
                })
                .ToList();
        }

        private static void FillCombo(ComboBox combo, IEnumerable<Employee> items)
        {
            combo.BeginUpdate();
            combo.Items.Clear();
            foreach (var item in items)
                combo.Items.Add(item);
            combo.EndUpdate();
        }

        // =====================================================================
        //  Eseménykezelők
        // =====================================================================

        private void AddMemberButton_Click(object sender, EventArgs e)
        {
            var programmer = programmerCombo.SelectedItem as Programmer;
            if (programmer == null)
            {
                UiHelpers.ShowWarning("Válassz ki egy programozót.");
                return;
            }
            if (draftTeam.Any(a => a.EmployeeId == programmer.Id))
            {
                UiHelpers.ShowWarning("Ez a programozó már a csapatban van.");
                return;
            }

            draftTeam.Add(new ProjectAssignment(programmer, programmerHoursInput.Value));
            RefreshTeam();
        }

        private void RemoveMemberButton_Click(object sender, EventArgs e)
        {
            if (teamGrid.CurrentRow == null || teamGrid.CurrentRow.Index >= draftTeam.Count)
            {
                UiHelpers.ShowWarning("Jelölj ki egy csapattagot az eltávolításhoz.");
                return;
            }

            draftTeam.RemoveAt(teamGrid.CurrentRow.Index);
            RefreshTeam();
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            try
            {
                var pm = (ProjectManager)pmCombo.SelectedItem;
                var project = new Project(0, projectNameBox.Text, budgetInput.Value, pm);
                foreach (var a in draftTeam)
                    project.AddAssignment(a.Programmer, a.AllocatedHours);

                // Csak a keretbe beleférő projekt fogadható el
                // Csak elfogadható projekt menthető: van PM, legalább egy fejlesztő, és belefér a keretbe
                if (!project.IsAcceptable)
                {
                    UiHelpers.ShowWarning("A projekt nem fér bele a költségkeretbe.");
                    return;
                }

                db.SaveProject(project);

                // Csak sikeres, kereten belüli és elmentett projekt kerül a naplóba
                AppLogger.ProjectAccepted(project);
                MessageBox.Show("A(z) \"" + project.Name + "\" projekt elfogadva és elmentve.\n"
                                + "Számított költség: " + project.CalculatedCost.ToString("N2") + " RON");

                ClearDraft();
                LoadProjects();
            }
            catch (Exception ex)
            {
                UiHelpers.ShowError("A projekt nem menthető: " + ex.Message);
            }
        }

        // =====================================================================
        //  Csapat és költség
        // =====================================================================

        private void RefreshTeam()
        {
            teamGrid.DataSource = draftTeam
                .Select(a => new
                {
                    a.Programmer.Id,
                    Név = a.Programmer.Name,
                    Senioritás = a.Programmer.Seniority.ToString(),
                    Órák = a.AllocatedHours,
                    Órabér = Math.Round(a.Programmer.GetHourlyRate(), 2),
                    Költség_RON = Math.Round(a.Cost, 2)
                })
                .ToList();

            UpdateCost();
        }

        private void UpdateCost()
        {
            var pm = pmCombo.SelectedItem as ProjectManager;

            // TotalDeveloperHours = Σ AllocatedHours, PmHours = TotalDeveloperHours × 0.20
            decimal developerHours = draftTeam.Sum(a => a.AllocatedHours);
            decimal pmHours = developerHours * Project.PmHourRatio;

            // CalculatedCost = Σ(HourlyRate × AllocatedHours) + PM HourlyRate × PmHours
            decimal developerCost = draftTeam.Sum(a => a.Cost);
            decimal pmCost = pm != null ? pm.GetHourlyRate() * pmHours : 0m;
            decimal cost = Math.Round(developerCost + pmCost, 2);
            decimal budget = budgetInput.Value;

            hoursLabel.Text = "Fejlesztői órák: " + developerHours.ToString("0.##")
                              + "   |   PM órák: " + pmHours.ToString("0.##")
                              + " (20% × " + developerHours.ToString("0.##") + ")";
            costLabel.Text = "Számított költség: " + cost.ToString("N2") + " RON";

            bool overBudget = budget > 0 && cost > budget;

            string problem = null;
            if (overBudget)
                problem = "A projekt nem fér bele a költségkeretbe. Különbség: "
                          + (cost - budget).ToString("N2") + " RON";
            else if (string.IsNullOrWhiteSpace(projectNameBox.Text))
                problem = "Add meg a projekt nevét.";
            else if (budget <= 0)
                problem = "Add meg a becsült költségkeretet.";
            else if (pm == null)
                problem = "Válassz projektmenedzsert.";
            else if (draftTeam.Count == 0)
                problem = "Adj hozzá legalább egy fejlesztőt.";

            if (problem == null)
            {
                budgetStatusLabel.Text = "Belefér a keretbe (marad " + (budget - cost).ToString("N2") + " RON).";
                budgetStatusLabel.ForeColor = Color.ForestGreen;
            }
            else
            {
                budgetStatusLabel.Text = problem;
                budgetStatusLabel.ForeColor = overBudget ? Color.Firebrick : SystemColors.GrayText;
            }

            // Keretet túllépő vagy hiányos projekt nem menthető
            acceptButton.Enabled = problem == null;
        }

        private void ClearDraft()
        {
            projectNameBox.Clear();
            budgetInput.Value = 0;
            pmCombo.SelectedIndex = -1;
            draftTeam.Clear();
            RefreshTeam();
        }
    }
}