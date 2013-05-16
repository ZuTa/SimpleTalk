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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using SimpleTalk.Windows;
using SimpleTalk.Client;
using System.Data;
using System.Threading;

namespace SimpleTalk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields and Properties
        
        private delegate void systemMessagesCallBack(string response);
        private delegate void friendRequestsCallBack(string response);
        private delegate void friendListCallBack(string response);
        private delegate void sendMessageCallBack(string message, int fromID);
        private delegate void deleteFriendCallBack(int ID);
        private delegate IMWindow createIMWindowCallBack(int id, string login, string status);
        private delegate void onServerOfflineCallBack();

        private UserStatuses userStatus;
        private string userName;
        private DataTable tableOfUsers;

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip ctxTrayMenu;
        private bool isAppExiting = false;

        private Dictionary<int, IMWindow> windows;

        private BitmapImage img = null;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateDataTable()
        {
            tableOfUsers = new DataTable();
            tableOfUsers.Columns.Add("ID");
            tableOfUsers.Columns.Add("Login");
            tableOfUsers.Columns.Add("UserStatus");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = e.OriginalSource as MenuItem;
            if (item != null)
            {
                switch (item.Name)
                {
                    case "miLogin":
                        Login();
                        break;
                    case "miRegister":
                        Registration();
                        break;
                    case "miClose":
                        this.Close();
                        break;
                    case "miSettings":
                        Settings();
                        break;
                    case "miStatusOnline":
                        ChangeStatus(UserStatuses.Online);
                        break;
                    case "miStatusOffline":
                        ChangeStatus(UserStatuses.Offline);
                        break;
                    case "miFindUsers":
                        FindUsers();
                        break;
                    default:
                        break;
                }
            }
        }

        #region CallBacks

        private void ResultRequestForSystemMessages(string response)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new systemMessagesCallBack(ResultRequestForSystemMessages), response);
            else
            {
                ReadSystemMessages(response);
                ClientHelper.SendRequestForFriendsList();
            }
        }

        private void ResultRequestForFriendsList(string response)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new friendListCallBack(ResultRequestForFriendsList), response);
            else
                ReadFriendsList(response);
        }

        private void NotificationAboutSuccessfulFriendRequest(string response)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new friendRequestsCallBack(NotificationAboutSuccessfulFriendRequest), response);
            else
            {
                ClientHelper.SendRequestForFriendsList();
                MessageBox.Show(
                    string.Format(Properties.Resources.txtSuccessfulFriendRequest, response),
                    Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SendMessageToWindow(string message, int fromID)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new sendMessageCallBack(SendMessageToWindow), message, fromID);
            else
            {
                foreach (int id in windows.Keys)
                {
                    if (id == fromID)
                    {
                        windows[id].GetMessage(message);
                        return;
                    }
                }
                //if conversation with user did not start
                foreach (DataRow r in tableOfUsers.Rows)
                {
                    if (int.Parse(r["ID"].ToString()) == fromID)
                    {
                        IMWindow window = CreateIMWindow(fromID, r["Login"].ToString(), r["UserStatus"].ToString());
                        window.GetMessage(message);
                        return;
                    }
                }
                //this user is not friend for current
            }
        }

        private void DeleteFriend(int ID)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new deleteFriendCallBack(DeleteFriend), ID);
            else
            {
                DataRow deleteRow = null;
                foreach (DataRow r in tableOfUsers.Rows)
                {
                    if (int.Parse(r["ID"].ToString()) == ID)
                    {
                        deleteRow = r;
                        break;
                    }
                }
                if (deleteRow != null)
                    deleteRow.Delete();
            }
        }


        #endregion

        #region Windows

        private void FindUsers()
        {
            FindUsersWindow findUsersWindow = new FindUsersWindow(userStatus);
            findUsersWindow.ShowDialog();
        }

        private void Settings()
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void Registration()
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog();
        }

        private void Login()
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Owner = this;
            loginWindow.ShowDialog();
            IsSuccessfulLogin(loginWindow);
        }

        #endregion
        
        private void IsSuccessfulLogin(LoginWindow loginWindow)
        {
            if (loginWindow.Successful)
            {
                SetUserName(loginWindow.UserName);
                SetUserStatus(UserStatuses.Online);
                RememberLoginAndPassword(loginWindow.GetPassword());
                Update();
            }
        }

        private void RememberLoginAndPassword(string password)
        {
            Properties.Settings.Default.Login = userName;
            Properties.Settings.Default.Password = password;
            Properties.Settings.Default.Save();
        }

        public void ChangeStatus(UserStatuses newUserStatus)
        {
            // if new status not offline but userStatus was offline
            if (userStatus == UserStatuses.Offline && newUserStatus != UserStatuses.Offline)
                Login();
            else
            {
                ClientHelper.ChangeUserStatus((int)newUserStatus);
                SetUserStatus(newUserStatus);
            }
            if (newUserStatus == UserStatuses.Offline)
                ClearData();
        }

        private void ClearData()
        {
            // maybe more
            SetUserStatus(UserStatuses.Offline);
            ClearFriendsList();
            CloseAllWindows();
        }

        private void ClearFriendsList()
        {
            tableOfUsers.Rows.Clear();
        }

        private void CloseAllWindows()
        {
            List<IMWindow> list = new List<IMWindow>();
            foreach(IMWindow window in windows.Values)
            {
                list.Add(window);
            }
            foreach (IMWindow window in list)
            {
                window.Close();
            }
        }

        public void SetUserName(string newUserName)
        {
            this.userName = newUserName;
            sbiUserName.Content = newUserName;
        }

        public void SetUserStatus(UserStatuses newUserStatus)
        {
            this.userStatus = newUserStatus;
            GetImageFor(newUserStatus); // save in img
            imgUserStatus.Source = img;
            if (userStatus == UserStatuses.Offline) sbiUserName.Content = "";
        }

        private void GetImageFor(UserStatuses status)
        {
            Uri src = null;
            if (status == UserStatuses.Online)
                src = new Uri(@"/SimpleTalk;component/Images/circle_green.png", UriKind.Relative);
            else
                src = new Uri(@"/SimpleTalk;component/Images/circle_red.png", UriKind.Relative);
            img = new BitmapImage(src);
        }

        private void Update()
        {
            // Successful login
            ClientHelper.SendRequestForSystemMessages();
            ClientHelper.SendRequestForFriendsList();
            ClientHelper.SendRequestForOfflineMessages();
        }

        private void ReadSystemMessages(string messagesString)
        {
            messagesString = messagesString.Remove(0, 1);
            string[] messages = messagesString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder responseReadMessage = new StringBuilder();
            StringBuilder responseAcceptFriendRequest = new StringBuilder();
            StringBuilder responseDeleteFriendRequest = new StringBuilder();

            foreach (string message in messages)
            {
                string[] details = message.Split(' ');
                if (details.Length < 6) continue;
                responseReadMessage.Append(details[0] + ",");
                // 0 - messageID
                // 1 - FromID
                // 2 - Login
                // 3 - Date
                // 4 - Time
                // 5 - Message

                if (details[5] == string.Empty)
                {
                    // it is a friend request
                    if (MessageBox.Show(
                        string.Format(Properties.Resources.txtFriendRequest, details[2]),
                        Properties.Resources.AppName, MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                        responseAcceptFriendRequest.Append(details[1] + ",");
                    else
                        responseDeleteFriendRequest.Append(details[0] + ",");
                }
                else
                {
                    // it is a simple message from server
                    ClientCommands command = (ClientCommands)int.Parse(details[5]);
                    switch (command)
                    {
                        case ClientCommands.SuccessfulFriendRequest:
                            NotificationAboutSuccessfulFriendRequest(details[2]);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (responseReadMessage.Length > 0) ClientHelper.SendReadMessages(responseReadMessage.ToString());
            if (responseAcceptFriendRequest.Length > 0) ClientHelper.SendAcceptedFriendsRequests(responseAcceptFriendRequest.ToString());
            if (responseDeleteFriendRequest.Length > 0) ClientHelper.SendDeleteMessages(responseDeleteFriendRequest.ToString());
        }

        private void ReadFriendsList(string listOfFriends)
        {
            tableOfUsers.Rows.Clear();
            string[] friends = listOfFriends.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string friend in friends)
            {
                string[] details = friend.Split(' ');
                if (details.Length < 3) continue;
                // 0 - UserID
                // 1 - Login
                // 2 - Status
                DataRow r = tableOfUsers.NewRow();
                for (int i = 0; i < 2; i++)
                    r[i] = details[i];

                UserStatuses status = (UserStatuses)int.Parse(details[2].ToString());
                //BitmapImage img = GetImageFor(status);
                //Image im = new Image();
                //im.Source=img;
                r[2] = status;

                int userID = int.Parse(r[0].ToString());

                //SendMessageToIMWindow(status, userID);
                tableOfUsers.Rows.Add(r);
            }
        }

        private void SendMessageToIMWindow(UserStatuses status, int userID)
        {
            if (status == UserStatuses.Offline)
            {
                IMWindow window = GetWindowByID(userID);
                if (window != null)
                {
                    window.SetSystemMessage(Properties.Resources.msgUserOffline);
                }
            }
        }

        private void Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isAppExiting)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
                ChangeStatus(UserStatuses.Offline);
        }

        private void friends_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (friends.SelectedItem != null)
            {
                IMWindow window;
                if (!IsExist(int.Parse(tableOfUsers.Rows[friends.SelectedIndex]["ID"].ToString()), out window))
                    CreateIMWindow(friends.SelectedIndex);
                else
                    OpenWindow(window);
            }
        }

        private void OpenWindow(Window window)
        {
            window.WindowState = WindowState.Normal;
            window.Show();
            window.Activate();
        }

        private bool IsExist(int ID,out IMWindow window)
        {
            window = null;
            foreach (int id in windows.Keys)
            {
                if (id == ID)
                {
                    window = windows[id];
                    return true;
                }
            }
            return false;
        }

        private void CreateIMWindow(int index)
        {
            Thread t = new Thread(new ThreadStart(
                 delegate()
                 {
                     this.Dispatcher.Invoke(
                         new createIMWindowCallBack(CreateIMWindow),
                         int.Parse(tableOfUsers.Rows[index]["ID"].ToString()),
                         tableOfUsers.Rows[index]["Login"].ToString(),
                         tableOfUsers.Rows[index]["UserStatus"].ToString());
                 }));
            t.Start();
        }

        private IMWindow CreateIMWindow(int id, string login, string status)
        {
            IMWindow imWindow = new IMWindow(id, login);
            imWindow.Owner = this;
            lock (windows) windows.Add(id, imWindow);

            imWindow.Closed += new EventHandler(imWindow_Closed);

            OpenWindow(imWindow);

            SendMessageToIMWindow(ConvertToUserStatus(status), id);

            return imWindow;
        }

        private UserStatuses ConvertToUserStatus(string status)
        {
            if (status.Trim(' ') == UserStatuses.Online.ToString()) return UserStatuses.Online;
            return UserStatuses.Offline;
        }

        private void SetWindowPosition()
        {
            Left = SystemParameters.PrimaryScreenWidth - (double)GetValue(WidthProperty) - 70;
            Top = SystemParameters.PrimaryScreenHeight - this.Height - 70;
        }

        private void imWindow_Closed(object sender, EventArgs e)
        {
            if (!isAppExiting)
                lock (windows) windows.Remove(GetIDByWindow(sender as IMWindow));
        }

        private int GetIDByWindow(IMWindow w)
        {
            foreach (KeyValuePair<int, IMWindow> item in windows)
            {
                if (item.Value == w)
                    return item.Key;
            }
            return -1;
        }
        private IMWindow GetWindowByID(int id)
        {
            foreach (KeyValuePair<int, IMWindow> item in windows)
            {
                if (item.Key == id)
                    return item.Value;
            }
            return null;
        }

        private void OnServerOffline()
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new onServerOfflineCallBack(OnServerOffline));
            else
            {
                ClearData();
                MessageBox.Show(Properties.Resources.ErrorLostConnection, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void Main_Initialized(object sender, EventArgs e)
        {
            CreateDataTable();
            friends.ItemsSource = tableOfUsers.DefaultView;

            SetUserStatus(UserStatuses.Offline);
            SetUserName("");

            //events
            ClientHelper.RequestForSystemMessagesResult = ResultRequestForSystemMessages;
            ClientHelper.AcceptedFriendsRequestsResult = NotificationAboutSuccessfulFriendRequest;
            ClientHelper.RequestForFriendsListResult = ResultRequestForFriendsList;
            ClientHelper.OnMessageReceived = SendMessageToWindow;
            ClientHelper.ServerOffline = OnServerOffline;
            ClientHelper.OnDeletedFriend = DeleteFriend;

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.google_talk;
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);

            //Create a object for the context menu
            ctxTrayMenu = new System.Windows.Forms.ContextMenuStrip();
            //Add the Menu Item to the context menu
            System.Windows.Forms.ToolStripMenuItem mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            mnuExit.Text = "Вийти";
            mnuExit.Click += new EventHandler(mnuExit_Click);
            System.Windows.Forms.ToolStripMenuItem mnuShow = new System.Windows.Forms.ToolStripMenuItem();
            mnuShow.Text = "Показати";
            mnuShow.Click += new EventHandler(mnuShow_Click);
            ctxTrayMenu.Items.Add(mnuShow);
            ctxTrayMenu.Items.Add(mnuExit);

            notifyIcon.ContextMenuStrip = ctxTrayMenu;

            SetWindowPosition();

            windows = new Dictionary<int, IMWindow>();
        }

        private void AutoLogin()
        {
            if (Properties.Settings.Default.RememberLogin)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.TryLogin(Properties.Settings.Default.Login, Properties.Settings.Default.Password);
                IsSuccessfulLogin(loginWindow);
            }
        }

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                mnuShow_Click(sender, e);
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            isAppExiting = true;
            this.Close();
            notifyIcon.Visible = false;
        }

        private void mnuShow_Click(object sender, EventArgs e)
        {
            this.Activate();
            this.WindowState = WindowState.Normal;
            this.Show();            
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
                this.Hide();
            base.OnStateChanged(e);
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            // Have problem with autologin
            //AutoLogin();  
        }

        private void friends_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete &&
                friends.SelectedItem != null &&
                MessageBox.Show(string.Format(Properties.Resources.AskDeleteFriend, tableOfUsers.Rows[friends.SelectedIndex]["Login"].ToString()),
                Properties.Resources.AppName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ClientHelper.DeleteFriend(int.Parse(tableOfUsers.Rows[friends.SelectedIndex]["ID"].ToString()));
                tableOfUsers.Rows[friends.SelectedIndex].Delete();
            }

        }

    }
}
