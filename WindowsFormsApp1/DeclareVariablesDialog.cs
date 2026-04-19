using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TextEditor
{
    public class DeclareVariablesDialog : Form
    {
        private DataGridView _grid;
        private Button _btnOk, _btnCancel, _btnAdd, _btnRemove;

        public List<(string name, string type, string value)> Variables { get; private set; }
            = new List<(string, string, string)>();

        public DeclareVariablesDialog(
            IEnumerable<(string name, string type, string value)> existing)
        {
            Text = "Объявление переменных";
            Size = new Size(520, 360);
            MinimumSize = new Size(420, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colName", HeaderText = "Имя", FillWeight = 30 });

            var typeCol = new DataGridViewComboBoxColumn
            {
                Name = "colType",
                HeaderText = "Тип",
                FillWeight = 25,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            typeCol.Items.AddRange("Int", "Float", "Bool");
            _grid.Columns.Add(typeCol);

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colValue",
                HeaderText = "Начальное значение (необязательно)",
                FillWeight = 45
            });

            foreach (var (n, t, v) in existing)
                AddRow(n, t, v);

            var lblHint = new Label
            {
                Text = "Bool: начальное значение — 'true' или 'false' (или оставьте пустым)",
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.Gray,
                Font = new Font(Font.FontFamily, 8f, FontStyle.Italic)
            };

            _btnAdd = new Button { Text = "+ Добавить", Width = 100 };
            _btnRemove = new Button { Text = "− Удалить", Width = 100 };

            _btnAdd.Click += (_, __) =>
            {
                AddRow("", "Int", "");
                _grid.ClearSelection();
                _grid.Rows[_grid.Rows.Count - 1].Selected = true;
                _grid.CurrentCell = _grid.Rows[_grid.Rows.Count - 1].Cells["colName"];
            };

            _btnRemove.Click += (_, __) =>
            {
                if (_grid.SelectedRows.Count > 0)
                    _grid.Rows.Remove(_grid.SelectedRows[0]);
            };

            var panelTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4)
            };
            panelTop.Controls.Add(_btnAdd);
            panelTop.Controls.Add(_btnRemove);

            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
            _btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 80 };
            _btnOk.Click += OnOkClick;

            var panelBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(4)
            };
            panelBottom.Controls.Add(_btnCancel);
            panelBottom.Controls.Add(_btnOk);

            Controls.Add(_grid);
            Controls.Add(lblHint);
            Controls.Add(panelTop);
            Controls.Add(panelBottom);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void AddRow(string name, string type, string value)
        {
            int ri = _grid.Rows.Add(name, string.IsNullOrEmpty(type) ? "Int" : type, value);
            _ = ri;
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            Variables.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (DataGridViewRow row in _grid.Rows)
            {
                string name = row.Cells["colName"].Value?.ToString()?.Trim() ?? "";
                string type = row.Cells["colType"].Value?.ToString() ?? "Int";
                string value = row.Cells["colValue"].Value?.ToString()?.Trim() ?? "";

                if (string.IsNullOrEmpty(name)) continue;

                if (!IsValidIdentifier(name))
                {
                    MessageBox.Show($"Некорректное имя переменной: '{name}'.\n" +
                        "Имя должно начинаться с буквы и содержать только буквы, цифры и '_'.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (!seen.Add(name))
                {
                    MessageBox.Show($"Переменная '{name}' объявлена более одного раза.\n" +
                        "Каждая переменная должна иметь уникальное имя.",
                        "Ошибка: дублирующее объявление",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (!string.IsNullOrEmpty(value))
                {
                    string validationError = ValidateValue(name, type, value);
                    if (validationError != null)
                    {
                        MessageBox.Show(validationError,
                            "Ошибка: несоответствие типа",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        return;
                    }
                }

                Variables.Add((name, type, value));
            }
        }

        private static string ValidateValue(string name, string type, string value)
        {
            switch (type)
            {
                case "Int":
                    if (!long.TryParse(value, out long iv))
                        return $"Переменная '{name}' имеет тип Int,\n" +
                               $"но значение '{value}' не является целым числом.";
                    if (iv < -2_147_483_648L || iv > 2_147_483_647L)
                        return $"Переменная '{name}' имеет тип Int,\n" +
                               $"но значение {iv} выходит за пределы [-2147483648; 2147483647].";
                    return null;

                case "Float":
                    if (!double.TryParse(value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                        return $"Переменная '{name}' имеет тип Float,\n" +
                               $"но значение '{value}' не является вещественным числом.\n" +
                               "Используйте точку в качестве разделителя (например: 3.14)";
                    return null;

                case "Bool":
                    if (value != "true" && value != "false")
                        return $"Переменная '{name}' имеет тип Bool.\n" +
                               $"Допустимые значения: 'true' или 'false'.\n" +
                               $"Получено: '{value}'\n\n" +
                               "Числа 0 и 1 не являются допустимыми значениями для Bool.";
                    return null;

                default:
                    return null;
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