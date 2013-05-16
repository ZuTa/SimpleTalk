using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.ComponentModel;
using System.Windows;

namespace SimpleTalk.Client
{
    #region Delegates
    public delegate void LoginResult(bool flag);
    public delegate void CheckLoginResult(bool flag);
    public delegate void GetUsersByPatternResult(string response);
    public delegate void RequestForSystemMessagesResult(string response);
    public delegate void AcceptedFriendsRequestsResult(string response);
    public delegate void RequestForFriendsListResult(string response);
    public delegate void OnMessageReceived(string message, int fromID);
    public delegate void ServerOffline();
    public delegate void OnHistoryReceived(string response);
    public delegate void OnInformationReceived(string response);
    public delegate void OnDeletedFriend(int ID);
    #endregion


    #region Enums

    public enum UserStatuses
    {
        Online,
        Offline
    }

    public enum MessageType
    {
        System,
        Friend
    }

    public enum ServerCommands
    {
        Login,
        CheckLogin,
        GetUsersByPattern,
        ChangeUserStatus,
        FriendRequest,
        RequestForSystemMessages,
        ReadMessages,
        AcceptedFriendsRequests,
        RequestForFriendsList,
        Message,
        RequestForOfflineMessages,
        DeleteMessages,
        GetHistory,
        GetInformation,
        DeleteFriend
    }

    public enum ClientCommands
    {
        Login,  
        CheckLogin,
        GetUsersByPattern,
        ChangeUserStatus,
        FriendRequest,
        RequestForSystemMessages,
        ReadMessages,
        SuccessfulFriendRequest,
        RequestForFriendsList,
        Message,
        ServerOffline,
        GetHistory,
        GetInformation,
        DeleteFriend
    }

    public enum ServerConnectResponses
    {
        Successful,
        AlreadyLogin,
        Unsuccessful
    }

    public enum ServerCheckResponses
    {
        YES,
        NO,
        Error
    }

    public enum ServerGetUsersResponses
    {
        OK,
        Error
    }

    #endregion

    internal static class ClientHelper
    {
        #region Fields and Properties
        private static readonly int bufferSize = 8192;
        private static Socket clientSocket = null;
        private static IPAddress address;
        private static byte[] buffer;
        private static List<byte> listOfBytes;
        private static IAsyncResult currentAynchResult;

        private static LoginResult loginresult;
        public static LoginResult LoginResult
        {
            get { return loginresult; }
            set { loginresult = value; }
        }
        private static CheckLoginResult checkloginresult;
        public static CheckLoginResult CheckLoginResult
        {
            get { return checkloginresult; }
            set { checkloginresult = value; }
        }
        private static GetUsersByPatternResult getusersbypatternresult;
        public static GetUsersByPatternResult GetUsersByPatternResult
        {
            get { return getusersbypatternresult; }
            set { getusersbypatternresult = value; }
        }
        private static RequestForSystemMessagesResult requestforsystemmessagesresult;
        public static RequestForSystemMessagesResult RequestForSystemMessagesResult
        {
            get { return requestforsystemmessagesresult; }
            set { requestforsystemmessagesresult = value; }
        }
        private static AcceptedFriendsRequestsResult acceptedfriendsrequestsresult;
        public static AcceptedFriendsRequestsResult AcceptedFriendsRequestsResult
        {
            get { return acceptedfriendsrequestsresult; }
            set { acceptedfriendsrequestsresult = value; }
        }
        private static RequestForFriendsListResult requestforfriendslistresult;
        public static RequestForFriendsListResult RequestForFriendsListResult
        {
            get { return requestforfriendslistresult; }
            set { requestforfriendslistresult = value; }
        }
        private static OnMessageReceived onmessagereceived;
        public static OnMessageReceived OnMessageReceived
        {
            get { return onmessagereceived; }
            set { onmessagereceived = value; }
        }
        private static ServerOffline serveroffline;
        public static ServerOffline ServerOffline
        {
            get { return serveroffline; }
            set { serveroffline = value; }
        }
        private static OnHistoryReceived onhistoryreceived;
        public static OnHistoryReceived OnHistoryReceived
        {
            get { return onhistoryreceived; }
            set { onhistoryreceived = value; }
        }
        private static OnInformationReceived oninformationreceived;
        public static OnInformationReceived OnInformationReceived
        {
            get { return oninformationreceived; }
            set { oninformationreceived = value; }
        }
        private static OnDeletedFriend ondeletedfriend;
        public static OnDeletedFriend OnDeletedFriend
        {
            get { return ondeletedfriend; }
            set { ondeletedfriend = value; }
        }
        #endregion

