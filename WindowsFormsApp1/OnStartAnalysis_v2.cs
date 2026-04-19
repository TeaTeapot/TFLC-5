// =========================================================
// ЗАМЕНИ OnStartAnalysis в Program.cs (класс MainForm)
// Добавь в начало файла:
//   using System.Linq;
//
// Также добавь новое поле в класс MainForm:
//   private AstNode _lastAst = null;
// =========================================================

private void OnStartAnalysis(object sender, EventArgs e)
{
    string inputText = textBoxEditor.Text;

    // ── 1. Лексический анализ ────────────────────────────────
    var (tokens, lexErrors) = _scanner.Scan(inputText);
    _lastTokens = tokens;

    dataGridViewResults.Rows.Clear();

    // Fix 1: все лекс. ошибки сразу в таблицу
    foreach (var errTok in lexErrors)
    {
        int ri = dataGridViewResults.Rows.Add(
            errTok.Value,
            errTok.Position,
            $"Лексическая ошибка: {errTok.Type}");
        dataGridViewResults.Rows[ri].Tag = errTok;
        dataGridViewResults.Rows[ri].DefaultCellStyle.BackColor = Color.LightCoral;
    }

    // ── 2. Синтаксический анализ + построение AST ────────────
    var validTokens = tokens.Where(t => t.Code != -1).ToList();
    var parser = new Parser();
    var (ast, syntaxErrors) = parser.Parse(validTokens, inputText);
    _lastAst = ast;

    foreach (var err in syntaxErrors)
    {
        int ri = dataGridViewResults.Rows.Add(
            err.Fragment,
            err.Location,
            err.Description);
        dataGridViewResults.Rows[ri].Tag = err;
        dataGridViewResults.Rows[ri].DefaultCellStyle.BackColor = Color.LightCoral;
    }

    // ── 3. Семантический анализ ──────────────────────────────
    // Считываем предобъявленные переменные из SymbolTable UI
    // (если у вас есть отдельная форма/таблица деклараций).
    // Здесь используем пустой список — пользователь объявляет
    // переменные через диалог (см. кнопку «Объявить переменные»).
    var predeclared = GetPredeclaredVariables();   // см. ниже

    var semanticAnalyzer = new SemanticAnalyzer();
    var (semErrors, symbolTable) = semanticAnalyzer.Analyze(ast, predeclared);

    foreach (var err in semErrors)
    {
        int ri = dataGridViewResults.Rows.Add(
            "",
            err.Location,
            $"Семантическая ошибка: {err.Message}");
        dataGridViewResults.Rows[ri].DefaultCellStyle.BackColor = Color.LightYellow;
    }

    // ── 4. Итоговый счётчик ──────────────────────────────────
    int totalErrors = lexErrors.Count + syntaxErrors.Count + semErrors.Count;

    int ri2 = dataGridViewResults.Rows.Add(
        "", "",
        $"Всего ошибок: {totalErrors}  " +
        $"(лексических: {lexErrors.Count}, " +
        $"синтаксических: {syntaxErrors.Count}, " +
        $"семантических: {semErrors.Count})");
    dataGridViewResults.Rows[ri2].DefaultCellStyle.BackColor =
        totalErrors == 0 ? Color.LightGreen : Color.LightGray;
    dataGridViewResults.Rows[ri2].DefaultCellStyle.Font =
        new Font(dataGridViewResults.Font, FontStyle.Bold);

    // ── 5. Вывод AST в область результатов ──────────────────
    if (ast != null)
    {
        string astText = AstPrinter.Print(ast);
        // Добавляем AST в конец таблицы результатов одной строкой
        int ri3 = dataGridViewResults.Rows.Add("AST", "", astText);
        dataGridViewResults.Rows[ri3].DefaultCellStyle.BackColor = Color.AliceBlue;
        dataGridViewResults.Rows[ri3].DefaultCellStyle.Font =
            new Font("Courier New", 8f);
    }

    // ── 6. Итоговое сообщение ────────────────────────────────
    if (totalErrors == 0)
        MessageBox.Show("Ошибок не найдено.", "Результат анализа",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    else
        MessageBox.Show(
            $"Обнаружено ошибок: {totalErrors}\n" +
            $"  • лексических:    {lexErrors.Count}\n" +
            $"  • синтаксических: {syntaxErrors.Count}\n" +
            $"  • семантических:  {semErrors.Count}",
            "Результат анализа",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

// ─────────────────────────────────────────────────────────────
// Диалог объявления переменных
// Добавь кнопку «Объявить переменные» на панель инструментов
// или в меню и вызывай этот метод.
// ─────────────────────────────────────────────────────────────

// Хранилище объявленных пользователем переменных
private readonly List<(string name, string type, string value)> _declaredVars
    = new List<(string, string, string)>();

// Возвращает предобъявленные переменные для семантического анализатора
private IEnumerable<(string name, string type, string value)> GetPredeclaredVariables()
    => _declaredVars;

// Вызывается из кнопки/меню «Объявить переменные»
private void OnDeclareVariables(object sender, EventArgs e)
{
    using (var dlg = new DeclareVariablesDialog(_declaredVars))
    {
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _declaredVars.Clear();
            _declaredVars.AddRange(dlg.Variables);
        }
    }
}
