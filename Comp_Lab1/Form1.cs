using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Comp_Lab1;
using FastColoredTextBoxNS;
namespace Comp_Lab1;

public partial class Form1 : Form
{
    Style BlueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Bold);
    Style BrownStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular); 
    
    public Form1()
    {
        
        InitializeComponent();
        this.AllowDrop = true;
        this.DragEnter += Form1_DragEnter;
        this.DragDrop += Form1_DragDrop;
        dgvErrors.MouseEnter += (s, e) => dgvErrors.Focus(); 
        dgvErrors.MouseWheel += DgvErrors_MouseWheel;
        EnableDragDropForAll(this);
        ApplyLocalization();
        dgvErrors.CellClick += dgvErrors_CellClick;
    }
    private void EnableDragDropForAll(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            c.AllowDrop = true;
            c.DragEnter += Form1_DragEnter;
            c.DragDrop += Form1_DragDrop;
        
            if (c.HasChildren)
            {
                EnableDragDropForAll(c);
            }
        }
    }


    private void Form1_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (string filePath in files)
        {
            try
            {
                string content = System.IO.File.ReadAllText(filePath);
                CreateNewTab(filePath, content);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }
    }
    private void CreateFile(object sender = null, EventArgs e = null)
    {
        CreateNewTab("Новый файл");
    }

    private void OpenFile(object sender = null, EventArgs e = null)
    {
        OpenFileDialog ofd = new OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            string content = System.IO.File.ReadAllText(ofd.FileName);
            CreateNewTab(ofd.FileName, content);
        }
    }
    private void SaveFile(object sender = null, EventArgs e = null) {
        if (tabControlEditor.SelectedTab == null || CurrentEditor == null) return;

        string path = tabControlEditor.SelectedTab.Tag as string;

        if (string.IsNullOrEmpty(path) || path == "Новый файл") {
            SaveFileAs(); 
        } else {
            System.IO.File.WriteAllText(path, CurrentEditor.Text);
            CurrentEditor.IsChanged = false;
            tabControlEditor.SelectedTab.Text = System.IO.Path.GetFileName(path);
            lblStatus.Text = "Файл успешно сохранен.";
        }
    }
    private void SaveFileAs(object sender = null, EventArgs e = null) {
        if (tabControlEditor.SelectedTab == null) return;

        SaveFileDialog sfd = new SaveFileDialog { Filter = "Text Files|*.txt|All Files|*.*" };
        if (sfd.ShowDialog() == DialogResult.OK) {
            tabControlEditor.SelectedTab.Tag = sfd.FileName;
            tabControlEditor.SelectedTab.Text = System.IO.Path.GetFileName(sfd.FileName);
        
            SaveFile();
        }
    }
    private void ExitApp(object sender = null, EventArgs e = null)
    {
        this.Close();
    }
    
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        foreach (TabPage tab in tabControlEditor.TabPages)
        {
            var fctb = tab.Controls[0] as FastColoredTextBox;
            if (fctb != null && fctb.IsChanged)
            {
                tabControlEditor.SelectedTab = tab; 
                var result = MessageBox.Show($"Сохранить изменения в файле {tab.Text}?", "Выход", 
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes) SaveFile();
                else if (result == DialogResult.Cancel) { e.Cancel = true; return; }
            }
        }
    }
    
    private void RunRegexSearch(object sender = null, EventArgs e = null)
    {
        if (tabControlEditor.TabCount == 0 || tabControlEditor.SelectedTab == null)
        {
            lblStatus.Text = "Ошибка: нет открытых вкладок.";
            lblStatus.ForeColor = Color.OrangeRed;
            MessageBox.Show("Сначала создайте или откройте файл для анализа.", "Вкладка не найдена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentEditor.Text))
        {
            lblStatus.Text = "Ошибка: введите текст для анализа.";
            lblStatus.ForeColor = Color.OrangeRed;
            MessageBox.Show("Пожалуйста, введите исходный код в редактор.", "Пустой ввод", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        dgvErrors.Rows.Clear();

        // Читаем выбранный тип поиска из выпадающего списка (допустим, он называется cmbSearchType)
        // По умолчанию ищем целые числа, если ничего не выбрано
        SearchPatternType searchType = SearchPatternType.IntegerNumber;
        
        // Обязательно добавьте на форму ComboBox с тремя элементами по порядку!
        if (cmbSearchType.SelectedIndex == 1) searchType = SearchPatternType.InitialsAndLastName;
        else if (cmbSearchType.SelectedIndex == 2) searchType = SearchPatternType.HslColor;
        

        // Запускаем анализатор
        var analyzer = new RegexAnalyzer();
        var results = analyzer.FindMatches(CurrentEditor.Text, searchType);
        
        // Заполняем таблицу
        foreach (var match in results)
        {
            AddMatchToGrid(match);
        }

        // Выводим статистику
        int totalMatches = results.Count;

        if (totalMatches == 0)
        {
            lblStatus.Text = "Поиск: Совпадений не найдено.";
            lblStatus.ForeColor = Color.Green;
            MessageBox.Show("Совпадений по выбранному правилу не обнаружено.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            lblStatus.Text = $"Найдено совпадений: {totalMatches}";
            lblStatus.ForeColor = Color.Blue; 
        }
    }

    // 3. Метод добавления строки в таблицу
    private void AddMatchToGrid(RegexMatchResult match)
    {
        string location = $"строка {match.Line}, поз. {match.StartPos}";
        
        // Добавляем 3 колонки: Сама подстрока | Позиция | Длина
        int rowIndex = dgvErrors.Rows.Add(match.Value, location, match.Length.ToString());
        
        // Прячем объект в Tag, чтобы достать его при клике (в dgvErrors_CellClick)
        dgvErrors.Rows[rowIndex].Tag = match;

        dgvErrors.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCyan;
        dgvErrors.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Black;
    }
    private void ShowAbout(object sender, EventArgs e) {
        MessageBox.Show("Текстовый редактор / Языковой процессор\nВерсия 1.0\nРазработчик: Обеленец Павел", "О программе");
    }

    private void ShowHelp(object sender, EventArgs e) {
        string helpPath = System.IO.Path.Combine(Application.StartupPath, "help.html");

        if (System.IO.File.Exists(helpPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpPath) 
            { 
                UseShellExecute = true 
            });
        }
        else
        {
            MessageBox.Show("Файл справки (help.html) не найден в папке с программой.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void Undo_Click(object sender, EventArgs e) {
        if (CurrentEditor.UndoEnabled) CurrentEditor.Undo();
    }

    private void Redo_Click(object sender, EventArgs e) {
        if (CurrentEditor.RedoEnabled) CurrentEditor.Redo();
    }

    private void Cut_Click(object sender, EventArgs e) {
        CurrentEditor.Cut();
    }

    private void Copy_Click(object sender, EventArgs e) {
        CurrentEditor.Copy();
    }

    private void Paste_Click(object sender, EventArgs e) {
        CurrentEditor.Paste();
    }

    private void Delete_Click(object sender, EventArgs e) {
        CurrentEditor.SelectedText = "";
    }

    private void SelectAll_Click(object sender, EventArgs e) {
        CurrentEditor.SelectAll();
    }
    private void Placeholder_Click(object sender, EventArgs e)
    {
        MessageBox.Show($"Функционал для  еще не реализован.", "Заглушка");
    }
    
    private FastColoredTextBox CreateNewTab(string fileName = "Новый файл", string content = "")
    {
        TabPage newTabPage = new TabPage(System.IO.Path.GetFileName(fileName));
        newTabPage.Tag = (fileName == "Новый файл") ? "" : fileName;
        FastColoredTextBox fctb = new FastColoredTextBox();
        fctb.Dock = DockStyle.Fill;
        fctb.Language = FastColoredTextBoxNS.Language.Custom;
        fctb.Text = content;

        fctb.HotkeysMapping.Clear();

        // --- Одиночные клавиши ---
        fctb.HotkeysMapping.Add(Keys.Tab, FCTBAction.IndentIncrease);
        fctb.HotkeysMapping.Add(Keys.Escape, FCTBAction.ClearHints);
        fctb.HotkeysMapping.Add(Keys.PageUp, FCTBAction.GoPageUp);
        fctb.HotkeysMapping.Add(Keys.PageDown, FCTBAction.GoPageDown);
        fctb.HotkeysMapping.Add(Keys.End, FCTBAction.GoEnd);
        fctb.HotkeysMapping.Add(Keys.Home, FCTBAction.GoHome);
        fctb.HotkeysMapping.Add(Keys.Left, FCTBAction.GoLeft);
        fctb.HotkeysMapping.Add(Keys.Up, FCTBAction.GoUp);
        fctb.HotkeysMapping.Add(Keys.Right, FCTBAction.GoRight);
        fctb.HotkeysMapping.Add(Keys.Down, FCTBAction.GoDown);
        fctb.HotkeysMapping.Add(Keys.Insert, FCTBAction.ReplaceMode);
        fctb.HotkeysMapping.Add(Keys.Delete, FCTBAction.DeleteCharRight);
        fctb.HotkeysMapping.Add(Keys.F3, FCTBAction.FindNext);

        // --- Shift + Клавиши (Выделение и правка) ---
        fctb.HotkeysMapping.Add(Keys.Tab | Keys.Shift, FCTBAction.IndentDecrease);
        fctb.HotkeysMapping.Add(Keys.PageUp | Keys.Shift, FCTBAction.GoPageUpWithSelection);
        fctb.HotkeysMapping.Add(Keys.PageDown | Keys.Shift, FCTBAction.GoPageDownWithSelection);
        fctb.HotkeysMapping.Add(Keys.End | Keys.Shift, FCTBAction.GoEndWithSelection);
        fctb.HotkeysMapping.Add(Keys.Home | Keys.Shift, FCTBAction.GoHomeWithSelection);
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Shift, FCTBAction.GoLeftWithSelection);
        fctb.HotkeysMapping.Add(Keys.Up | Keys.Shift, FCTBAction.GoUpWithSelection);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Shift, FCTBAction.GoRightWithSelection);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Shift, FCTBAction.GoDownWithSelection);
        fctb.HotkeysMapping.Add(Keys.Insert | Keys.Shift, FCTBAction.Paste);
        fctb.HotkeysMapping.Add(Keys.Delete | Keys.Shift, FCTBAction.Cut);

        // --- CTRL + Клавиши (Навигация и Буфер) ---
        fctb.HotkeysMapping.Add(Keys.Back | Keys.Control, FCTBAction.ClearWordLeft); 

        fctb.HotkeysMapping.Add(Keys.Space | Keys.Control, FCTBAction.AutocompleteMenu);
        fctb.HotkeysMapping.Add(Keys.End | Keys.Control, FCTBAction.GoLastLine);
        fctb.HotkeysMapping.Add(Keys.Home | Keys.Control, FCTBAction.GoFirstLine);
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Control, FCTBAction.GoWordLeft);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Control, FCTBAction.GoWordRight);
        fctb.HotkeysMapping.Add(Keys.Up | Keys.Control, FCTBAction.ScrollUp);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Control, FCTBAction.ScrollDown);
        fctb.HotkeysMapping.Add(Keys.Insert | Keys.Control, FCTBAction.Copy);
        fctb.HotkeysMapping.Add(Keys.Delete | Keys.Control, FCTBAction.ClearWordRight);

        // --- CTRL + Буквы (Стандартные команды) ---
        fctb.HotkeysMapping.Add(Keys.D0 | Keys.Control, FCTBAction.ZoomNormal);
        fctb.HotkeysMapping.Add(Keys.A | Keys.Control, FCTBAction.SelectAll);
        fctb.HotkeysMapping.Add(Keys.B | Keys.Control, FCTBAction.BookmarkLine);
        fctb.HotkeysMapping.Add(Keys.C | Keys.Control, FCTBAction.Copy);
        fctb.HotkeysMapping.Add(Keys.E | Keys.Control, FCTBAction.MacroExecute);
        fctb.HotkeysMapping.Add(Keys.F | Keys.Control, FCTBAction.FindDialog);
        fctb.HotkeysMapping.Add(Keys.G | Keys.Control, FCTBAction.GoToDialog);
        fctb.HotkeysMapping.Add(Keys.H | Keys.Control, FCTBAction.ReplaceDialog);
        fctb.HotkeysMapping.Add(Keys.I | Keys.Control, FCTBAction.AutoIndentChars);
        fctb.HotkeysMapping.Add(Keys.M | Keys.Control, FCTBAction.MacroRecord);
        fctb.HotkeysMapping.Add(Keys.N | Keys.Control, FCTBAction.GoNextBookmark);
        fctb.HotkeysMapping.Add(Keys.R | Keys.Control, FCTBAction.Redo);
        fctb.HotkeysMapping.Add(Keys.U | Keys.Control, FCTBAction.UpperCase);
        fctb.HotkeysMapping.Add(Keys.V | Keys.Control, FCTBAction.Paste);
        fctb.HotkeysMapping.Add(Keys.X | Keys.Control, FCTBAction.Cut);
        fctb.HotkeysMapping.Add(Keys.Z | Keys.Control, FCTBAction.Undo);

        // --- Клавиатура Zoom и Навигация ---
        fctb.HotkeysMapping.Add(Keys.Add | Keys.Control, FCTBAction.ZoomIn);
        fctb.HotkeysMapping.Add(Keys.Subtract | Keys.Control, FCTBAction.ZoomOut);
        fctb.HotkeysMapping.Add(Keys.OemMinus | Keys.Control, FCTBAction.NavigateBackward);

        // --- CTRL + SHIFT ---
        fctb.HotkeysMapping.Add(Keys.End | Keys.Control | Keys.Shift, FCTBAction.GoLastLineWithSelection);
        fctb.HotkeysMapping.Add(Keys.Home | Keys.Control | Keys.Shift, FCTBAction.GoFirstLineWithSelection);
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Control | Keys.Shift, FCTBAction.GoWordLeftWithSelection);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Control | Keys.Shift, FCTBAction.GoWordRightWithSelection);
        fctb.HotkeysMapping.Add(Keys.B | Keys.Control | Keys.Shift, FCTBAction.UnbookmarkLine);
        fctb.HotkeysMapping.Add(Keys.C | Keys.Control | Keys.Shift, FCTBAction.CommentSelected);
        fctb.HotkeysMapping.Add(Keys.N | Keys.Control | Keys.Shift, FCTBAction.GoPrevBookmark);
        fctb.HotkeysMapping.Add(Keys.U | Keys.Control | Keys.Shift, FCTBAction.LowerCase);
        fctb.HotkeysMapping.Add(Keys.OemMinus | Keys.Control | Keys.Shift, FCTBAction.NavigateForward);

        // --- ALT + Клавиши ---
        fctb.HotkeysMapping.Add(Keys.Back | Keys.Alt, FCTBAction.Undo); 

        fctb.HotkeysMapping.Add(Keys.Up | Keys.Alt, FCTBAction.MoveSelectedLinesUp);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Alt, FCTBAction.MoveSelectedLinesDown);
        fctb.HotkeysMapping.Add(Keys.F | Keys.Alt, FCTBAction.FindChar);

        // --- ALT + SHIFT (Колоночное выделение) ---
        fctb.HotkeysMapping.Add(Keys.Left | Keys.Alt | Keys.Shift, FCTBAction.GoLeft_ColumnSelectionMode);
        fctb.HotkeysMapping.Add(Keys.Up | Keys.Alt | Keys.Shift, FCTBAction.GoUp_ColumnSelectionMode);
        fctb.HotkeysMapping.Add(Keys.Right | Keys.Alt | Keys.Shift, FCTBAction.GoRight_ColumnSelectionMode);
        fctb.HotkeysMapping.Add(Keys.Down | Keys.Alt | Keys.Shift, FCTBAction.GoDown_ColumnSelectionMode);

        newTabPage.Controls.Add(fctb);
        tabControlEditor.TabPages.Add(newTabPage);
        tabControlEditor.SelectedTab = newTabPage;
        fctb.IsChanged = false;
        fctb.SelectionChanged += (s, ev) => {
            int line = fctb.Selection.Start.iLine + 1;
            lblStatus.Text = $"Строка: {line}| Всего строк: {fctb.LinesCount}";
        };
        fctb.TextChanged += OnTextChanged;
        return fctb;
    }
    private FastColoredTextBox CurrentEditor => 
        tabControlEditor.SelectedTab?.Controls[0] as FastColoredTextBox;
    
    private void DgvErrors_MouseWheel(object sender, MouseEventArgs e)
    {
        if (Control.ModifierKeys == Keys.Control)
        {
            float currentSize = dgvErrors.DefaultCellStyle.Font.Size;
            float newSize = e.Delta > 0 ? currentSize + 1 : currentSize - 1;

            if (newSize >= 6 && newSize <= 32)
            {
                Font newFont = new Font(dgvErrors.Font.FontFamily, newSize);

                dgvErrors.DefaultCellStyle.Font = newFont;

                dgvErrors.ColumnHeadersDefaultCellStyle.Font = newFont;

                dgvErrors.AutoResizeColumnHeadersHeight();
            
                dgvErrors.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            }

            ((HandledMouseEventArgs)e).Handled = true;
        }
    }
   private void ApplyLocalization()
    {
        // Заголовок окна
        this.Text = Label.AppTitle;
    
        // Меню Файл
        файлToolStripMenuItem.Text = Label.MenuFile;
        создатьToolStripMenuItem.Text = Label.MenuCreate;
        открытьToolStripMenuItem.Text = Label.MenuOpen;
        сохранитьToolStripMenuItem.Text = Label.MenuSave;
        сохранитьКакToolStripMenuItem.Text = Label.MenuSaveAs;
        выходToolStripMenuItem.Text = Label.MenuExit;

        // Меню Правка
        текстToolStripMenuItem.Text = Label.MenuEdit;
        отменитьToolStripMenuItem.Text = Label.MenuUndo;
        повторитToolStripMenuItem.Text = Label.MenuRedo;
        вырезатьToolStripMenuItem.Text = Label.MenuCut;
        копироватьToolStripMenuItem.Text = Label.MenuCopy;
        вставитьToolStripMenuItem.Text = Label.MenuPaste;
        удалитьToolStripMenuItem.Text = Label.MenuDelete;
        выделитьВсёToolStripMenuItem.Text = Label.MenuSelectAll;

        // Меню Текст
        текстToolStripMenuItem1.Text = Label.MenuText;
        постановкаЗадачиToolStripMenuItem.Text = Label.MenuTask;
        грамматикаToolStripMenuItem.Text = Label.MenuGrammar;
        классификацияГрамматикиToolStripMenuItem.Text = Label.MenuClass;
        методАнализаToolStripMenuItem.Text = Label.MenuMethod;
        тестовыйПримерToolStripMenuItem.Text = Label.MenuExample;
        списокЛитературыToolStripMenuItem.Text = Label.MenuDocs;
        исходныйКодПрограммыToolStripMenuItem.Text = Label.MenuSource;

        // Меню Пуск и Справка
        пускToolStripMenuItem.Text = Label.MenuRun;
        справкаToolStripMenuItem.Text = Label.MenuHelpRoot;
        вызовСправкиToolStripMenuItem.Text = Label.MenuShowHelp;
        оПрограммеToolStripMenuItem.Text = Label.MenuAbout;

        // Кнопки на панели (подсказки при наведении)
        toolStripButton1.Text = Label.MenuCreate;
        toolStripButton2.Text = Label.MenuOpen;
        toolStripButton3.Text = Label.MenuSave;
        toolStripButton10.Text = Label.MenuRun;
        toolStripButton11.Text = Label.MenuShowHelp;
        toolStripButton8.Text = Label.MenuAbout;

        // Таблица ошибок
        colFragment.HeaderText = Label.colFragment;
        colPos.HeaderText = Label.colPos;
        ColMessage.HeaderText = Label.ColMessage;
        // Кнопки на ToolStrip (текст всплывающих подсказок)
        toolStripButton4.Text = Label.MenuUndo;
        toolStripButton5.Text = Label.MenuRedo;
        toolStripButton6.Text = Label.MenuCopy;
        toolStripButton7.Text = Label.MenuCut;
        toolStripButton9.Text = Label.MenuPaste;

        // Меню выбора языка
        языкToolStripMenuItem.Text = Label.MenuLangRoot;
        русскийToolStripMenuItem.Text = Label.LangRu;
        английскийToolStripMenuItem.Text = Label.LangEn;
        
        // Начальный статус
        if (string.IsNullOrEmpty(lblStatus.Text)) 
            lblStatus.Text = Label.StatusReady;
    }
   
    private void englishToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeLanguage("en-US");
    }

    private void russianToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeLanguage("ru-RU");
    }
    private void ChangeLanguage(string cultureName)
    {
        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(cultureName);
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);

        ApplyLocalization();

        foreach (TabPage tab in tabControlEditor.TabPages)
        {
            if (tab.Tag != null && string.IsNullOrEmpty(tab.Tag.ToString()))
            {
                tab.Text = Label.NewFile; 
            }
        }
    }
    
    private void dgvErrors_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        // Проверяем, что клик был по строке и в Tag лежит наш результат поиска
        if (e.RowIndex >= 0 && dgvErrors.Rows[e.RowIndex].Tag is RegexMatchResult match)
        {
            if (CurrentEditor == null) return;

            // FastColoredTextBox использует индексацию с нуля
            int lineIndex = match.Line - 1;
            int charIndexInLine = match.StartPos - 1;

            try
            {
                FastColoredTextBoxNS.Place place = new FastColoredTextBoxNS.Place(charIndexInLine, lineIndex);
                CurrentEditor.Focus();
                CurrentEditor.SelectionStart = CurrentEditor.PlaceToPosition(place);
                CurrentEditor.SelectionLength = match.Length;
                CurrentEditor.DoSelectionVisible();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка навигации: " + ex.Message);
            }
        }
    }
    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        e.ChangedRange.ClearStyle(BlueStyle, BrownStyle);
        e.ChangedRange.SetStyle(BrownStyle, @"""""|@""[^""]*""|""[^""\\]*(?:\\.[^""\\]*)*""");
        e.ChangedRange.SetStyle(BlueStyle, @"\b(const|val)\b");
    }
}