        #region Constructors
        static ClientHelper()
        {
            ConnectToServer();
        }
        #endregion

        #region Methods

        private static void ConnectToServer()
        {           
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Properties.Settings.Default.Server);
                address = (from h in host.AddressList
                            where h.AddressFamily == AddressFamily.InterNetwork
                            select h).First();
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    clientSocket.Connect(address.ToString(), Properties.Settings.Default.ServerPort);
                }
                catch
                {
                    throw new Exception(Properties.Resources.ErrorConnetingToServer);
                }
                buffer = new byte[bufferSize];
                listOfBytes = new List<byte>();
                BeginReceive(clientSocket);
            }
            catch { }
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            try
            {
                int bytesRead = socket.EndReceive(result);
                if (bytesRead > 0)
                {
                    //Begin
                    //Adding received bytes to list of bytes for current connection
                    for (int i = 0; i < bytesRead; i++)
                        listOfBytes.Add(buffer[i]);
                    while (listOfBytes.Count > 5)
                    {
                        int size = BitConverter.ToInt32(listOfBytes.ToArray(), 0);
                        Int16 command = BitConverter.ToInt16(listOfBytes.ToArray(), 4);
                        if (size <= listOfBytes.Count - 6)
                        {
                            string request = Encoding.GetEncoding(1251).GetString(listOfBytes.ToArray(), 6, size);
                            WorkWith(request, (ClientCommands)command);
                            listOfBytes.RemoveRange(0, size + 6);
                        }
                        else
                            break;
                    }
                    //End
                    if (socket.Connected)
                    {
                        buffer = new byte[bufferSize];
                        BeginReceive(socket);
                    }
                }
            }
            catch (SocketException ex)
            {
                socket.Close();
                serveroffline();
            }
            catch
            {
                socket.Close();
                serveroffline();
                MessageBox.Show(Properties.Resources.ErrorLostConnection, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private static void WorkWith(string request, ClientCommands command)
        {
            string[] requests = request.Split('~');

            switch (command)
            {
                case ClientCommands.Login:
                    loginresult(IsSuccessfulLogin(requests[1]));
                    break;
                case ClientCommands.CheckLogin:
                    checkloginresult(IsSuitable(requests[1]));
                    break;
                case ClientCommands.GetUsersByPattern:
                    getusersbypatternresult(ReturnStringOfUsers(requests[1]));
                    break;
                case ClientCommands.ChangeUserStatus:
                    // do nothing
                    break;
                case ClientCommands.FriendRequest:
                    // do nothing
                    break;
                case ClientCommands.RequestForSystemMessages:
                    requestforsystemmessagesresult(requests[1]);
                    break;
                case ClientCommands.ReadMessages:
                    // do nothing
                    break;
                case ClientCommands.SuccessfulFriendRequest:
                    acceptedfriendsrequestsresult(requests[1]);
                    break;
                case ClientCommands.RequestForFriendsList:
                    requestforfriendslistresult(requests[1]);
                    break;
                case ClientCommands.Message:
                    onmessagereceived(Concatinations(request, 2), int.Parse(requests[1]));
                    break;
                case ClientCommands.ServerOffline:
                    serveroffline();
                    break;
                case ClientCommands.GetHistory:
                    onhistoryreceived(requests[1]);
                    break;
                case ClientCommands.GetInformation:
                    oninformationreceived(requests[1]);
                    break;
                case ClientCommands.DeleteFriend:
                    ondeletedfriend(int.Parse(requests[1]));
                    break;
                default:
                    break;
            }
        }

        private static string Concatinations(string m, int start)
        {
            int j = 0;
            while (start > 0)
            {
                if (m[j] == '~') start--;
                j++;
            }
            string s = "";
            for (int i = j; i < m.Length; i++)
                s += m[i];
            return s;
        }

        private static string ReturnStringOfUsers(string buffer)
        {
            string[] responses = buffer.Split('?');
            ServerGetUsersResponses response = (ServerGetUsersResponses)int.Parse(responses[0]);

            switch (response)
            {
                case ServerGetUsersResponses.OK:
                    return responses[1];
                case ServerGetUsersResponses.Error:
                    throw new Exception(Properties.Resources.ErrorGettingUsers);
                default:
                    break;
            }
            return "";

        }

        private static void BeginReceive(Socket socket)
        {
            currentAynchResult = socket.BeginReceive(
               buffer,
               0,
               buffer.Length,
               SocketFlags.None,
               new AsyncCallback(ReceiveCallback),
               socket);
        }
      
        private static void Request(string request, ServerCommands command)
        {
            try
            {
                List<byte> requestBuffer = new List<byte>();
                requestBuffer.AddRange(BitConverter.GetBytes((int)request.Length));
                requestBuffer.AddRange(BitConverter.GetBytes((Int16)command));
                requestBuffer.AddRange(Encoding.GetEncoding(1251).GetBytes(request));
                
                // change implementation
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        clientSocket.Send(requestBuffer.ToArray(), requestBuffer.Count, SocketFlags.None);
                        return;
                    }
                    catch { }
                }
                throw new Exception();
            }
            catch
            {
                MessageBox.Show(Properties.Resources.ErrorConnetingToServer, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
               
        internal static void SendLogin(string login, string password)
        {
            if (clientSocket == null || !clientSocket.Connected) ConnectToServer();
            string request = string.Format(" {0} {1} ", login, password);
            Request(request, ServerCommands.Login);
        }

        private static bool IsSuccessfulLogin(string buffer)
        {
            bool result = false;
            ServerConnectResponses response = (ServerConnectResponses)int.Parse(buffer);
            switch (response)
            {
                case ServerConnectResponses.Successful:
                    result = true;
                    break;
                case ServerConnectResponses.AlreadyLogin:
                    result = false;
                    MessageBox.Show(Properties.Resources.txtUserAlreadyLogged, Properties.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    break;
                case ServerConnectResponses.Unsuccessful:
                default:
                    result = false;
                    break;
            }
            return result;
        }
        
        internal static void CheckLogin(string login, string passwrod, string fname, string lname)
        {
            if (clientSocket == null || !clientSocket.Connected) ConnectToServer();
            string request = string.Format(" {0} {1} {2} {3} ", login, passwrod, fname, lname);
            Request(request, ServerCommands.CheckLogin);
        }
        
        private static bool IsSuitable(string buffer)
        {
            bool result = false;
            ServerCheckResponses response = (ServerCheckResponses)int.Parse(buffer);
            switch (response)
            {
                case ServerCheckResponses.YES:
                    result = true;
                    break;
                case ServerCheckResponses.Error:
                case ServerCheckResponses.NO:
                default:
                    result = false;
                    break;
            }
            return result;
        }

        internal static void GetUsersByPattern(string pattern)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                string request = string.Format(" {0} ", pattern);
                Request(request, ServerCommands.GetUsersByPattern);
            }
        }

        internal static void ChangeUserStatus(int newUserStatus)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                string request = string.Format(" {0} ", newUserStatus);
                Request(request, ServerCommands.ChangeUserStatus);
            }
        }

        internal static void SendRequestAskFriend(int toID)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                string request = string.Format(" {0} ", toID);
                Request(request, ServerCommands.FriendRequest);
            }
        }

        internal static void SendRequestForSystemMessages()
        {
            if (clientSocket != null && clientSocket.Connected)
                Request("", ServerCommands.RequestForSystemMessages);
        }

        internal static void SendReadMessages(string request)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                request = request.Remove(request.Length - 1, 1);
                request = string.Format(" {0} ", request);
                Request(request, ServerCommands.ReadMessages);
            }
        }

        internal static void SendAcceptedFriendsRequests(string request)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                request = request.Remove(request.Length - 1, 1);
                request = string.Format(" {0} ", request);
                Request(request, ServerCommands.AcceptedFriendsRequests);
            }
        }

        internal static void SendRequestForFriendsList()
        {
            if (clientSocket != null && clientSocket.Connected)
                Request("", ServerCommands.RequestForFriendsList);
        }

        internal static void SendMessage(string message, int id)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                string request = string.Format(" {0} {1} ", id, message);
                Request(request, ServerCommands.Message);
            }
        }

        internal static void SendRequestForOfflineMessages()
        {
            if (clientSocket != null && clientSocket.Connected)
                Request("", ServerCommands.RequestForOfflineMessages);
        }

        internal static void SendDeleteMessages(string request)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                request = request.Remove(request.Length - 1, 1);
                request = string.Format(" {0} ", request);
                Request(request, ServerCommands.DeleteMessages);
            }
        }

        internal static void GetHistory(int withID)
        {
            if (clientSocket != null && clientSocket.Connected)
                Request(string.Format(" {0} ", withID), ServerCommands.GetHistory);
        }

        internal static void GetInformation(int forID)
        {
            if (clientSocket != null && clientSocket.Connected)
                Request(string.Format(" {0} ", forID), ServerCommands.GetInformation);
        }

        internal static void DeleteFriend(int userID)
        {
            if (clientSocket != null && clientSocket.Connected)
                Request(string.Format(" {0} ", userID), ServerCommands.DeleteFriend);
        }

        #endregion

    }
}
