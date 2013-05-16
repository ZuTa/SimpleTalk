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
using System.Data;
using System.Windows.Threading;

namespace SimpleTalk.Windows
{
    /// <summary>
    /// Interaction logic for FindUsersWindow.xaml
    /// </summary>
    public partial class FindUsersWindow : Window
    {
        #region Fields and Properties
        private delegate void updateCallBack(string response);
        private DataTable tableOfUsers;
        private const int columnsSize = 6;
        #endregion

        public FindUsersWindow(UserStatuses userStatus)
        {
            InitializeComponent();
            CreateDataTable();
            listView.ItemsSource = tableOfUsers.DefaultView;

            ClientHelper.GetUsersByPatternResult = Result;

            btnSendRequest.IsEnabled = (userStatus != UserStatuses.Offline);
        }

        private void Result(string response)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new updateCallBack(Result), response);
            else
                CreateListOfUsers(response);
        }

        private void CreateDataTable()
        {
            tableOfUsers = new DataTable();
            tableOfUsers.Columns.Add("N");
            tableOfUsers.Columns.Add("ID");
            tableOfUsers.Columns.Add("Login");
            tableOfUsers.Columns.Add("FirstName");
            tableOfUsers.Columns.Add("LastName");
            tableOfUsers.Columns.Add("UserStatus");
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSendRequest_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItem != null)
            {
                try
                {
                    ClientHelper.SendRequestAskFriend(int.Parse(tableOfUsers.Rows[listView.SelectedIndex]["ID"].ToString()));
                    MessageBox.Show(
                        string.Format(Properties.Resources.txtSentFriendRequest, tableOfUsers.Rows[listView.SelectedIndex]["Login"].ToString()),
                        Properties.Resources.AppName, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
                MessageBox.Show(Properties.Resources.WarningSendFriendRequest, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            tableOfUsers.Rows.Clear();
            try
            {
                ClientHelper.GetUsersByPattern(tbPattern.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void CreateListOfUsers(string listString)
        {
            string[] users = listString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string user in users)
            {
                string[] details= user.Split(' ');
                if (details.Length < columnsSize) continue; 

                DataRow r = tableOfUsers.NewRow();
                for (int i = 0; i < columnsSize; i++) 
                    r[i] = details[i];
                r[columnsSize - 1] = (UserStatuses)int.Parse(r[columnsSize - 1].ToString());
                tableOfUsers.Rows.Add(r);
            }
        }
    }
}
