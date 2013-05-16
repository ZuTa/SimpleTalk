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

namespace SimpleTalk.Windows
{
    /// <summary>
    /// Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        private readonly Brush friendsColor = Brushes.Blue;
        private readonly Brush myColor = Brushes.Gray;
        private readonly Brush textColor = Brushes.Black;

        private int ID;
        private string login;

        private delegate void HistoryReceived(string list);

        public HistoryWindow(int friendID, string friendLogin)
        {
            InitializeComponent();

            this.ID = friendID;
            this.login = friendLogin;

            this.Title = string.Format(Properties.Resources.msgHistory, login);

            ClientHelper.OnHistoryReceived = onHistoryReceived;

            GetHistory();
        }

        private void onHistoryReceived(string list)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new HistoryReceived(onHistoryReceived), list);
            else
                SetHistory(list);
        }

        private void GetHistory()
        {
            Messages.Document.Blocks.Clear();
            ClientHelper.GetHistory(ID);
        }

        private void SetHistory(string list)
        {
            // TODO: Change sending from server
            // StringBuilder List = new StringBuilder(list);
            int p=0, fromID, toID, size;
            string date, time, message;
            while (list.Length > 0)
            {
                if ((p = list.IndexOf(' ')) != -1) fromID = int.Parse(list.Substring(0, p));
                else break;
                list = list.Remove(0, p + 1);
                if ((p = list.IndexOf(' ')) != -1) toID = int.Parse(list.Substring(0, p));
                else break;
                list = list.Remove(0, p + 1);
                if ((p = list.IndexOf(' ')) != -1) date = list.Substring(0, p);
                else break;
                list = list.Remove(0, p + 1);
                if ((p = list.IndexOf(' ')) != -1) time = list.Substring(0, p);
                else break;
                list = list.Remove(0, p + 1);
                if ((p = list.IndexOf(' ')) != -1) size = int.Parse(list.Substring(0, p));
                else break;
                list = list.Remove(0, p + 1);
                message = "";
                for (int i = 0; i < size; i++)
                    message += list[i];
                list = list.Remove(0, size);
                if (fromID != ID)
                    AddMessage(string.Format(Properties.Settings.Default.Login + " ({0} {1}): ", date, time), myColor);
                else
                    AddMessage(string.Format(login + " ({0} {1}): ", date, time), friendsColor);
                AddMessage(string.Format(" {0}\n", message), textColor);
                Messages.ScrollToEnd();
            }
        }

        private void AddMessage(string Text, Brush color)
        {
            TextRange range = new TextRange(Messages.Document.ContentEnd, Messages.Document.ContentEnd);
            range.Text = Text;
            range.ApplyPropertyValue(TextElement.ForegroundProperty, color);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
