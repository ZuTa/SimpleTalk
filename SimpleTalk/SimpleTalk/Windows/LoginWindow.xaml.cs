using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SimpleTalk.Client;
using SimpleTalk;

namespace SimpleTalk.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        #region Fields and Properties

        private delegate void updateCallback(bool flag);
        private string userName = "";
        private bool successful = false;

        public string UserName { get { return userName; } }
        public bool Successful { get { return successful; } }

        #endregion

        public LoginWindow()
        {
            InitializeComponent();

            ClientHelper.LoginResult = Result;

            tbLogin.Focus();
        }

        public string GetPassword()
        {
            return pbPassowrd.Password.ToString();
        }

        private void Result(bool flag)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new updateCallback(Result), flag);
            else
            {

                if (flag)
                {
                    //MessageBox.Show(Properties.Resources.txtSuccessfulLogin, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                    userName = tbLogin.Text;
                    successful = true;
                    this.Close();
                }
                else
                    MessageBox.Show(Properties.Resources.ErrorLogging, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
           TryLogin(tbLogin.Text, pbPassowrd.Password.ToString());
        }

        public void TryLogin(string login, string password)
        {
            ClientHelper.SendLogin(tbLogin.Text, pbPassowrd.Password.ToString());
        }

    }
}
