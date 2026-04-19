using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TextEditor
{
    public class DeclareVariablesDialog : Form
    {
        private DataGridView _grid;
        private Button _btnOk;
        private Button _btnCancel;
        private Button _btnAdd;
        private Button _btnRemove;

        public List<(string name, string type, string value)> Variables { get; private set; }
            = new List<(string, string, string)>();

        public DeclareVariablesDialog(
            IEnumerable<(string name, string type, string value)> existing)
        {
            Text            = "Объявление переменных";
            Size            = new Size(480, 340);
            MinimumSize     = new Size(400, 280);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;

            _grid = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colName",  HeaderText = "Имя",       FillWeight = 35 });
            _grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "colType", HeaderText = "Тип", FillWeight = 30,
                Items = { "Int", "Float", "Bool" },
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colValue", HeaderText = "Начальное значение", FillWeight = 35 });

            foreach (var (name, type, val) in existing)
                AddRow(name, type, val);

            _btnAdd    = new Button { Text = "+ Добавить", Width = 100 };
            _btnRemove = new Button { Text = "− Удалить",  Width = 100 };
            _btnAdd.Click    += (_, __) => AddRow("", "Int", "");
            _btnRemove.Click += (_, __) =>
            {
                if (_grid.SelectedRows.Count > 0)
                    _grid.Rows.Remove(_grid.SelectedRows[0]);
            };

            var panelTop = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 36,
                FlowDirection = FlowDirection.LeftToRight,
                Padding       = new Padding(4)
            };
            panelTop.Controls.Add(_btnAdd);
            panelTop.Controls.Add(_btnRemove);

            _btnOk     = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Width = 80 };
            _btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 80 };
            _btnOk.Click += OnOkClick;

            var panelBottom = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding       = new Padding(4)
            };
            panelBottom.Controls.Add(_btnCancel);
            panelBottom.Controls.Add(_btnOk);

            Controls.Add(_grid);
            Controls.Add(panelTop);
            Controls.Add(panelBottom);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void AddRow(string name, string type, string value)
        {
            int ri = _grid.Rows.Add(name, type, value);
            if (string.IsNullOrEmpty(type))
                _grid.Rows[ri].Cells["colType"].Value = "Int";
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            Variables.Clear();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                string name  = row.Cells["colName"].Value?.ToString()?.Trim()  ?? "";
                string type  = row.Cells["colType"].Value?.ToString()           ?? "Int";
                string value = row.Cells["colValue"].Value?.ToString()?.Trim() ?? "";

                if (string.IsNullOrEmpty(name)) continue;

                if (!IsValidIdentifier(name))
                {
                    MessageBox.Show($"Некорректное имя переменной: '{name}'",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                Variables.Add((name, type, value));
            }
        }

        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsLetter(name[0])) return false;
            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;
            return true;
        }
    }
}
