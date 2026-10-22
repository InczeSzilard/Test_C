using System.Drawing;
using System.Windows.Forms;

namespace Test
{
    // Közös segédmetódusok mindkét ablakhoz
    public static class UiHelpers
    {
        public static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window
            };
            // "Havi_bér_RON" -> "Havi bér RON"
            grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in grid.Columns)
                    col.HeaderText = col.HeaderText.Replace('_', ' ');
            };
            return grid;
        }

        public static NumericUpDown CreateNumber(decimal min, decimal max, decimal step, decimal value)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = step,
                Value = value,
                ThousandsSeparator = true,
                DecimalPlaces = 0
            };
        }

        public static ComboBox CreateEmployeeCombo()
        {
            var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FormattingEnabled = true };
            combo.Format += (s, e) =>
            {
                var emp = e.ListItem as Employee;
                if (emp != null)
                    e.Value = "#" + emp.Id + "  " + emp.Name + " – " + emp.GetRoleName()
                              + " (" + emp.GetHourlyRate().ToString("N2") + " RON/óra)";
            };
            return combo;
        }

        public static void AddRow(TableLayoutPanel table, string labelText, Control input)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int row = table.RowStyles.Count - 1;
            table.RowCount = table.RowStyles.Count;

            table.Controls.Add(new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 3)
            }, 0, row);

            if (!(input is FlowLayoutPanel))
                input.Dock = DockStyle.Fill;
            table.Controls.Add(input, 1, row);
        }

        public static void AddWide(TableLayoutPanel table, Control control, bool fill)
        {
            table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
            int row = table.RowStyles.Count - 1;
            table.RowCount = table.RowStyles.Count;

            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 2);
        }

        public static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Figyelem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}