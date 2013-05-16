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
    /// Interaction logic for InformationWindow.xaml
    /// </summary>
    public partial class InformationWindow : Window
    {
        private delegate void onInformationReceived(string response);

        public InformationWindow(int friendID, string friendLogin)
        {
            InitializeComponent();

            this.Title = string.Format(Properties.Resources.msgInformation, friendLogin);
            ClientHelper.OnInformationReceived = InformationReceived;

            ClientHelper.GetInformation(friendID);         
        }

        private void InformationReceived(string response)
        {
            if (!this.Dispatcher.CheckAccess())
                this.Dispatcher.Invoke(new onInformationReceived(InformationReceived), response);
            else
                SetInformation(response);            
        }

        private void SetInformation(string response)
        {
            int size = 0, p;
            string fname, lname;
            while (response.Length > 0)
            {
                if ((p = response.IndexOf(' ')) != -1) size = int.Parse(response.Substring(0, p));
                else break;
                response = response.Remove(0, p + 1);
                fname = "";
                for (int i = 0; i < size; i++)
                    fname += response[i];
                response = response.Remove(0, size);
                if ((p = response.IndexOf(' ')) != -1) size = int.Parse(response.Substring(0, p));
                else break;
                response = response.Remove(0, p + 1);
                lname = "";
                for (int i = 0; i < size; i++)
                    lname += response[i];
                response = response.Remove(0, size);
                tbFirstName.Text = fname;
                tbLastName.Text = lname;
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
