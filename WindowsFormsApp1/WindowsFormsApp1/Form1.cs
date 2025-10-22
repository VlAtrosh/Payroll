using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;

namespace WindowsFormsApp1
{
   
    public enum CustomSortOrder
    {
        None,
        Ascending,
        Descending
    }

    public class SortSettings
    {
        public string ColumnName { get; set; }
        public CustomSortOrder SortOrder { get; set; }
        public string SortType { get; set; } // "simple", "multiple", "custom"
    }

    public partial class Form1 : Form
    {
        private string connectionString;
        private TabControl tabControl;
        private Dictionary<string, SortSettings> currentSortSettings = new Dictionary<string, SortSettings>();

        public Form1(string connString)
        {
            connectionString = connString;
            InitializeComponent();
            CreateInterface();
            try { UiTheme.ApplyTheme(this, UiTheme.Palettes.Light); } catch {}
        }

        public Form1() : this("") { }

        private void CreateInterface()
        {
            this.Text = "Система управления зарплатой - " + GetDatabaseName();
            this.Size = new System.Drawing.Size(1400, 800);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);

            // Добавляем вкладки с указанием названий таблиц для CRUD операций
            AddTab("Отделы", "Department", "SELECT * FROM Department");
            AddTab("Должности", "Position", "SELECT * FROM Position");
            AddTab("Сотрудники", "Employee",
    @"SELECT e.employee_id, e.full_name, e.hire_date, e.bank_account, 
             d.name as department_name, p.title as position_title,
             p.base_salary, e.phone -- ДОБАВЬТЕ ЭТОТ СТОЛБЕЦ
      FROM Employee e
      JOIN Department d ON e.department_id = d.department_id
      JOIN Position p ON e.position_id = p.position_id");
            AddTab("Расчётные периоды", "PayPeriod", "SELECT * FROM PayPeriod");
            AddTab("Расчёты зарплаты", "Payroll",
                @"SELECT py.payroll_id, e.full_name, pp.month, pp.year, 
                         py.calculation_date, py.total_earnings, 
                         py.total_deductions, py.net_payment
                  FROM Payroll py
                  JOIN Employee e ON py.employee_id = e.employee_id
                  JOIN PayPeriod pp ON py.period_id = pp.period_id");

            AddReconnectButton();
        }

        private string GetDatabaseName()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return connection.Database;
                }
            }
            catch
            {
                return "Неизвестная база";
            }
        }

        private void AddReconnectButton()
        {
            Button btnReconnect = new Button();
            btnReconnect.Text = "Сменить пользователя";
            btnReconnect.AutoSize = true;
            btnReconnect.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnReconnect.MinimumSize = new System.Drawing.Size(160, 34);
            btnReconnect.Location = new System.Drawing.Point(12, this.ClientSize.Height - 50);
            btnReconnect.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnReconnect.Click += ReconnectButton_Click;
            this.Controls.Add(btnReconnect);
            btnReconnect.BringToFront();
        }

        private void ReconnectButton_Click(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();

            if (loginForm.ShowDialog() == DialogResult.OK && loginForm.LoginSuccessful)
            {
                connectionString = loginForm.ConnectionString;
                this.Text = "Система управления зарплатой - " + GetDatabaseName();

                foreach (Control control in this.Controls)
                {
                    if (control is TabControl currentTabControl)
                    {
                        foreach (TabPage tabPage in currentTabControl.TabPages)
                        {
                            foreach (Control tabControl1 in tabPage.Controls)
                            {
                                if (tabControl1 is DataGridView dataGridView)
                                {
                                    foreach (Control btn in tabPage.Controls)
                                    {
                                        if (btn is Button button && button.Text == "Обновить данные")
                                        {
                                            button.PerformClick();
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddTab(string tabName, string tableName, string query)
        {
            TabPage tabPage = new TabPage(tabName);
            tabControl.Controls.Add(tabPage);

            // ОБНОВЛЯЕМ ЗАПРОС ДЛЯ PAYROLL ДО СОЗДАНИЯ ЭЛЕМЕНТОВ
            if (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT py.payroll_id, e.full_name, pp.month, pp.year, 
                         py.calculation_date, py.total_earnings, 
                         py.total_deductions, py.net_payment, py.status
                  FROM Payroll py
                  JOIN Employee e ON py.employee_id = e.employee_id
                  JOIN PayPeriod pp ON py.period_id = pp.period_id";
            }

            // Создаем DataGridView
            DataGridView dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView.ReadOnly = true;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Включаем базовую сортировку
            dataGridView.ColumnHeaderMouseClick += DataGridView_ColumnHeaderMouseClick;

            // Создаем панель с кнопками (FlowLayout для корректной раскладки)
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Height = 52;
            buttonPanel.Padding = new Padding(10, 6, 10, 6);
            buttonPanel.WrapContents = false;
            buttonPanel.AutoSize = false;
            buttonPanel.AutoScroll = true;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;

            // Кнопка обновления
            Button refreshButton = new Button()
            {
                Text = "Обновить данные",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            refreshButton.Margin = new Padding(0, 4, 8, 4);

            // Кнопка добавления
            Button addButton = new Button()
            {
                Text = "Добавить",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            addButton.Margin = new Padding(0, 4, 8, 4);

            // Кнопка редактирования
            Button editButton = new Button()
            {
                Text = "Изменить",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            editButton.Margin = new Padding(0, 4, 8, 4);

            // Кнопка удаления
            Button deleteButton = new Button()
            {
                Text = "Удалить",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            deleteButton.Margin = new Padding(0, 4, 12, 4);

            // Кнопка сброса сортировки
            Button resetSortButton = new Button()
            {
                Text = "Сбросить сортировку",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            resetSortButton.Margin = new Padding(0, 4, 8, 4);

            // Комбобокс для быстрой сортировки
            ComboBox quickSortComboBox = new ComboBox()
            {
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            quickSortComboBox.Margin = new Padding(0, 6, 8, 6);

            // Кнопка применения быстрой сортировки
            Button applyQuickSortButton = new Button()
            {
                Text = "Применить",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            applyQuickSortButton.Margin = new Padding(0, 4, 12, 4);

            // Поле поиска и кнопки
            TextBox searchTextBox = new TextBox()
            {
                Width = 220
            };
            searchTextBox.Margin = new Padding(0, 6, 8, 6);

            Button searchButton = new Button()
            {
                Text = "Найти",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            searchButton.Margin = new Padding(0, 4, 8, 4);

            Button clearSearchButton = new Button()
            {
                Text = "Сброс",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            clearSearchButton.Margin = new Padding(0, 4, 0, 4);

            // События поиска
            searchButton.Click += (s, e) => ApplySearchFilter(dataGridView, searchTextBox.Text);
            clearSearchButton.Click += (s, e) => { searchTextBox.Text = string.Empty; ApplySearchFilter(dataGridView, string.Empty); };
            searchTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { ApplySearchFilter(dataGridView, searchTextBox.Text); e.Handled = true; e.SuppressKeyPress = true; } };

            // Обработчики событий для кнопок
            refreshButton.Click += (s, e) => LoadDataToGrid(dataGridView, query, tabPage, tableName);
            addButton.Click += (s, e) => AddRecord(tableName, dataGridView, query, tabPage);
            editButton.Click += (s, e) => EditRecord(tableName, dataGridView, query, tabPage);
            deleteButton.Click += (s, e) => DeleteRecord(tableName, dataGridView, query, tabPage);
            resetSortButton.Click += (s, e) => ResetSorting(dataGridView, query, tabPage, tableName);
            applyQuickSortButton.Click += (s, e) => ApplyQuickSort(dataGridView, quickSortComboBox, tableName);

            // Заполняем комбобокс для быстрой сортировки
            FillQuickSortComboBox(quickSortComboBox, tableName);

            // Добавляем кнопки на панель
            buttonPanel.Controls.AddRange(new Control[] {
                refreshButton, addButton, editButton, deleteButton,
                resetSortButton, quickSortComboBox, applyQuickSortButton,
                searchTextBox, searchButton, clearSearchButton
            });

            // Добавляем элементы на вкладку
            tabPage.Controls.Add(dataGridView);
            tabPage.Controls.Add(buttonPanel);

            // Инициализируем настройки сортировки для этой вкладки
            currentSortSettings[tabName] = new SortSettings
            {
                SortOrder = CustomSortOrder.None,
                SortType = "simple"
            };

            // Загружаем данные
            LoadDataToGrid(dataGridView, query, tabPage, tableName);
        }
        private void FillQuickSortComboBox(ComboBox comboBox, string tableName)
        {
            comboBox.Items.Clear();

            // Добавляем предопределенные варианты сортировки в зависимости от таблицы
            switch (tableName.ToLower())
            {
                case "employee":
                    comboBox.Items.Add("По имени (А-Я)");
                    comboBox.Items.Add("По имени (Я-А)");
                    comboBox.Items.Add("По дате приема (новые first)");
                    comboBox.Items.Add("По дате приема (старые first)");
                    comboBox.Items.Add("По отделу и имени");
                    comboBox.Items.Add("По должности и зарплате");
                    break;
                case "department":
                    comboBox.Items.Add("По названию (А-Я)");
                    comboBox.Items.Add("По названию (Я-А)");
                    break;
                case "position":
                    comboBox.Items.Add("По должности (А-Я)");
                    comboBox.Items.Add("По должности (Я-А)");
                    comboBox.Items.Add("По зарплате (возрастание)");
                    comboBox.Items.Add("По зарплате (убывание)");
                    break;
                case "payroll":
                    comboBox.Items.Add("По дате расчета (новые first)");
                    comboBox.Items.Add("По дате расчета (старые first)");
                    comboBox.Items.Add("По сумме выплаты (возрастание)");
                    comboBox.Items.Add("По сумме выплаты (убывание)");
                    comboBox.Items.Add("По сотруднику и дате");
                    break;
                case "payperiod":
                    comboBox.Items.Add("По году и месяцу (возрастание)");
                    comboBox.Items.Add("По году и месяцу (убывание)");
                    break;
                default:
                    comboBox.Items.Add("Без сортировки");
                    break;
            }

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }

        private void DataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridView dataGridView = (DataGridView)sender;

            // Получаем DataTable из DataSource (может быть DataTable или DataView)
            DataTable dataTable = GetDataTableFromDataSource(dataGridView.DataSource);

            if (dataTable != null && e.ColumnIndex >= 0)
            {
                string columnName = dataGridView.Columns[e.ColumnIndex].DataPropertyName;
                DataView dataView = dataTable.DefaultView;

                // Определяем текущую вкладку
                string currentTab = ((TabPage)dataGridView.Parent).Text.Split('(')[0].Trim();

                if (currentSortSettings.ContainsKey(currentTab) &&
                    currentSortSettings[currentTab].ColumnName == columnName)
                {
                    // Переключаем порядок сортировки для той же колонки
                    if (currentSortSettings[currentTab].SortOrder == CustomSortOrder.Ascending)
                    {
                        dataView.Sort = columnName + " DESC";
                        currentSortSettings[currentTab].SortOrder = CustomSortOrder.Descending;
                    }
                    else
                    {
                        dataView.Sort = columnName + " ASC";
                        currentSortSettings[currentTab].SortOrder = CustomSortOrder.Ascending;
                    }
                }
                else
                {
                    // Новая колонка для сортировки
                    dataView.Sort = columnName + " ASC";
                    currentSortSettings[currentTab] = new SortSettings
                    {
                        ColumnName = columnName,
                        SortOrder = CustomSortOrder.Ascending,
                        SortType = "simple"
                    };
                }

                dataGridView.DataSource = dataView;
                UpdateSortIndicators(dataGridView);
            }
        }

        // Новый метод для безопасного получения DataTable из DataSource
        private DataTable GetDataTableFromDataSource(object dataSource)
        {
            if (dataSource == null)
                return null;

            if (dataSource is DataTable dataTable)
                return dataTable;

            if (dataSource is DataView dataView)
                return dataView.Table;

            return null;
        }

        private void UpdateSortIndicators(DataGridView dataGridView)
        {
            // Сбрасываем все индикаторы
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.HeaderCell.Style.Padding = new Padding(0);
                column.HeaderText = column.HeaderText.Replace(" ↑", "").Replace(" ↓", "");
            }

            // Устанавливаем индикатор для текущей колонки сортировки
            string currentTab = ((TabPage)dataGridView.Parent).Text.Split('(')[0].Trim();
            if (currentSortSettings.ContainsKey(currentTab) &&
                !string.IsNullOrEmpty(currentSortSettings[currentTab].ColumnName))
            {
                var sortSettings = currentSortSettings[currentTab];
                var sortedColumn = dataGridView.Columns
                    .Cast<DataGridViewColumn>()
                    .FirstOrDefault(c => c.DataPropertyName == sortSettings.ColumnName);

                if (sortedColumn != null)
                {
                    string indicator = sortSettings.SortOrder == CustomSortOrder.Ascending ? " ↑" : " ↓";
                    sortedColumn.HeaderText = sortedColumn.HeaderText.Replace(" ↑", "").Replace(" ↓", "") + indicator;
                }
            }
        }

        private void ApplyQuickSort(DataGridView dataGridView, ComboBox sortComboBox, string tableName)
        {
            if (sortComboBox.SelectedItem == null) return;

            string selectedSort = sortComboBox.SelectedItem.ToString();
            DataTable dataTable = GetDataTableFromDataSource(dataGridView.DataSource);

            if (dataTable == null) return;

            string sortExpression = "";

            switch (tableName.ToLower())
            {
                case "employee":
                    sortExpression = GetEmployeeSortExpression(selectedSort);
                    break;
                case "department":
                    sortExpression = GetDepartmentSortExpression(selectedSort);
                    break;
                case "position":
                    sortExpression = GetPositionSortExpression(selectedSort);
                    break;
                case "payroll":
                    sortExpression = GetPayrollSortExpression(selectedSort);
                    break;
                case "payperiod":
                    sortExpression = GetPayPeriodSortExpression(selectedSort);
                    break;
            }

            if (!string.IsNullOrEmpty(sortExpression))
            {
                try
                {
                    dataTable.DefaultView.Sort = sortExpression;
                    dataGridView.DataSource = dataTable.DefaultView;

                    // Обновляем настройки сортировки
                    string currentTab = ((TabPage)dataGridView.Parent).Text.Split('(')[0].Trim();
                    currentSortSettings[currentTab] = new SortSettings
                    {
                        SortType = "custom",
                        ColumnName = "custom"
                    };

                    UpdateSortIndicators(dataGridView);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сортировке: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string GetEmployeeSortExpression(string sortType)
        {
            switch (sortType)
            {
                case "По имени (А-Я)": return "full_name ASC";
                case "По имени (Я-А)": return "full_name DESC";
                case "По дате приема (новые first)": return "hire_date DESC";
                case "По дате приема (старые first)": return "hire_date ASC";
                case "По отделу и имени": return "department_name ASC, full_name ASC";
                case "По должности и зарплате": return "position_title ASC, base_salary DESC";
                default: return "";
            }
        }

        private string GetDepartmentSortExpression(string sortType)
        {
            switch (sortType)
            {
                case "По названию (А-Я)": return "name ASC";
                case "По названию (Я-А)": return "name DESC";
                default: return "";
            }
        }

        private string GetPositionSortExpression(string sortType)
        {
            switch (sortType)
            {
                case "По должности (А-Я)": return "title ASC";
                case "По должности (Я-А)": return "title DESC";
                case "По зарплате (возрастание)": return "base_salary ASC";
                case "По зарплате (убывание)": return "base_salary DESC";
                default: return "";
            }
        }

        private string GetPayrollSortExpression(string sortType)
        {
            switch (sortType)
            {
                case "По дате расчета (новые first)": return "calculation_date DESC";
                case "По дате расчета (старые first)": return "calculation_date ASC";
                case "По сумме выплаты (возрастание)": return "net_payment ASC";
                case "По сумме выплаты (убывание)": return "net_payment DESC";
                case "По сотруднику и дате": return "full_name ASC, calculation_date DESC";
                default: return "";
            }
        }

        private string GetPayPeriodSortExpression(string sortType)
        {
            switch (sortType)
            {
                case "По году и месяцу (возрастание)": return "year ASC, month ASC";
                case "По году и месяцу (убывание)": return "year DESC, month DESC";
                default: return "";
            }
        }

        private void ResetSorting(DataGridView dataGridView, string query, TabPage tabPage, string tableName)
        {
            // Перезагружаем данные без сортировки
            LoadDataToGrid(dataGridView, query, tabPage, tableName);

            // Сбрасываем настройки сортировки
            string currentTab = tabPage.Text.Split('(')[0].Trim();
            currentSortSettings[currentTab] = new SortSettings
            {
                SortOrder = CustomSortOrder.None,
                SortType = "simple"
            };

            UpdateSortIndicators(dataGridView);
        }

        private void LoadDataToGrid(DataGridView dataGridView, string query, TabPage tabPage, string tableName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dataGridView.DataSource = table;
                    dataGridView.Tag = table; // сохраняем оригинальные данные для поиска

                    // Проверяем и добавляем недостающие столбцы ID
                    string idColumn = GetIdColumnName(tableName);
                    if (!table.Columns.Contains(idColumn) && dataGridView.Columns[idColumn] == null)
                    {
                        // Добавляем скрытый столбец для ID если нужно
                    }

                    // Обновляем заголовок вкладки с количеством записей
                    string originalName = tabPage.Text.Replace($" ({table.Rows.Count})", "").Split('(')[0].Trim();
                    tabPage.Text = $"{originalName} ({table.Rows.Count})";

                    // Обновляем индикаторы сортировки
                    UpdateSortIndicators(dataGridView);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySearchFilter(DataGridView grid, string search)
        {
            if (grid?.DataSource == null) return;
            if (!(grid.Tag is DataTable sourceTable)) return;

            if (string.IsNullOrWhiteSpace(search))
            {
                grid.DataSource = sourceTable;
                return;
            }

            string term = search.Trim().Replace("'", "''");
            // строим фильтр по всем видимым строковым/числовым столбцам
            var likeParts = new List<string>();
            foreach (DataColumn col in sourceTable.Columns)
            {
                if (col.DataType == typeof(string))
                    likeParts.Add($"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{term}%'");
                else if (col.DataType == typeof(int) || col.DataType == typeof(decimal))
                    likeParts.Add($"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{term}%'");
            }

            var view = new DataView(sourceTable);
            view.RowFilter = likeParts.Count > 0 ? string.Join(" OR ", likeParts) : string.Empty;
            grid.DataSource = view;
        }

        // РЕАЛЬНЫЕ МЕТОДЫ CRUD ОПЕРАЦИЙ
        private void AddRecord(string tableName, DataGridView dataGridView, string query, TabPage tabPage)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем структуру таблицы
                    SqlCommand command = new SqlCommand($"SELECT TOP 0 * FROM {tableName}", connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable schemaTable = new DataTable();
                    adapter.Fill(schemaTable);

                    // ОСОБАЯ ЛОГИКА ДЛЯ СОТРУДНИКОВ - получаем данные для ComboBox
                    Dictionary<string, DataTable> lookupData = new Dictionary<string, DataTable>();
                    if (tableName.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                    {
                        // Получаем список отделов
                        SqlCommand deptCommand = new SqlCommand("SELECT department_id, name FROM Department", connection);
                        SqlDataAdapter deptAdapter = new SqlDataAdapter(deptCommand);
                        DataTable deptTable = new DataTable();
                        deptAdapter.Fill(deptTable);
                        lookupData["department_id"] = deptTable;

                        // Получаем список должностей
                        SqlCommand posCommand = new SqlCommand("SELECT position_id, title FROM Position", connection);
                        SqlDataAdapter posAdapter = new SqlDataAdapter(posCommand);
                        DataTable posTable = new DataTable();
                        posAdapter.Fill(posTable);
                        lookupData["position_id"] = posTable;
                    }
                    // ДОБАВЛЯЕМ ЛОГИКУ ДЛЯ PAYROLL
                    else if (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase))
                    {
                        // Получаем список сотрудников
                        SqlCommand empCommand = new SqlCommand("SELECT employee_id, full_name FROM Employee", connection);
                        SqlDataAdapter empAdapter = new SqlDataAdapter(empCommand);
                        DataTable empTable = new DataTable();
                        empAdapter.Fill(empTable);
                        lookupData["employee_id"] = empTable;

                        // Получаем список расчетных периодов
                        SqlCommand periodCommand = new SqlCommand("SELECT period_id, CONCAT(month, '/', year) as period FROM PayPeriod", connection);
                        SqlDataAdapter periodAdapter = new SqlDataAdapter(periodCommand);
                        DataTable periodTable = new DataTable();
                        periodAdapter.Fill(periodTable);
                        lookupData["period_id"] = periodTable;
                    }

                    // Создаем форму для ввода данных
                    using (Form inputForm = new Form())
                    {
                        inputForm.Text = $"Добавить запись в {tableName}";
                        inputForm.Size = new System.Drawing.Size(400, 500);
                        inputForm.StartPosition = FormStartPosition.CenterParent;
                        inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                        FlowLayoutPanel panel = new FlowLayoutPanel();
                        panel.Dock = DockStyle.Fill;
                        panel.FlowDirection = FlowDirection.TopDown;
                        panel.WrapContents = false;
                        panel.AutoScroll = true;

                        Dictionary<string, Control> inputControls = new Dictionary<string, Control>();

                        // Создаем поля для ввода для каждого столбца
                        foreach (DataColumn column in schemaTable.Columns)
                        {
                            if (column.ColumnName.ToLower().EndsWith("_id") && column.DataType == typeof(int))
                            {
                                // ОСОБАЯ ОБРАБОТКА ДЛЯ FOREIGN KEY (department_id, position_id, employee_id, period_id)
                                if (lookupData.ContainsKey(column.ColumnName))
                                {
                                    Label labelDept = new Label()
                                    {
                                        Text = GetRussianColumnName(column.ColumnName),
                                        Width = 180,
                                        Margin = new Padding(3, 10, 3, 3)
                                    };

                                    ComboBox comboBox = new ComboBox();
                                    comboBox.Width = 190;
                                    comboBox.Margin = new Padding(3, 3, 3, 10);
                                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                                    comboBox.DisplayMember = GetDisplayMember(column.ColumnName);
                                    comboBox.ValueMember = GetValueMember(column.ColumnName);

                                    // Заполняем данными
                                    DataTable lookupTable = lookupData[column.ColumnName];
                                    comboBox.DataSource = lookupTable;

                                    // Добавляем обязательную валидацию
                                    comboBox.Validating += (s, e) =>
                                    {
                                        if (comboBox.SelectedItem == null)
                                        {
                                            MessageBox.Show($"Поле '{GetRussianColumnName(column.ColumnName)}' обязательно для заполнения!", "Ошибка",
                                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            comboBox.Focus();
                                            e.Cancel = true;
                                        }
                                    };

                                    inputControls[column.ColumnName] = comboBox;
                                    panel.Controls.Add(labelDept);
                                    panel.Controls.Add(comboBox);
                                }
                                continue; // Пропускаем обычные ID поля
                            }

                            Label labelField = new Label()
                            {
                                Text = GetRussianColumnName(column.ColumnName),
                                Width = 180,
                                Margin = new Padding(3, 10, 3, 3)
                            };

                            Control inputControl = null;

                            // ОСОБАЯ ОБРАБОТКА ДЛЯ СТАТУСА PAYPERIOD И PAYROLL
                            if ((tableName.Equals("PayPeriod", StringComparison.OrdinalIgnoreCase) ||
                                 tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase)) &&
                                column.ColumnName.Equals("status", StringComparison.OrdinalIgnoreCase))
                            {
                                ComboBox comboBox = new ComboBox();
                                comboBox.Width = 190;
                                comboBox.Margin = new Padding(3, 3, 3, 10);
                                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                                if (tableName.Equals("PayPeriod", StringComparison.OrdinalIgnoreCase))
                                {
                                    comboBox.Items.Add("Open");
                                    comboBox.Items.Add("Closed");
                                    comboBox.Items.Add("Processing");
                                }
                                else if (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase))
                                {
                                    comboBox.Items.Add("Draft");
                                    comboBox.Items.Add("Calculated");
                                    comboBox.Items.Add("Approved");
                                    comboBox.Items.Add("Paid");
                                    comboBox.Items.Add("Cancelled");
                                }

                                comboBox.SelectedIndex = 0;
                                inputControl = comboBox;


                            }
                            // ВАЛИДАЦИЯ ДЛЯ МЕСЯЦА И ГОДА В PAYPERIOD
                            else if (tableName.Equals("PayPeriod", StringComparison.OrdinalIgnoreCase) &&
                                     column.DataType == typeof(int) &&
                                     (column.ColumnName.Equals("month", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("year", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки поля и подсказки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                NumericUpDown numericUpDown = new NumericUpDown()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5
                                };

                                Label hintLabel = new Label()
                                {
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                if (column.ColumnName.Equals("month", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Настройки для месяца
                                    numericUpDown.Minimum = 1;
                                    numericUpDown.Maximum = 12;
                                    numericUpDown.Value = DateTime.Now.Month;
                                    hintLabel.Text = "Выберите месяц от 1 до 12";
                                }
                                else if (column.ColumnName.Equals("year", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Настройки для года
                                    int currentYear = DateTime.Now.Year;
                                    numericUpDown.Minimum = currentYear - 5;
                                    numericUpDown.Maximum = currentYear + 5;
                                    numericUpDown.Value = currentYear;
                                    hintLabel.Text = $"Выберите год от {currentYear - 5} до {currentYear + 5}";
                                }

                                fieldPanel.Controls.Add(numericUpDown);
                                fieldPanel.Controls.Add(hintLabel);

                                inputControl = numericUpDown;
                                // Регистрируем контрол для последующей сборки параметров
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                // Добавляем метку и панель с полем
                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);

                                // Пропускаем стандартное добавление inputControl
                                continue;
                            }
                            // ВАЛИДАЦИЯ ТЕЛЕФОНА
                            else if (column.DataType == typeof(string) &&
                                     (column.ColumnName.Equals("phone", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("phone_number", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("telephone", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("mobile_phone", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("contact_phone", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                MaskedTextBox maskedTextBox = new MaskedTextBox()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5,
                                    Mask = "+7 (000) 000-00-00",
                                    PromptChar = ' '
                                };

                                Label phoneHint = new Label()
                                {
                                    Text = "Формат: +7 (912) 345-67-89",
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                // Добавляем валидацию для номера телефона
                                maskedTextBox.Validating += (s, e) =>
                                {
                                    string phoneValue = maskedTextBox.Text.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");
                                    if (!string.IsNullOrEmpty(phoneValue) && !IsValidPhoneNumber(maskedTextBox.Text))
                                    {
                                        MessageBox.Show("Некорректный номер телефона! Заполните все цифры.", "Ошибка",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        maskedTextBox.Focus();
                                        e.Cancel = true;
                                    }
                                };

                                fieldPanel.Controls.Add(maskedTextBox);
                                fieldPanel.Controls.Add(phoneHint);

                                inputControl = maskedTextBox;
                                // Регистрируем контрол
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);
                                continue;
                            }
                            // ВАЛИДАЦИЯ ДЛЯ ЗАРПЛАТЫ
                            else if (column.DataType == typeof(decimal) &&
                                     (column.ColumnName.Equals("base_salary", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("salary", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("total_earnings", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("net_payment", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("total_deductions", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5,
                                    Text = "0"
                                };

                                Label salaryHint = new Label()
                                {
                                    Text = "Введите сумму в рублях",
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                // Добавляем валидацию для зарплаты
                                textBox.Validating += (s, e) =>
                                {
                                    if (!string.IsNullOrEmpty(textBox.Text))
                                    {
                                        string decimalValue = textBox.Text.Replace(',', '.');
                                        if (decimal.TryParse(decimalValue, System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture, out decimal salary))
                                        {
                                            if (salary < 0)
                                            {
                                                MessageBox.Show("Сумма не может быть отрицательной!", "Ошибка",
                                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                textBox.Focus();
                                                e.Cancel = true;
                                            }
                                            else if (salary > 1000000)
                                            {
                                                var result = MessageBox.Show($"Вы указали очень высокую сумму: {salary:N2}₽\nВы уверены, что это правильно?",
                                                                           "Подтверждение",
                                                                           MessageBoxButtons.YesNo,
                                                                           MessageBoxIcon.Question);
                                                if (result == DialogResult.No)
                                                {
                                                    textBox.Focus();
                                                    e.Cancel = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("Неверный формат числа!", "Ошибка",
                                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            textBox.Focus();
                                            e.Cancel = true;
                                        }
                                    }
                                };

                                fieldPanel.Controls.Add(textBox);
                                fieldPanel.Controls.Add(salaryHint);

                                inputControl = textBox;
                                // Регистрируем контрол
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);
                                continue;
                            }
                            // ВАЛИДАЦИЯ ДЛЯ EMAIL
                            else if (column.DataType == typeof(string) &&
                                     (column.ColumnName.Equals("email", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("email_address", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5
                                };

                                Label emailHint = new Label()
                                {
                                    Text = "Формат: example@mail.ru",
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                // Добавляем валидацию для email
                                textBox.Validating += (s, e) =>
                                {
                                    if (!string.IsNullOrEmpty(textBox.Text) && !IsValidEmail(textBox.Text))
                                    {
                                        MessageBox.Show("Некорректный email адрес! Пример: example@mail.ru", "Ошибка",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        textBox.Focus();
                                        e.Cancel = true;
                                    }
                                };

                                fieldPanel.Controls.Add(textBox);
                                fieldPanel.Controls.Add(emailHint);

                                inputControl = textBox;
                                // Регистрируем контрол
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);
                                continue;
                            }
                            else if (column.DataType == typeof(DateTime))
                            {
                                DateTimePicker datePicker = new DateTimePicker()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10),
                                    Value = DateTime.Today
                                };

                                // ДОБАВЛЯЕМ ВАЛИДАЦИЮ ДЛЯ ДАТ
                                if (column.ColumnName.Equals("end_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Для end_date добавляем подсказку
                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 65;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    Label dateHint = new Label()
                                    {
                                        Text = "Дата окончания не может быть раньше даты начала",
                                        Font = new Font("Arial", 7),
                                        ForeColor = Color.Gray,
                                        Width = 180,
                                        Top = 25,
                                        Left = 5,
                                        Height = 30
                                    };

                                    fieldPanel.Controls.Add(datePicker);
                                    fieldPanel.Controls.Add(dateHint);

                                    inputControl = datePicker;
                                    // Регистрируем контрол
                                    inputControl.Name = column.ColumnName;
                                    inputControls[column.ColumnName] = inputControl;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
                                else if (column.ColumnName.Equals("start_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Для start_date добавляем подсказку
                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 65;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    Label dateHint = new Label()
                                    {
                                        Text = "Дата начала периода",
                                        Font = new Font("Arial", 7),
                                        ForeColor = Color.Gray,
                                        Width = 180,
                                        Top = 25,
                                        Left = 5,
                                        Height = 30
                                    };

                                    fieldPanel.Controls.Add(datePicker);
                                    fieldPanel.Controls.Add(dateHint);

                                    inputControl = datePicker;
                                    // Регистрируем контрол
                                    inputControl.Name = column.ColumnName;
                                    inputControls[column.ColumnName] = inputControl;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
				else if (column.ColumnName.Equals("created_date", StringComparison.OrdinalIgnoreCase) ||
						 column.ColumnName.Equals("calculation_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Для created_date и calculation_date ограничиваем диапазон
                                    datePicker.MinDate = DateTime.Today.AddYears(-5);
                                    datePicker.MaxDate = DateTime.Today.AddDays(1); // Можно установить на завтра

                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 65;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    Label dateHint = new Label()
                                    {
                                        Text = $"Диапазон: {datePicker.MinDate:dd.MM.yyyy} - {datePicker.MaxDate:dd.MM.yyyy}",
                                        Font = new Font("Arial", 7),
                                        ForeColor = Color.Gray,
                                        Width = 180,
                                        Top = 25,
                                        Left = 5,
                                        Height = 30
                                    };

                                    fieldPanel.Controls.Add(datePicker);
                                    fieldPanel.Controls.Add(dateHint);

                                    inputControl = datePicker;
                                    // Регистрируем контрол
                                    inputControl.Name = column.ColumnName;
                                    inputControls[column.ColumnName] = inputControl;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
				else if (column.ColumnName.Equals("created_at", StringComparison.OrdinalIgnoreCase))
				{
					// Для created_at запрещаем прошлые даты
					datePicker.MinDate = DateTime.Today;
					datePicker.Value = DateTime.Today;
					datePicker.Format = DateTimePickerFormat.Custom;
					datePicker.CustomFormat = "dd.MM.yyyy"; // только дата, без времени
					datePicker.Format = DateTimePickerFormat.Custom;
					datePicker.CustomFormat = "dd.MM.yyyy"; // только дата, без времени

					Panel fieldPanel = new Panel();
					fieldPanel.Width = 200;
					fieldPanel.Height = 40;
					fieldPanel.Margin = new Padding(3, 3, 3, 10);

					datePicker.Top = 0;
					datePicker.Left = 5;
					datePicker.Width = 190;

					fieldPanel.Controls.Add(datePicker);
					inputControl = datePicker;

					panel.Controls.Add(labelField);
					panel.Controls.Add(fieldPanel);
					continue;
				}
                                else
                                {
                                    // Для остальных дат стандартная обработка
                                    inputControl = datePicker;
                                }
                            }
                            else if (column.DataType == typeof(bool))
                            {
                                inputControl = new CheckBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                            }
                            else if (column.DataType == typeof(string) && column.ColumnName.Equals("bank_account", StringComparison.OrdinalIgnoreCase))
                            {
                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };

                                // Добавляем валидацию для банковского счета
                                textBox.Validating += (s, e) =>
                                {
                                    if (!string.IsNullOrEmpty(textBox.Text) && !IsValidBankAccount(textBox.Text))
                                    {
                                        MessageBox.Show("Некорректный номер банковского счета! Должен содержать только цифры и быть длиной 20 символов.", "Ошибка",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        textBox.Focus();
                                        e.Cancel = true;
                                    }
                                };

                                inputControl = textBox;
                            }
                            else
                            {
                                inputControl = new TextBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                            }

                            // Стандартное добавление для полей без специальной обработки
                            if (inputControl != null)
                            {
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(inputControl);
                            }
                        }

                        Button btnSave = new Button()
                        {
                            Text = "Сохранить",
                            Width = 100,
                            Margin = new Padding(3, 20, 3, 3)
                        };
                        Button btnCancel = new Button()
                        {
                            Text = "Отмена",
                            Width = 100,
                            Margin = new Padding(3, 3, 3, 10)
                        };

                        // ДИНАМИЧЕСКАЯ ПРОВЕРКА ДАТ
                        DateTimePicker startDatePicker = null;
                        DateTimePicker endDatePicker = null;

                        foreach (var control in inputControls)
                        {
                            if (control.Value is DateTimePicker datePicker)
                            {
                                if (control.Key.Equals("start_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    startDatePicker = datePicker;
                                }
                                else if (control.Key.Equals("end_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    endDatePicker = datePicker;
                                }
                            }
                        }

                        // ОБРАБОТЧИК СОХРАНЕНИЯ
                        btnSave.Click += (s, e) =>
                        {
                            try
                            {
                                // 1. Специальная проверка уникальности для Payroll (employee_id + period_id)
                                if (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase))
                                {
                                    int? employeeId = null;
                                    int? periodId = null;

                                    if (inputControls.TryGetValue("employee_id", out Control empCtrl) && empCtrl is ComboBox empCb && empCb.SelectedValue != null)
                                        employeeId = Convert.ToInt32(empCb.SelectedValue);

                                    if (inputControls.TryGetValue("period_id", out Control perCtrl) && perCtrl is ComboBox perCb && perCb.SelectedValue != null)
                                        periodId = Convert.ToInt32(perCb.SelectedValue);

                                    if (employeeId.HasValue && periodId.HasValue)
                                    {
                                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Payroll WHERE employee_id=@e AND period_id=@p", connection))
                                        {
                                            checkCmd.Parameters.AddWithValue("@e", employeeId.Value);
                                            checkCmd.Parameters.AddWithValue("@p", periodId.Value);
                                            int exists = (int)checkCmd.ExecuteScalar();
                                            if (exists > 0)
                                            {
                                                MessageBox.Show("Для выбранного сотрудника уже существует расчёт за этот период.", "Дублирование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                return;
                                            }
                                        }
                                    }
                                }

                                // Сбор параметров и сохранение

                                string columns = "";
                                string paramNames = "";
                                List<SqlParameter> parameters = new List<SqlParameter>();

                                // Собираем параметры
                                foreach (var control in inputControls)
                                {
                                    string columnName = control.Key;
                                    Control inputControl = control.Value;

                                    if (columns != "") columns += ", ";
                                    if (paramNames != "") paramNames += ", ";

                                    columns += columnName;
                                    paramNames += "@" + columnName;

                                    var col = schemaTable.Columns[columnName];
                                    var p = CreateParameter(columnName, inputControl, col?.DataType);
                                    if (p != null) parameters.Add(p);
                                }

                                // ВЫПОЛНЯЕМ ЗАПРОС С ПАРАМЕТРАМИ
                                string insertQuery = $"INSERT INTO {tableName} ({columns}) VALUES ({paramNames})";
                                SqlCommand insertCommand = new SqlCommand(insertQuery, connection);

                                // ДОБАВЛЯЕМ ВСЕ ПАРАМЕТРЫ
                                insertCommand.Parameters.AddRange(parameters.ToArray());

                                int rowsAffected = insertCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Запись успешно добавлена!", "Успех",
                                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    inputForm.DialogResult = DialogResult.OK;
                                    inputForm.Close();
                                    LoadDataToGrid(dataGridView, query, tabPage, tableName);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };

                        btnCancel.Click += (s, e) =>
                        {
                            inputForm.DialogResult = DialogResult.Cancel;
                            inputForm.Close();
                        };

                        panel.Controls.Add(btnSave);
                        panel.Controls.Add(btnCancel);
                        inputForm.Controls.Add(panel);

                        if (inputForm.ShowDialog() == DialogResult.OK)
                        {
                            LoadDataToGrid(dataGridView, query, tabPage, tableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditRecord(string tableName, DataGridView dataGridView, string query, TabPage tabPage)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для редактирования!", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
                string idColumn = GetIdColumnName(tableName);
                object idValue = selectedRow.Cells[idColumn].Value;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем данные выбранной записи
                    SqlCommand command = new SqlCommand($"SELECT * FROM {tableName} WHERE {idColumn} = @id", connection);
                    command.Parameters.AddWithValue("@id", idValue);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 0)
                    {
                        MessageBox.Show("Запись не найдена!", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    DataRow row = dataTable.Rows[0];

                    // ОСОБАЯ ЛОГИКА ДЛЯ СОТРУДНИКОВ И PAYROLL - получаем данные для ComboBox
                    Dictionary<string, DataTable> lookupData = new Dictionary<string, DataTable>();

                    // ДЛЯ СОТРУДНИКОВ
                    if (tableName.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                    {
                        // Получаем список отделов
                        SqlCommand deptCommand = new SqlCommand("SELECT department_id, name FROM Department", connection);
                        SqlDataAdapter deptAdapter = new SqlDataAdapter(deptCommand);
                        DataTable deptTable = new DataTable();
                        deptAdapter.Fill(deptTable);
                        lookupData["department_id"] = deptTable;

                        // Получаем список должностей
                        SqlCommand posCommand = new SqlCommand("SELECT position_id, title FROM Position", connection);
                        SqlDataAdapter posAdapter = new SqlDataAdapter(posCommand);
                        DataTable posTable = new DataTable();
                        posAdapter.Fill(posTable);
                        lookupData["position_id"] = posTable;
                    }
                    // ДЛЯ PAYROLL - получаем сотрудников и периоды
                    else if (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase))
                    {
                        // Получаем список сотрудников
                        SqlCommand empCommand = new SqlCommand("SELECT employee_id, full_name FROM Employee", connection);
                        SqlDataAdapter empAdapter = new SqlDataAdapter(empCommand);
                        DataTable empTable = new DataTable();
                        empAdapter.Fill(empTable);
                        lookupData["employee_id"] = empTable;

                        // Получаем список расчетных периодов
                        SqlCommand periodCommand = new SqlCommand("SELECT period_id, CONCAT(month, '/', year) as period FROM PayPeriod", connection);
                        SqlDataAdapter periodAdapter = new SqlDataAdapter(periodCommand);
                        DataTable periodTable = new DataTable();
                        periodAdapter.Fill(periodTable);
                        lookupData["period_id"] = periodTable;
                    }

                    // Создаем форму для редактирования
                    using (Form editForm = new Form())
                    {
                        editForm.Text = $"Редактировать запись в {tableName}";
                        editForm.Size = new System.Drawing.Size(400, 500);
                        editForm.StartPosition = FormStartPosition.CenterParent;
                        editForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                        FlowLayoutPanel panel = new FlowLayoutPanel();
                        panel.Dock = DockStyle.Fill;
                        panel.FlowDirection = FlowDirection.TopDown;
                        panel.WrapContents = false;
                        panel.AutoScroll = true;

                        Dictionary<string, Control> inputControls = new Dictionary<string, Control>();

                        // Создаем поля для редактирования
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            if (column.ColumnName == idColumn) continue;

                            Label labelField = new Label()
                            {
                                Text = GetRussianColumnName(column.ColumnName),
                                Width = 180,
                                Margin = new Padding(3, 10, 3, 3)
                            };

                            Control inputControl = null;

                            // ОСОБАЯ ОБРАБОТКА ДЛЯ FOREIGN KEY ПОЛЕЙ (department_id, position_id, employee_id, period_id)
                            if ((tableName.Equals("Employee", StringComparison.OrdinalIgnoreCase) &&
                                 (column.ColumnName.Equals("department_id", StringComparison.OrdinalIgnoreCase) ||
                                  column.ColumnName.Equals("position_id", StringComparison.OrdinalIgnoreCase))) ||
                                (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase) &&
                                 (column.ColumnName.Equals("employee_id", StringComparison.OrdinalIgnoreCase) ||
                                  column.ColumnName.Equals("period_id", StringComparison.OrdinalIgnoreCase))) &&
                                lookupData.ContainsKey(column.ColumnName))
                            {
                                ComboBox comboBox = new ComboBox();
                                comboBox.Width = 190;
                                comboBox.Margin = new Padding(3, 3, 3, 10);
                                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                                comboBox.DisplayMember = GetDisplayMember(column.ColumnName);
                                comboBox.ValueMember = GetValueMember(column.ColumnName);

                                // Заполняем данными
                                DataTable lookupTable = lookupData[column.ColumnName];
                                comboBox.DataSource = lookupTable;

                                // Устанавливаем текущее значение
                                if (row[column] != DBNull.Value)
                                {
                                    int currentId = Convert.ToInt32(row[column]);
                                    foreach (DataRowView item in comboBox.Items)
                                    {
                                        if (Convert.ToInt32(item.Row[GetValueMember(column.ColumnName)]) == currentId)
                                        {
                                            comboBox.SelectedItem = item;
                                            break;
                                        }
                                    }
                                }

                                inputControl = comboBox;
                            }
                            // ОСОБАЯ ОБРАБОТКА ДЛЯ СТАТУСА PAYPERIOD И PAYROLL
                            else if ((tableName.Equals("PayPeriod", StringComparison.OrdinalIgnoreCase) ||
                                      tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase)) &&
                                     column.ColumnName.Equals("status", StringComparison.OrdinalIgnoreCase))
                            {
                                ComboBox comboBox = new ComboBox();
                                comboBox.Width = 190;
                                comboBox.Margin = new Padding(3, 3, 3, 10);
                                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                                // ИСПОЛЬЗУЕМ GetAllowedStatusValues ДЛЯ ПОЛУЧЕНИЯ ДОПУСТИМЫХ ЗНАЧЕНИЙ
                                var allowedStatuses = GetAllowedStatusValues(tableName, column.ColumnName);
                                foreach (var status in allowedStatuses)
                                {
                                    comboBox.Items.Add(status);
                                }

                                if (row[column] != DBNull.Value)
                                    comboBox.SelectedItem = row[column].ToString();
                                else
                                    comboBox.SelectedIndex = 0;

                                inputControl = comboBox;
                            }
                            // ВАЛИДАЦИЯ ДЛЯ МЕСЯЦА И ГОДА В PAYPERIOD
                            else if (tableName.Equals("PayPeriod", StringComparison.OrdinalIgnoreCase) &&
                                     column.DataType == typeof(int) &&
                                     (column.ColumnName.Equals("month", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("year", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки поля и подсказки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                NumericUpDown numericUpDown = new NumericUpDown()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5
                                };

                                Label hintLabel = new Label()
                                {
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                if (column.ColumnName.Equals("month", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Настройки для месяца
                                    numericUpDown.Minimum = 1;
                                    numericUpDown.Maximum = 12;
                                    numericUpDown.Value = row[column] != DBNull.Value ? Convert.ToDecimal(row[column]) : DateTime.Now.Month;
                                    hintLabel.Text = "Выберите месяц от 1 до 12";
                                }
                                else if (column.ColumnName.Equals("year", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Настройки для года
                                    int currentYear = DateTime.Now.Year;
                                    numericUpDown.Minimum = currentYear - 5;
                                    numericUpDown.Maximum = currentYear + 5;
                                    numericUpDown.Value = row[column] != DBNull.Value ? Convert.ToDecimal(row[column]) : currentYear;
                                    hintLabel.Text = $"Выберите год от {currentYear - 5} до {currentYear + 5}";
                                }

                                fieldPanel.Controls.Add(numericUpDown);
                                fieldPanel.Controls.Add(hintLabel);

                                inputControl = numericUpDown;
                                // Регистрируем контрол для сохранения
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                // Добавляем метку и панель с полем
                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);

                                // Пропускаем стандартное добавление inputControl
                                continue;
                            }
                            // ВАЛИДАЦИЯ ТЕЛЕФОНА
                            else if (column.DataType == typeof(string) &&
                                     (column.ColumnName.Equals("phone", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("phone_number", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("telephone", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("mobile_phone", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("contact_phone", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                MaskedTextBox maskedTextBox = new MaskedTextBox()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5,
                                    Mask = "+7 (000) 000-00-00",
                                    PromptChar = ' '
                                };

                                if (row[column] != DBNull.Value)
                                {
                                    string phone = row[column].ToString();
                                    if (!string.IsNullOrEmpty(phone))
                                    {
                                        string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
                                        if (digitsOnly.Length >= 10)
                                        {
                                            if (digitsOnly.StartsWith("7") && digitsOnly.Length == 11)
                                                digitsOnly = digitsOnly.Substring(1);
                                            else if (digitsOnly.StartsWith("8") && digitsOnly.Length == 11)
                                                digitsOnly = digitsOnly.Substring(1);

                                            if (digitsOnly.Length == 10)
                                            {
                                                maskedTextBox.Text = digitsOnly;
                                            }
                                        }
                                    }
                                }

                                Label phoneHint = new Label()
                                {
                                    Text = "Формат: +7 (912) 345-67-89",
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                maskedTextBox.Validating += (s, e) =>
                                {
                                    string phoneValue = maskedTextBox.Text.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");
                                    if (!string.IsNullOrEmpty(phoneValue) && !IsValidPhoneNumber(maskedTextBox.Text))
                                    {
                                        MessageBox.Show("Некорректный номер телефона! Заполните все цифры.", "Ошибка",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        maskedTextBox.Focus();
                                        e.Cancel = true;
                                    }
                                };

                                fieldPanel.Controls.Add(maskedTextBox);
                                fieldPanel.Controls.Add(phoneHint);

                                inputControl = maskedTextBox;
                                // Регистрируем контрол
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);
                                continue;
                            }
                            // ВАЛИДАЦИЯ ДЛЯ ЗАРПЛАТЫ
                            else if (tableName.Equals("Position", StringComparison.OrdinalIgnoreCase) &&
                                     column.DataType == typeof(decimal) &&
                                     column.ColumnName.Equals("base_salary", StringComparison.OrdinalIgnoreCase))
                            {
                                // Создаем панель для группировки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5
                                };

                                if (row[column] != DBNull.Value)
                                {
                                    decimal value = (decimal)row[column];
                                    textBox.Text = value.ToString("0.#####");
                                }

                                Label salaryHint = new Label()
                                {
                                    Text = "Введите базовую зарплату в рублях",
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                // Добавляем валидацию для зарплаты
                                textBox.Validating += (s, e) =>
                                {
                                    if (!string.IsNullOrEmpty(textBox.Text))
                                    {
                                        string decimalValue = textBox.Text.Replace(',', '.');
                                        if (decimal.TryParse(decimalValue, System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture, out decimal salary))
                                        {
                                            if (salary < 0)
                                            {
                                                MessageBox.Show("Зарплата не может быть отрицательной!", "Ошибка",
                                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                textBox.Focus();
                                                e.Cancel = true;
                                            }
                                            else if (salary > 1000000)
                                            {
                                                var result = MessageBox.Show($"Вы указали очень высокую зарплату: {salary:N2}₽\nВы уверены, что это правильно?",
                                                                           "Подтверждение",
                                                                           MessageBoxButtons.YesNo,
                                                                           MessageBoxIcon.Question);
                                                if (result == DialogResult.No)
                                                {
                                                    textBox.Focus();
                                                    e.Cancel = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("Неверный формат зарплаты!", "Ошибка",
                                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            textBox.Focus();
                                            e.Cancel = true;
                                        }
                                    }
                                };

                                fieldPanel.Controls.Add(textBox);
                                fieldPanel.Controls.Add(salaryHint);

                                inputControl = textBox;
                                // Регистрируем контрол
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);
                                continue;
                            }
                            // ВАЛИДАЦИЯ ДЛЯ EMAIL
                            else if (column.DataType == typeof(string) &&
                                     (column.ColumnName.Equals("email", StringComparison.OrdinalIgnoreCase) ||
                                      column.ColumnName.Equals("email_address", StringComparison.OrdinalIgnoreCase)))
                            {
                                // Создаем панель для группировки
                                Panel fieldPanel = new Panel();
                                fieldPanel.Width = 200;
                                fieldPanel.Height = 65;
                                fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Top = 0,
                                    Left = 5
                                };

                                if (row[column] != DBNull.Value)
                                    textBox.Text = row[column].ToString();

                                Label emailHint = new Label()
                                {
                                    Text = "Формат: example@mail.ru",
                                    Font = new Font("Arial", 7),
                                    ForeColor = Color.Gray,
                                    Width = 180,
                                    Top = 25,
                                    Left = 5,
                                    Height = 30
                                };

                                // Добавляем валидацию для email
                                textBox.Validating += (s, e) => 
                                {
                                    if (!string.IsNullOrEmpty(textBox.Text) && !IsValidEmail(textBox.Text))
                                    {
                                        MessageBox.Show("Некорректный email адрес! Пример: example@mail.ru", "Ошибка",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        textBox.Focus();
                                        e.Cancel = true;
                                    }
                                };

                                fieldPanel.Controls.Add(textBox);
                                fieldPanel.Controls.Add(emailHint);

                                inputControl = textBox;
                                // Регистрируем контрол
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(fieldPanel);
                                continue;
                            }
                            else if (column.DataType == typeof(DateTime))
                            {
                                DateTimePicker datePicker = new DateTimePicker()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };

                                if (row[column] != DBNull.Value)
                                    datePicker.Value = (DateTime)row[column];

                                // ДОБАВЛЯЕМ ВАЛИДАЦИЮ ДЛЯ ДАТ
                                if (column.ColumnName.Equals("end_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 65;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    Label dateHint = new Label()
                                    {
                                        Text = "Дата окончания не может быть раньше даты начала",
                                        Font = new Font("Arial", 7),
                                        ForeColor = Color.Gray,
                                        Width = 180,
                                        Top = 25,
                                        Left = 5,
                                        Height = 30
                                    };

                                    fieldPanel.Controls.Add(datePicker);
                                    fieldPanel.Controls.Add(dateHint);

                                    inputControl = datePicker;
                                    // Регистрируем контрол
                                    inputControl.Name = column.ColumnName;
                                    inputControls[column.ColumnName] = inputControl;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
                                else if (column.ColumnName.Equals("start_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 65;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    Label dateHint = new Label()
                                    {
                                        Text = "Дата начала периода",
                                        Font = new Font("Arial", 7),
                                        ForeColor = Color.Gray,
                                        Width = 180,
                                        Top = 25,
                                        Left = 5,
                                        Height = 30
                                    };

                                    fieldPanel.Controls.Add(datePicker);
                                    fieldPanel.Controls.Add(dateHint);

                                    inputControl = datePicker;
                                    // Регистрируем контрол
                                    inputControl.Name = column.ColumnName;
                                    inputControls[column.ColumnName] = inputControl;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
                                else if (column.ColumnName.Equals("created_date", StringComparison.OrdinalIgnoreCase) ||
                                         column.ColumnName.Equals("calculation_date", StringComparison.OrdinalIgnoreCase))
                                {
                                    datePicker.MinDate = DateTime.Today.AddYears(-5);
                                    datePicker.MaxDate = DateTime.Today.AddDays(1);

                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 65;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    Label dateHint = new Label()
                                    {
                                        Text = $"Диапазон: {datePicker.MinDate:dd.MM.yyyy} - {datePicker.MaxDate:dd.MM.yyyy}",
                                        Font = new Font("Arial", 7),
                                        ForeColor = Color.Gray,
                                        Width = 180,
                                        Top = 25,
                                        Left = 5,
                                        Height = 30
                                    };

                                    fieldPanel.Controls.Add(datePicker);
                                    fieldPanel.Controls.Add(dateHint);

                                    inputControl = datePicker;
                                    // Регистрируем контрол
                                    inputControl.Name = column.ColumnName;
                                    inputControls[column.ColumnName] = inputControl;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
                                else if (column.ColumnName.Equals("created_at", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Запрещаем прошлые даты для created_at
                                    datePicker.MinDate = DateTime.Today;
                                    datePicker.Value = DateTime.Today;

                                    Panel fieldPanel = new Panel();
                                    fieldPanel.Width = 200;
                                    fieldPanel.Height = 40;
                                    fieldPanel.Margin = new Padding(3, 3, 3, 10);

                                    datePicker.Top = 0;
                                    datePicker.Left = 5;
                                    datePicker.Width = 190;

                                    fieldPanel.Controls.Add(datePicker);
                                    inputControl = datePicker;

                                    panel.Controls.Add(labelField);
                                    panel.Controls.Add(fieldPanel);
                                    continue;
                                }
                                else
                                {
                                    inputControl = datePicker;
                                }
                            }
                            else if (column.DataType == typeof(bool))
                            {
                                CheckBox checkBox = new CheckBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                                if (row[column] != DBNull.Value)
                                    checkBox.Checked = (bool)row[column];
                                inputControl = checkBox;
                            }
                            else if (column.DataType == typeof(decimal))
                            {
                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                                if (row[column] != DBNull.Value)
                                {
                                    decimal value = (decimal)row[column];
                                    textBox.Text = value.ToString("0.#####");
                                }
                                inputControl = textBox;
                            }
                            else if (column.DataType == typeof(string) && column.ColumnName.Equals("bank_account", StringComparison.OrdinalIgnoreCase))
                            {
                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                                if (row[column] != DBNull.Value)
                                    textBox.Text = row[column].ToString();

                                textBox.Validating += (s, e) =>
                                {
                                    if (!string.IsNullOrEmpty(textBox.Text) && !IsValidBankAccount(textBox.Text))
                                    {
                                        MessageBox.Show("Некорректный номер банковского счета! Должен содержать только цифры и быть длиной 20 символов.", "Ошибка",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        textBox.Focus();
                                        e.Cancel = true;
                                    }
                                };

                                inputControl = textBox;
                            }
                            else if (column.DataType == typeof(int))
                            {
                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                                if (row[column] != DBNull.Value)
                                    textBox.Text = row[column].ToString();
                                inputControl = textBox;
                            }
                            else
                            {
                                TextBox textBox = new TextBox()
                                {
                                    Width = 190,
                                    Margin = new Padding(3, 3, 3, 10)
                                };
                                if (row[column] != DBNull.Value)
                                    textBox.Text = row[column].ToString();
                                inputControl = textBox;
                            }

                            // Стандартное добавление для полей без специальной обработки
                            if (inputControl != null)
                            {
                                inputControl.Name = column.ColumnName;
                                inputControls[column.ColumnName] = inputControl;

                                panel.Controls.Add(labelField);
                                panel.Controls.Add(inputControl);
                            }
                        }

                        Button btnSave = new Button()
                        {
                            Text = "Сохранить",
                            Width = 100,
                            Margin = new Padding(3, 20, 3, 3)
                        };
                        Button btnCancel = new Button()
                        {
                            Text = "Отмена",
                            Width = 100,
                            Margin = new Padding(3, 3, 3, 10)
                        };

                        // ОБРАБОТЧИК СОХРАНЕНИЯ
                        btnSave.Click += (s, e) =>
                        {
                            try
                            {
                                // 0. Специальная проверка уникальности для Payroll (employee_id + period_id), исключая текущую запись
                                if (tableName.Equals("Payroll", StringComparison.OrdinalIgnoreCase))
                                {
                                    int? employeeId = null;
                                    int? periodId = null;

                                    if (inputControls.TryGetValue("employee_id", out Control empCtrl) && empCtrl is ComboBox empCb && empCb.SelectedValue != null)
                                        employeeId = Convert.ToInt32(empCb.SelectedValue);

                                    if (inputControls.TryGetValue("period_id", out Control perCtrl) && perCtrl is ComboBox perCb && perCb.SelectedValue != null)
                                        periodId = Convert.ToInt32(perCb.SelectedValue);

                                    if (employeeId.HasValue && periodId.HasValue)
                                    {
                                        using (SqlCommand checkCmd = new SqlCommand($"SELECT COUNT(*) FROM {tableName} WHERE employee_id=@e AND period_id=@p AND {idColumn} <> @id", connection))
                                        {
                                            checkCmd.Parameters.AddWithValue("@e", employeeId.Value);
                                            checkCmd.Parameters.AddWithValue("@p", periodId.Value);
                                            checkCmd.Parameters.AddWithValue("@id", idValue);
                                            int exists = (int)checkCmd.ExecuteScalar();
                                            if (exists > 0)
                                            {
                                                MessageBox.Show("Для выбранного сотрудника уже существует расчёт за этот период.", "Дублирование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                return;
                                            }
                                        }
                                    }
                                }

                                // Сбор параметров и обновление

                                string setClause = "";
                                List<SqlParameter> parameters = new List<SqlParameter>();

                                // Собираем параметры
                                foreach (var control in inputControls)
                                {
                                    string columnName = control.Key;
                                    Control inputControl = control.Value;

                                    if (setClause != "") setClause += ", ";

                                    setClause += $"{columnName} = @{columnName}";

                                    var col = dataTable.Columns[columnName];
                                    var p = CreateParameter(columnName, inputControl, col?.DataType);
                                    if (p != null) parameters.Add(p);
                                }

                                // ДОБАВЛЯЕМ ID ПАРАМЕТР
                                parameters.Add(new SqlParameter("@id", idValue));

                                // ВЫПОЛНЯЕМ UPDATE
                                string updateQuery = $"UPDATE {tableName} SET {setClause} WHERE {idColumn} = @id";
                                SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                                updateCommand.Parameters.AddRange(parameters.ToArray());

                                int rowsAffected = updateCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Запись успешно обновлена!", "Успех",
                                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    editForm.DialogResult = DialogResult.OK;
                                    editForm.Close();
                                    LoadDataToGrid(dataGridView, query, tabPage, tableName);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при обновлении записи: {ex.Message}", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };
                        // Кнопка отмены
                        btnCancel.Click += (s, e) =>
                        {
                            editForm.DialogResult = DialogResult.Cancel;
                            editForm.Close();
                        };

                        // Добавляем кнопки на панель
                        panel.Controls.Add(btnSave);
                        panel.Controls.Add(btnCancel);
                        editForm.Controls.Add(panel);

                        if (editForm.ShowDialog() == DialogResult.OK)
                        {
                            LoadDataToGrid(dataGridView, query, tabPage, tableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании записи: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteRecord(string tableName, DataGridView dataGridView, string query, TabPage tabPage)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для удаления!", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить выбранную запись?",
                                       "Подтверждение удаления",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
                    string idColumn = GetIdColumnName(tableName);
                    object idValue = selectedRow.Cells[idColumn].Value;

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Мягкие проверки ссылок, чтобы показать понятное сообщение вместо SQL исключения
                        if (tableName.Equals("Department", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Employee WHERE department_id = @id", connection))
                            {
                                check.Parameters.AddWithValue("@id", idValue);
                                int cnt = (int)check.ExecuteScalar();
                                if (cnt > 0)
                                {
                                    MessageBox.Show(
                                        "Нельзя удалить отдел: к нему привязаны сотрудники (" + cnt + ").\n" +
                                        "Сначала переведите сотрудников в другие отделы.",
                                        "Удаление запрещено",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }
                        else if (tableName.Equals("Position", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Employee WHERE position_id = @id", connection))
                            {
                                check.Parameters.AddWithValue("@id", idValue);
                                int cnt = (int)check.ExecuteScalar();
                                if (cnt > 0)
                                {
                                    MessageBox.Show(
                                        "Нельзя удалить должность: к ней привязаны сотрудники (" + cnt + ").\n" +
                                        "Сначала измените должность у сотрудников.",
                                        "Удаление запрещено",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }
                        else if (tableName.Equals("PayPeriod", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Payroll WHERE period_id = @id", connection))
                            {
                                check.Parameters.AddWithValue("@id", idValue);
                                int cnt = (int)check.ExecuteScalar();
                                if (cnt > 0)
                                {
                                    MessageBox.Show(
                                        "Нельзя удалить расчётный период: к нему привязаны расчёты зарплаты (" + cnt + ").",
                                        "Удаление запрещено",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }

                        SqlCommand command = new SqlCommand($"DELETE FROM {tableName} WHERE {idColumn} = @id", connection);
                        command.Parameters.AddWithValue("@id", idValue);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Запись успешно удалена!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Обновляем данные в таблице
                            LoadDataToGrid(dataGridView, query, tabPage, tableName);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить запись!", "Ошибка",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string GetRussianColumnName(string columnName)
        {
            var translations = new Dictionary<string, string>
            {
                {"employee_id", "ID сотрудника"},
                {"full_name", "ФИО"},
                {"hire_date", "Дата приема"},
                {"bank_account", "Банковский счет"},
                {"department_id", "ID отдела"},
                {"position_id", "ID должности"},
                {"department_name", "Название отдела"},
                {"position_title", "Должность"},
                {"base_salary", "Базовая зарплата"},
                {"name", "Название"},
                {"title", "Название"},
                {"payroll_id", "ID расчета"},
                {"month", "Месяц"},
                {"year", "Год"},
                {"calculation_date", "Дата расчета"},
                {"total_earnings", "Общий доход"},
                {"total_deductions", "Общие удержания"},
                {"net_payment", "Чистая выплата"},
                {"phone", "Телефон"},
                {"phone_number", "Телефон"},
                {"telephone", "Телефон"},
                {"mobile_phone", "Мобильный телефон"},
                {"contact_phone", "Контактный телефон"},
                {"email", "Email"},
                {"email_address", "Email адрес"}
            };

            return translations.ContainsKey(columnName) ? translations[columnName] : columnName;
        }

        private string GetIdColumnName(string tableName)
        {
            // Специфичные случаи для разных таблиц
            var idColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"Department", "department_id"},
        {"Position", "position_id"},
        {"Employee", "employee_id"},
        {"PayPeriod", "period_id"}, // Исправлено здесь
        {"Payroll", "payroll_id"}
    };

            return idColumns.ContainsKey(tableName) ? idColumns[tableName] : tableName.ToLower() + "_id";
        }

        private List<string> GetAllowedStatusValues(string tableName, string columnName)
        {
            var allowedValues = new Dictionary<string, List<string>>
    {
        {"PayPeriod_status", new List<string> {"Open", "Closed", "Processing"}},
        {"Payroll_status", new List<string> {"Draft", "Calculated", "Approved", "Paid", "Cancelled"}}
    };

            string key = $"{tableName}_{columnName}";
            return allowedValues.ContainsKey(key) ? allowedValues[key] : new List<string>();
        }

        private bool IsValidBankAccount(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return true; // Пустое значение допустимо

            // Убираем пробелы и другие разделители
            string cleanAccount = accountNumber.Replace(" ", "").Replace("-", "");

            // Проверяем, что содержит только цифры и правильную длину
            if (cleanAccount.Length != 20)
                return false;

            return cleanAccount.All(char.IsDigit);
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return true; // Пустое значение допустимо

            // Убираем все нецифровые символы кроме плюса в начале
            string cleanPhone = phoneNumber.Trim();

            // Проверяем основные форматы:
            // +7XXXXXXXXXX, 8XXXXXXXXXX, +375XXXXXXXXX, etc.
            if (cleanPhone.StartsWith("+"))
            {
                // Международный формат
                string digitsOnly = new string(cleanPhone.Skip(1).Where(char.IsDigit).ToArray());
                return digitsOnly.Length >= 10 && digitsOnly.Length <= 12;
            }
            else
            {
                // Локальный формат
                string digitsOnly = new string(cleanPhone.Where(char.IsDigit).ToArray());
                return digitsOnly.Length >= 10 && digitsOnly.Length <= 11;
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true;

            try
            {
                // Простая валидация email
                var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private string GetDisplayMember(string columnName)
        {
            var displayMembers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"department_id", "name"},
        {"position_id", "title"},
        {"employee_id", "full_name"},
        {"period_id", "period"}
    };

            return displayMembers.ContainsKey(columnName) ? displayMembers[columnName] : columnName;
        }

        private SqlParameter CreateParameter(string columnName, Control inputControl, Type targetType)
        {
            if (inputControl is ComboBox combo)
            {
                if (targetType == typeof(int))
                {
                    if (combo.SelectedValue == null) return new SqlParameter("@" + columnName, DBNull.Value);
                    return new SqlParameter("@" + columnName, Convert.ToInt32(combo.SelectedValue));
                }
                if (combo.SelectedItem == null) return new SqlParameter("@" + columnName, DBNull.Value);
                return new SqlParameter("@" + columnName, combo.SelectedItem.ToString());
            }

            if (inputControl is TextBox tb)
            {
                if (string.IsNullOrEmpty(tb.Text)) return null;
                if (targetType == typeof(decimal))
                {
                    return new SqlParameter("@" + columnName, decimal.Parse(tb.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture));
                }
                if (targetType == typeof(int))
                {
                    return new SqlParameter("@" + columnName, int.Parse(tb.Text));
                }
                return new SqlParameter("@" + columnName, tb.Text);
            }

            if (inputControl is DateTimePicker dtp)
            {
                return new SqlParameter("@" + columnName, dtp.Value);
            }

            if (inputControl is NumericUpDown nud)
            {
                return new SqlParameter("@" + columnName, (int)nud.Value);
            }

            if (inputControl is CheckBox chk)
            {
                return new SqlParameter("@" + columnName, chk.Checked);
            }

            if (inputControl is MaskedTextBox mtb)
            {
                return new SqlParameter("@" + columnName, mtb.Text);
            }

            return null;
        }

        private string GetValueMember(string columnName)
        {
            var valueMembers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"department_id", "department_id"},
        {"position_id", "position_id"},
        {"employee_id", "employee_id"},
        {"period_id", "period_id"}
    };

            return valueMembers.ContainsKey(columnName) ? valueMembers[columnName] : columnName + "_id";
        }


        private void TestConnection()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    MessageBox.Show("Подключение к базе данных успешно!", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
    }
}