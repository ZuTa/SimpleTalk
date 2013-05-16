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
using System.Threading;
using SimpleTalk.Client;

namespace SimpleTalk.Windows
{
    /// <summary>
    /// Interaction logic for IMWindow.xaml
    /// </summary>
    public partial class IMWindow : Window
    {
        #region Fields and Properties

        private delegate void openHistory(int friendID, string friendLogin);

        private int friendID = -1;
        private string friendLogin = "";

        private readonly Brush friendsColor = Brushes.Blue;
        private readonly Brush myColor = Brushes.Gray;
        private readonly Brush systemColor = Brushes.Red;
        private readonly Brush textColor = Brushes.Black;

        private HistoryWindow historyWindow = null;
        private InformationWindow informationWindow = null;
        private bool isHistoryWindowOpen = false;
        private bool isInfoWindowOpen = false;

        #endregion

        public IMWindow(int friendID, string friendLogin)
        {
            InitializeComponent();

            this.friendID = friendID;
            this.friendLogin = friendLogin;

            this.Title = string.Format(Properties.Resources.txtConversationWith, friendLogin);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            string mess = (message.Text).Trim(' ');

            SetMessage(mess);
            ClientHelper.SendMessage(mess, friendID);
            message.Text = "";
        }

        private string ConvertRichTextBoxContentsToString(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(rtb.Document.ContentStart,
                rtb.Document.ContentEnd);
            return textRange.Text;
        }

        private void AppendText(string Text, Brush color)
        {
            TextRange range = new TextRange(messages.Document.ContentEnd, messages.Document.ContentEnd);
            range.Text = Text;
            range.ApplyPropertyValue(TextElement.ForegroundProperty, color);
        }

        public void GetMessage(string message)
        {
            string[] m = message.Split('~');
            AppendText(string.Format(friendLogin + " ({0}): ", m[0]), friendsColor);
            AppendText(string.Format(" {0}\n", m[1]), textColor);
            messages.ScrollToEnd();
        }

        private void SetMessage(string message)
        {
            AppendText(string.Format(Properties.Settings.Default.Login + " ({0}): ", DateTime.Now), myColor);
            AppendText(string.Format(" {0}\n", message), textColor);
            messages.ScrollToEnd();
        }

        private void message_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSend.IsEnabled = ((message.Text.ToString().Trim(' ', '\r')).Length != 0);
        }

        private void message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // press ENTER
                SendMessage();
            }
        }

        public void SetSystemMessage(string message)
        {
            AppendText(Properties.Resources.msgSystem, systemColor);
            AppendText(string.Format(message + "\n", friendLogin), textColor);
            messages.ScrollToEnd();
        }

        public void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            if (!isHistoryWindowOpen)
            {
                Thread thread = new Thread(new ThreadStart(
                    delegate()
                    {
                        this.Dispatcher.Invoke(new openHistory(CreateHistoryWindow), friendID, friendLogin);
                    }));
                thread.Start();
            }
            else OpenHistoryWindow();
        }

        private void OpenHistoryWindow()
        {
            isHistoryWindowOpen = true;
            historyWindow.WindowState = WindowState.Normal;
            historyWindow.Show();
            historyWindow.Activate();
        }

        private void CreateHistoryWindow(int friendID, string friendLogin)
        {
            historyWindow = new HistoryWindow(friendID, friendLogin);
            historyWindow.Closed += new EventHandler(historyWindow_Closed);
            OpenHistoryWindow();
        }

        private void historyWindow_Closed(object sender, EventArgs e)
        {
            isHistoryWindowOpen = false;
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
            if (!isInfoWindowOpen)
            {
                Thread thread = new Thread(new ThreadStart(
                    delegate()
                    {
                        this.Dispatcher.Invoke(new openHistory(CreateInformationWindow), friendID, friendLogin);
                    }));
                thread.Start();
            }
            else OpenInformationWindow();
        }

        private void OpenInformationWindow()
        {
            isInfoWindowOpen = true;
            informationWindow.WindowState = WindowState.Normal;
            informationWindow.Show();
            informationWindow.Activate();
        }

        private void CreateInformationWindow(int friendID, string friendLogin)
        {
            informationWindow = new InformationWindow(friendID, friendLogin);
            informationWindow.Closed += new EventHandler(informationWindow_Closed);
            OpenInformationWindow();
        }

        private void informationWindow_Closed(object sender, EventArgs e)
        {
            isInfoWindowOpen = false;
        }

    }
}
