using System;
using System.Linq;
using System.Windows.Forms;
using Test;

namespace Test
{
    // A Designerben tegyél a formra:
    //   Button       -> Name: btnGenerate, Text: Generate employees
    //   DataGridView -> Name: dgvEmployees (ReadOnly = true)
    //   Label        -> Name: lblCount
    // A Click eseményt itt kódból kötöm be, ezért a gombra NE kattints duplán a Designerben.
    public partial class Form1 : Form
    {
        private readonly Database db;
        private readonly EmployeeGenerater generator;

        public Form1()
        {
            InitializeComponent();

            db = new Database();
            generator = new EmployeeGenerater(db);

            btnGenerate.Click += btnGenerate_Click;
            LoadEmployees();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
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
                MessageBox.Show("Hiba a generálás közben: " + ex.Message, "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadEmployees()
        {
            var employees = db.GetAllEmployees();

            dgvEmployees.DataSource = employees
                .Select(x => new
                {
                    x.Id,
                    Név = x.Name,
                    Munkakör = x.GetRoleName(),
                    Havi_bér_RON = x.MonthlySalary,
                    Órabér_RON = x.GetHourlyRate()
                })
                .ToList();

            lblCount.Text = "Munkatársak: " + employees.Count;
        }
    }
}