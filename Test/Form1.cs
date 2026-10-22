using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Test
{
    // 1. ablak: a Designerben felrakott vezérlőket használja.
    // Vezérlők nevei (Properties -> (Name)):
    //   DataGridView -> dgvEmployees
    //   Button       -> btnGenerate   ("Generate employees")
    //   Label        -> lblCount
    //   Button       -> btnNext       ("Next->")
    // Az eseményeket itt kódból kötöm be, ezért a gombokra NE kattints duplán a Designerben.
    public partial class Form1 : Form
    {
        private const int RequiredEmployees = 100;

        private readonly Database db;
        private readonly EmployeeGenerater generator;

        public Form1()
        {
            InitializeComponent();

            // Trace naplózás indítása (trace.txt) – az adatbázis előtt
            AppLogger.Init();

            db = new Database();
            generator = new EmployeeGenerater(db);

            // A táblázat beállításai
            dgvEmployees.ReadOnly = true;
            dgvEmployees.AllowUserToAddRows = false;
            dgvEmployees.AllowUserToDeleteRows = false;
            dgvEmployees.RowHeadersVisible = false;
            dgvEmployees.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvEmployees.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgvEmployees.Columns)
                    col.HeaderText = col.HeaderText.Replace('_', ' ');
            };

            btnGenerate.Click += BtnGenerate_Click;
            btnNext.Click += BtnNext_Click;

            try
            {
                LoadEmployees();
            }
            catch (Exception ex)
            {
                UiHelpers.ShowError("Hiba az adatok betöltésekor: " + ex.Message);
            }
        }

        private void LoadEmployees()
        {
            List<Employee> employees = db.GetAllEmployees();

            dgvEmployees.DataSource = employees
                .Select(x => new
                {
                    x.Id,
                    Név = x.Name,
                    Munkakör = x.GetRoleName(),
                    Senioritás = x is Programmer ? ((Programmer)x).Seniority.ToString() : "—",
                    Havi_bér_RON = x.MonthlySalary
                })
                .ToList();

            lblCount.Text = "Munkatársak: " + employees.Count;

            // Tovább csak akkor, ha már megvan a kötelező adathalmaz
            btnNext.Enabled = employees.Count >= RequiredEmployees;
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                int added = generator.Generate();
                LoadEmployees();

                MessageBox.Show(added == 0
                    ? "Minden munkakör megvan, nem kellett új munkatársat létrehozni."
                    : added + " új munkatárs elmentve.");
            }
            catch (Exception ex)
            {
                UiHelpers.ShowError("Hiba a generálás közben: " + ex.Message);
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            Hide();
            using (var projectsForm = new ProjectsForm(db))
            {
                projectsForm.ShowDialog();
            }
            // A projektek ablak bezárása után visszajövünk ide
            Show();
            LoadEmployees();
        }
    }
}