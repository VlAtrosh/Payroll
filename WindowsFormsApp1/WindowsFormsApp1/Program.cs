using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Показываем форму входа
            LoginForm loginForm = new LoginForm();

            if (loginForm.ShowDialog() == DialogResult.OK && loginForm.LoginSuccessful)
            {
                // Если вход успешен, запускаем основное приложение
                Application.Run(new Form1(loginForm.ConnectionString));
            }
            else
            {
                // Если пользователь отменил вход
                MessageBox.Show("Вход отменен. Приложение будет закрыто.", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
        }
    }
}