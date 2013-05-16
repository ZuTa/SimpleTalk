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
using System.Windows.Threading;

namespace SimpleTalk.Windows
{
    /// <summary>
    /// Interaction logic for Registration.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        #region Fields and Properties
        private delegate void updateCallback(bool flag);
        #endregion

        public RegistrationWindow()
        {
            InitializeComponent();

            ClientHelper.CheckLoginResult = Result;
        }

        private void Result(bool flag)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new updateCallback(Result), flag);
            else
            {
                if(flag)
                {
                    MessageBox.Show(Properties.Resources.txtSuccessfulRegistration, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                    MessageBox.Show(Properties.Resources.txtUserAlreadyExist, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Verify())
                    ClientHelper.CheckLogin(tbLogin.Text, pbPassword.Password, tbFirstName.Text, tbLastName.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool Verify()
        {
            if (tbLogin.Text == "")
            {
                MessageBox.Show(Properties.Resources.ErrorRegistrationFields, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            //TODO : implement checking password
            return true;
        }
    }
}
