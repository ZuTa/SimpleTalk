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

namespace SimpleTalk.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SimpleTalk.Properties.Settings.Default.Server = tbIP.Text;
            SimpleTalk.Properties.Settings.Default.ServerPort = int.Parse(tbPort.Text.ToString());
            SimpleTalk.Properties.Settings.Default.RememberLogin = (bool)cbRememberLogin.IsChecked;
            SimpleTalk.Properties.Settings.Default.Save();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbIP.Text = SimpleTalk.Properties.Settings.Default.Server;
            tbPort.Text = SimpleTalk.Properties.Settings.Default.ServerPort.ToString();
            cbRememberLogin.IsChecked = SimpleTalk.Properties.Settings.Default.RememberLogin;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
