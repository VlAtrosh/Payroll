using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class LoginForm : Form
    {
        public string ConnectionString { get; private set; }
        public bool LoginSuccessful { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
            LoginSuccessful = false;
            try { WindowsFormsApp1.UiTheme.ApplyTheme(this, WindowsFormsApp1.UiTheme.Palettes.Light); } catch {}
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Настройка формы
            this.Text = "Вход в систему";
            this.Size = new System.Drawing.Size(350, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Метки
            Label lblServer = new Label() { Text = "Сервер:", Left = 20, Top = 20, Width = 80 };
            Label lblDatabase = new Label() { Text = "База данных:", Left = 20, Top = 50, Width = 80 };
            Label lblLogin = new Label() { Text = "Логин:", Left = 20, Top = 80, Width = 80 };
            Label lblPassword = new Label() { Text = "Пароль:", Left = 20, Top = 110, Width = 80 };

            // Текстовые поля
            TextBox txtServer = new TextBox() { Text = "192.168.9.203\\sqlexpress", Left = 110, Top = 20, Width = 200 };
            TextBox txtDatabase = new TextBox() { Text = "Абоба", Left = 110, Top = 50, Width = 200 };
            TextBox txtLogin = new TextBox() { Text = "student1", Left = 110, Top = 80, Width = 200 };
            TextBox txtPassword = new TextBox() { PasswordChar = '*', Left = 110, Top = 110, Width = 200 };

            // Кнопки
            Button btnConnect = new Button() { Text = "Подключиться", Left = 110, Top = 150, Width = 100 };
            Button btnCancel = new Button() { Text = "Отмена", Left = 220, Top = 150, Width = 80 };

            btnConnect.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtServer.Text) || string.IsNullOrEmpty(txtDatabase.Text) ||
                    string.IsNullOrEmpty(txtLogin.Text) || string.IsNullOrEmpty(txtPassword.Text))
                {
                    MessageBox.Show("Заполните все поля!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Тестируем подключение
                string testConnectionString = $"Server={txtServer.Text};Database={txtDatabase.Text};User Id={txtLogin.Text};Password={txtPassword.Text};";

                if (TestConnection(testConnectionString))
                {
                    ConnectionString = testConnectionString;
                    LoginSuccessful = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверные данные для подключения!", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[] {
                lblServer, txtServer,
                lblDatabase, txtDatabase,
                lblLogin, txtLogin,
                lblPassword, txtPassword,
                btnConnect, btnCancel
            });

            this.ResumeLayout(false);
        }

        private bool TestConnection(string connectionString)
        {
            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}