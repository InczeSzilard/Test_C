using System.Drawing;
using System.Windows.Forms;

namespace Test
{
    // Shared helper methods for both windows.
    // Static class: no instance needed, call e.g. UiHelpers.CreateGrid().
    public static class UiHelpers
    {
        // Creates a read-only table (DataGridView) with the same settings everywhere
        public static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,                       // fills its container
                ReadOnly = true,                             // the user cannot edit cells
                AllowUserToAddRows = false,                  // no empty "new row" at the bottom
                AllowUserToDeleteRows = false,               // rows cannot be deleted with the Delete key
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // clicking selects the whole row
                MultiSelect = false,                         // only one row can be selected
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, // columns share the full width
                RowHeadersVisible = false,                   // hide the empty grey column on the left
                BackgroundColor = SystemColors.Window        // white background instead of dark grey
            };

            // After data binding, replace underscores in the column headers with spaces,
            // because property names cannot contain spaces: "Havi_bér_RON" -> "Havi bér RON"
            grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in grid.Columns)
                    col.HeaderText = col.HeaderText.Replace('_', ' ');
            };
            return grid;
        }

        // Creates a number input with the given range, step and starting value
        public static NumericUpDown CreateNumber(decimal min, decimal max, decimal step, decimal value)
        {
            // Order matters: Minimum and Maximum must be set before Value,
            // otherwise Value could fall outside the default 0–100 range
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = step,          // how much the arrows change the value
                Value = value,
                ThousandsSeparator = true, // e.g. 10 000 instead of 10000
                DecimalPlaces = 0          // whole numbers only
            };
        }

        // Creates a dropdown that holds Employee objects and displays them in a readable form
        public static ComboBox CreateEmployeeCombo()
        {
            // DropDownList: the user can only choose from the list, not type free text
            var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FormattingEnabled = true };

            // The items are real Employee objects; the Format event only decides what text is shown,
            // e.g. "#12  Kovács Anna – Junior programmer (31.25 RON/óra)"
            combo.Format += (s, e) =>
            {
                var emp = e.ListItem as Employee;
                if (emp != null)
                    e.Value = "#" + emp.Id + "  " + emp.Name + " – " + emp.GetRoleName()
                              + " (" + emp.GetHourlyRate().ToString("N2") + " RON/óra)";
            };
            return combo;
        }

        // Adds a "label | input" row to a two-column TableLayoutPanel
        public static void AddRow(TableLayoutPanel table, string labelText, Control input)
        {
            // New row that is as tall as its content
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int row = table.RowStyles.Count - 1;   // index of the row just added
            table.RowCount = table.RowStyles.Count;

            // Column 0: the label text
            table.Controls.Add(new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 3)   // small top margin to line up with the input
            }, 0, row);

            // Column 1: the input; stretch it to the full width
            // (except a FlowLayoutPanel, which sizes itself to its content)
            if (!(input is FlowLayoutPanel))
                input.Dock = DockStyle.Fill;
            table.Controls.Add(input, 1, row);
        }

        // Adds a control that spans both columns (e.g. a grid, a button or a status label)
        public static void AddWide(TableLayoutPanel table, Control control, bool fill)
        {
            // fill = true: the row takes all remaining height (used for the team grid)
            // fill = false: the row is only as tall as its content
            table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
            int row = table.RowStyles.Count - 1;
            table.RowCount = table.RowStyles.Count;

            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 2);   // stretch across both columns
        }

        // Shows a yellow warning message box (e.g. missing selection)
        public static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Figyelem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Shows a red error message box (e.g. database error)
        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}