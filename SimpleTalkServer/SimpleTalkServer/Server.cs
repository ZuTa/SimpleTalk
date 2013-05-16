using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Data.OleDb;

namespace SimpleTalkServer
{
    class Server
    {
        private class ConnectionInfo
        {
            public Socket socket;
            public byte[] buffer;
            public List<byte> listOfBytes;
            public int UserID;
            public int PreviousID;
        }

        #region Fields and Properties

        private Socket serverSocket;
        private List <ConnectionInfo> connections = new List<ConnectionInfo>();
        private const int bufferSize = 8192;
        private const int maxMessage = 800;

        #endregion

        #region Constructors
        #endregion

        #region Methods

        public void Start()
        {
            SetupServerSocket();
            //TODO: maybe enough only one call ? 
            for (int i = 0; i < 10; i++)
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
        }

        public void CloseAllConnections()
        {
            foreach (ConnectionInfo ci in connections)
            {
                SqlRequests.ChangeUserStatus(UserStatuses.Offline, ci.UserID);
                ci.socket.Close();
            }
        }

        private void CloseConnection(ConnectionInfo ci)
        {
            if (ci != null)
            {
                SqlRequests.ChangeUserStatus(UserStatuses.Offline, ci.UserID);
                NotifyAllThatUserChangedStatus(ci.UserID);
                if (ci.socket.Connected)
                {                    
                    SendCommandTo(ClientCommands.ServerOffline, ci);
                    ci.socket.Close();
                }
                lock (connections) connections.Remove(ci);
            }
        }

        private void SetupServerSocket()
        {
            IPEndPoint myEndpoint = new IPEndPoint(IPAddress.Any, Properties.Settings.Default.Port);
            serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            serverSocket.Bind(myEndpoint);
            serverSocket.Listen((int)SocketOptionName.MaxConnections);
            Console.WriteLine("Server is running...");
        }

        private void AcceptCallback(IAsyncResult result)
        {
            ConnectionInfo connection = new ConnectionInfo();
            try
            {
                Socket s = (Socket)result.AsyncState;
                connection.socket = s.EndAccept(result);
                connection.buffer = new byte[bufferSize];
                connection.listOfBytes = new List<byte>();
                lock (connections) connections.Add(connection);

                Console.WriteLine("Connected to {0}", connection.socket.RemoteEndPoint.ToString());

                connection.socket.BeginReceive(
                    connection.buffer,
                    0,
                    connection.buffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(ReceiveCallback),
                    connection);
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), result.AsyncState);
            }
            catch (SocketException exc)
            {
                CloseConnection(connection);
                Console.WriteLine("Connection closed");
                Console.WriteLine("Socket exception: " + exc.SocketErrorCode);
            }
            catch (Exception exc)
            {
                CloseConnection(connection);
                Console.WriteLine("Connection closed");
                Console.WriteLine("Exception: " + exc);
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ConnectionInfo connection = (ConnectionInfo)result.AsyncState;
            try
            {
                int bytesRead = connection.socket.EndReceive(result);
                if (bytesRead > 0)
                {
                    //Begin
                    //Adding received bytes to list of bytes for current connection
                    for (int i = 0; i < bytesRead; i++)
                        connection.listOfBytes.Add(connection.buffer[i]);
                    while (connection.listOfBytes.Count > 5)
                    {
                        int size = BitConverter.ToInt32(connection.listOfBytes.ToArray(), 0);
                        Int16 command = BitConverter.ToInt16(connection.listOfBytes.ToArray(), 4);
                        if (size <= connection.listOfBytes.Count - 6)
                        {
                            string request = Encoding.GetEncoding(1251).GetString(connection.listOfBytes.ToArray(), 6, size);
                            WorkWith(connection, (ServerCommands)command, request);
                            connection.listOfBytes.RemoveRange(0, size + 6);
                        }
                        else
                            break;
                    }
                    //End

                    if (connection.socket.Connected)
                    {
                        connection.buffer = new byte[bufferSize];
                        connection.socket.BeginReceive(
                            connection.buffer,
                            0,
                            connection.buffer.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallback),
                            connection);
                    }
                }
                else CloseConnection(connection);
            }
            catch (SocketException exc)
            {
                CloseConnection(connection);
                Console.WriteLine("Connection closed");
                Console.WriteLine("Socket exception: " + exc.SocketErrorCode);
            }
            catch (Exception exc)
            {
                SendCommandToAll(ClientCommands.ServerOffline);
                CloseAllConnections();
                Console.WriteLine("Connection closed");
                Console.WriteLine("Exception: " + exc);
            }
        }

        private void SendCommandToAll(ClientCommands command)
        {
            byte[] response = new byte[bufferSize];
            response = ConvertToBytes("", command);

            foreach (ConnectionInfo connection in connections)
            {
                SendBySocket(connection, response);
            }
        }
        
        private void SendCommandTo(ClientCommands command, ConnectionInfo ci)
        {
            byte[] response = new byte[bufferSize];
            response = ConvertToBytes("", command);
            SendBySocket(ci, response);
        }

        private void WorkWith(ConnectionInfo connection, ServerCommands command, string request)
        {
            string[] requests = request.Split(' ');
            Console.WriteLine("COMMAND : " + command);
            byte[] response = null;

            switch (command)
            {
                case ServerCommands.Login:
                    if (!IsLoginSuccessful(requests[1], requests[2], out connection.UserID, out response))
                    {
                        SendBySocket(connection, response);
                    }
                    else
                    {
                        if (connection.PreviousID > 0)
                            SqlRequests.ChangeUserStatus(UserStatuses.Offline, connection.PreviousID);
                        ChangeUserStatus(UserStatuses.Online, connection);
                        SendBySocket(connection, response);
                        connection.PreviousID = connection.UserID;
                    }
                    break;
                case ServerCommands.CheckLogin:
                    if (IsSuitable(requests[1], out response))
                        SqlRequests.AddUser(requests[1], requests[2], requests[3], requests[4]);
                    SendBySocket(connection, response);

                    break;
                case ServerCommands.GetUsersByPattern:
                    GetUsersByPattern(requests[1], out response);
                    SendBySocket(connection, response);

                    Console.WriteLine("Sent list of users for {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.ChangeUserStatus:
                    Console.WriteLine("Changed status for {0}", connection.socket.RemoteEndPoint.ToString());
                    ChangeUserStatus((UserStatuses)int.Parse(requests[1]), connection);

                    break;
                case ServerCommands.FriendRequest:
                    SetFriendRequest(connection.UserID, int.Parse(requests[1]));

                    Console.WriteLine("Add friend request from {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.RequestForSystemMessages:
                    GetMessages(connection.UserID, (int)MessageType.System, (int)MessageState.Sent, out response);
                    Console.WriteLine("Sending system messages to {0}", connection.socket.RemoteEndPoint.ToString());
                    SendBySocket(connection, response);

                    break;
                case ServerCommands.ReadMessages:
                    SqlRequests.SetStateForMessages(requests[1], (int)MessageState.Received);

                    Console.WriteLine("Setting state to RECEIVED to messages");
                    break;
                case ServerCommands.AcceptedFriendsRequests:
                    List<int> IDs;
                    ServerHelper.WorkWithAcceptedFriendRequests(connection.UserID, requests[1], out IDs);
                    SendNotificationToUsers(IDs, connection.UserID);

                    Console.WriteLine("Added new friendship and sent notifications");
                    break;
                case ServerCommands.RequestForFriendsList:
                    GetFriendsList(connection.UserID, out response);
                    SendBySocket(connection, response);

                    Console.WriteLine("Sending friend's list to {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.Message:
                    Console.WriteLine("Sent message from {0}", connection.socket.RemoteEndPoint.ToString());
                    SendMessage(connection.UserID, int.Parse(requests[1]), Concatinations(request, 2));
                    break;
                case ServerCommands.RequestForOfflineMessages:
                    GetOfflineMessagesAndSend(connection.UserID);

                    break;
                case ServerCommands.DeleteMessages:
                    SqlRequests.DeleteMessages(requests[1]);

                    Console.WriteLine("Deleted messages for {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.GetHistory:
                    SendHistory(int.Parse(requests[1]), connection);

                    Console.WriteLine("Sent history for {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.GetInformation:
                    GetInformation(int.Parse(requests[1]), out response);
                    SendBySocket(connection, response);

                    Console.WriteLine("Sent user's information for {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.DeleteFriend:
                    SqlRequests.DeleteFriend(connection.UserID, int.Parse(requests[1]));
                    SendNoticationTo(connection.UserID, int.Parse(requests[1]));

                    Console.WriteLine("Deleted friendship for {0}", connection.socket.RemoteEndPoint.ToString());
                    break;
                case ServerCommands.AskSendFile:
                    SendAskSendFile(connection.UserID, int.Parse(requests[1]), int.Parse(requests[2]), int.Parse(requests[3]), request);

                    break;
                case ServerCommands.ResponseAskSendFile:
                    SendAcceptedAskSendFile(connection.UserID, int.Parse(requests[1]), (ServerAskSendFileResponses)int.Parse(requests[2]));

                    break;
                case ServerCommands.SendPacket:
                    SendPacket(connection.UserID, int.Parse(requests[1]), int.Parse(requests[2]), request);

                    break;
                case ServerCommands.Offline:
                    CloseConnection(connection);

                    break;
                case ServerCommands.Knock:
                    // donothing
                    break;
                default:
                    break;
            }
        }

        private void SendPacket(int fromID, int toID, int size, string packet)
        {
            for (int i = 0; i < 3; i++)
                packet = packet.Remove(0, packet.IndexOf(' ') + 1);
            packet = packet.Substring(0, size);
            ConnectionInfo ci = GetConnectionByUserID(toID);
            byte[] response = ConvertToBytes(string.Format("~{0}~{1}~{2}~", fromID, size, packet), ClientCommands.SendPacket);
            SendBySocket(ci, response); 
        }

        private void SendAcceptedAskSendFile(int fromID, int toID, ServerAskSendFileResponses resp)
        {
            ConnectionInfo ci = GetConnectionByUserID(toID);
            byte[] response = ConvertToBytes(string.Format("~{0}~{1}~", fromID, (int)resp), ClientCommands.ResponseAskSendFile);
            SendBySocket(ci, response);            
        }

        private void SendAskSendFile(int fromID, int toID, int amountOfPackets, int sizeOfFileName, string fileName)
        {
            for (int i = 0; i < 4; i++)
                fileName = fileName.Remove(0, fileName.IndexOf(' ') +1 );
            fileName = fileName.Substring(0, sizeOfFileName);
            ConnectionInfo ci = GetConnectionByUserID(toID);
            byte[] response = ConvertToBytes(string.Format("~{0}~{1}~{2}~{3}", fromID, amountOfPackets, sizeOfFileName, fileName), ClientCommands.AskSendFile);
            SendBySocket(ci, response);
        }

        private void SendBySocket(ConnectionInfo ci, byte[] response)
        {
            try
            {
                ci.socket.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(SendCallBack), ci.socket);
            }
            catch (Exception ex)
            {
                CloseConnection(ci);
                Console.WriteLine(ex.Message);
            }
        }

        private static void SendCallBack(IAsyncResult iar)
        {
            Socket socket = (Socket)iar.AsyncState;
            try
            {
                socket.EndSend(iar);
            }
            catch (Exception ex)
            {
                socket.Close();
                Console.WriteLine(ex.Message);
            }
        }

        private void ChangeUserStatus(UserStatuses userStatus, ConnectionInfo connection)
        {
            SqlRequests.ChangeUserStatus(userStatus, connection.UserID);
            NotifyAllThatUserChangedStatus(connection.UserID);
        }

        private void SendNoticationTo(int deletedID, int ID)
        {
            foreach (ConnectionInfo ci in connections)
            {
                if (ci.UserID == ID)
                {
                    byte[] response = ConvertToBytes(string.Format("~{0}~", deletedID), ClientCommands.DeleteFriend);
                    SendBySocket(ci, response);
                    break;
                }
            }
        }

        private void SetFriendRequest(int fromID, int toID)
        {
            byte[] response = new byte[bufferSize];
            ServerHelper.SetFriendRequest(fromID, toID, (int)MessageState.Sent, (int)MessageType.System);

            foreach (ConnectionInfo connection in connections)
            {
                if (connection.UserID == toID)
                {
                    GetMessages(connection.UserID, (int)MessageType.System, (int)MessageState.Sent, out response);
                    SendBySocket(connection, response);
                }
            }
        }

        private void GetOfflineMessagesAndSend(int toID)
        {
            string messagesString = SqlRequests.GetMessages(toID, (int)MessageType.Friend, (int)MessageState.Sent).ToString();
            messagesString = messagesString.Remove(0, 1);
            string[] messages = messagesString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder responseReadMessage = new StringBuilder();

            foreach (string message in messages)
            {
                string[] details = message.Split(' ');
                if (details.Length < 6) continue;
                details[5] = Concatinations(message, 5);
                responseReadMessage.Append(details[0] + ",");
                // 0 - messageID
                // 1 - FromID
                // 2 - Login
                // 3 - Date
                // 4 - Time
                // 5 - Message
                SendMessage(int.Parse(details[1]), toID, details[5], details[3] + " " + details[4]);
            }
            if (responseReadMessage.Length > 0) SqlRequests.SetStateForMessages(responseReadMessage.ToString(), (int)MessageState.Received);
        }

        private void NotifyAllThatUserChangedStatus(int ID)
        {
            if (ID == -1) return;
            foreach (ConnectionInfo ci in connections)
            {
                if (ci.UserID != ID)
                {
                    byte[] response;
                    GetFriendsList(ci.UserID, out response);
                    SendBySocket(ci, response);
                }
            }
        }

        private string Concatinations(string m, int start)
        {
            int j = 0;
            while (start > 0)
            {
                if (m[j] == ' ') start--;
                j++;
            }
            string s = "";
            for (int i = j; i < m.Length; i++)
                s += m[i];
            return s;
        }

        private byte[] ConvertToBytes(string message, ClientCommands command)
        {
            //first, add number of bytes
            List<byte> requestBuffer = new List<byte>();
            requestBuffer.AddRange(BitConverter.GetBytes((int)message.Length));
            requestBuffer.AddRange(BitConverter.GetBytes((Int16)command));
            requestBuffer.AddRange(Encoding.GetEncoding(1251).GetBytes(message));
            return requestBuffer.ToArray();
        }

        private byte[] ConvertToBytes(int len, ClientCommands command)
        {
            List<byte> requestBuffer = new List<byte>();
            requestBuffer.AddRange(BitConverter.GetBytes(len));
            requestBuffer.AddRange(BitConverter.GetBytes((Int16)command));
            return requestBuffer.ToArray();
        }

        private byte[] ConvertToBytes(string message)
        {
            return Encoding.GetEncoding(1251).GetBytes(message);
        }

        private byte[] ConvertToBytes(ClientCommands command)
        {
            return BitConverter.GetBytes((Int16)command);
        }

        private void SendMessage(int fromID, int toID, string message)
        {
            byte[] response = new byte[bufferSize];
            string m = string.Format("~{0}~{1}~{2}", fromID, DateTime.Now, message);

            bool send = false;
            foreach (ConnectionInfo connection in connections)
                if (connection.UserID == toID && IsUserOnline(toID))
                {
                    if (ServerHelper.SetMessage(fromID, toID, (int)MessageState.Received, (int)MessageType.Friend, message))
                    {
                        response = ConvertToBytes(m.Length, ClientCommands.Message);
                        SendBySocket(connection, response);
                        while (m.Length > 0)
                        {
                            response = ConvertToBytes(m.Substring(0, Math.Min(m.Length, maxMessage)));
                            SendBySocket(connection, response);
                            m = m.Remove(0, Math.Min(m.Length, maxMessage));
                        }
                        send = true;
                    }
                    else
                    {
                        CloseConnection(connection);                        
                        ConnectionInfo ci = GetConnectionByUserID(fromID);
                        CloseConnection(ci);                        
                        return;
                    }
                }
            if (send) return;
            // if toID is offline
            // add message with status Sent( not Received )
            ServerHelper.SetMessage(fromID, toID, (int)MessageState.Sent, (int)MessageType.Friend, message);
        }

        private ConnectionInfo GetConnectionByUserID(int userID)
        {
            foreach (ConnectionInfo ci in connections)
                if (ci.UserID == userID) return ci;
            return null;
        }

        private void SendMessage(int fromID, int toID, string message, string date)
        {
            // only offline messages
            // so do not save in database
            byte[] response = new byte[bufferSize];
            response = ConvertToBytes(string.Format("~{0}~{1}~{2}", fromID, date, message), ClientCommands.Message);
            foreach (ConnectionInfo connection in connections)
                if (connection.UserID == toID && IsUserOnline(toID))
                {
                    SendBySocket(connection, response);
                }
        }

        private bool IsUserOnline(int userID)
        {
            return (SqlRequests.GetUserStatus(userID) == UserStatuses.Online);
        }

        private void SendNotificationToUsers(List<int> IDs, int userID)
        {
            byte[] response = new byte[bufferSize];
            string command = ((int)ClientCommands.SuccessfulFriendRequest).ToString();
            string userLogin = SqlRequests.GetUserLogin(userID);
            string message = string.Format("~{0} ", userLogin);
            response = ConvertToBytes(message, ClientCommands.SuccessfulFriendRequest);

            // send notification to user who is online
            foreach (ConnectionInfo connection in connections)
            {
                int index = IDs.IndexOf(connection.UserID);
                if (index != -1)
                {
                    SendBySocket(connection, response);
                    IDs.RemoveAt(index);
                }
            }
            // add system messages to user who is offline
            foreach (int id in IDs)
                if (id != -1) SqlRequests.AddMessage(userID, id, (int)MessageState.Sent, (int)MessageType.System, command);
        }

        private bool IsLoginSuccessful(string login, string password, out int id, out byte[] response)
        {
            ServerConnectResponses result = ServerHelper.DoLogin(login, password, out id);
            response = ConvertToBytes(string.Format("~{0}~", ((int)result).ToString()), ClientCommands.Login);
            switch (result)
            {
                case ServerConnectResponses.Successful:
                    Console.WriteLine("Successful login for {0}", login);
                    return true;
                case ServerConnectResponses.AlreadyLogin:
                    Console.WriteLine("Unsuccessful login! Already logged {0}", login);
                    break;
                case ServerConnectResponses.Unsuccessful:
                    Console.WriteLine("Unsuccessful login for {0}", login);
                    break;
                default:
                    break;
            }
            return false;
        }

        private bool IsSuitable(string login, out byte[] response)
        {
            ServerCheckResponses result = ServerHelper.DoCheckLogin(login);
            response = ConvertToBytes(string.Format("~{0}~", ((int)result).ToString()), ClientCommands.CheckLogin);
            switch (result)
            {
                case ServerCheckResponses.YES:
                    Console.WriteLine("Successful registration for {0}", login);
                    return true;
                case ServerCheckResponses.Error:
                    // TODO: define error !!!
                    Console.WriteLine("Unsuccessful registration for {0}", login);
                    break;
                case ServerCheckResponses.NO:
                    Console.WriteLine("Unsuccessful registration for {0}", login);
                    break;
            }
            return false;
        }

        private void GetUsersByPattern(string pattern, out byte[] response)
        {
            StringBuilder responseString = new StringBuilder();
            ServerGetUsersResponses result = ServerHelper.GetUsersByPattern(pattern, ref responseString);
            response = ConvertToBytes(string.Format("~{0}~", responseString.ToString()), ClientCommands.GetUsersByPattern);
            switch (result)
            {
                case ServerGetUsersResponses.OK:
                    Console.WriteLine("Successful! GetUsersByPattern");
                    break;
                case ServerGetUsersResponses.Error:
                    Console.WriteLine("Error! GetUsersByPattern");
                    break;
                default:
                    break;
            }
        }

        private void GetMessages(int userID, int type, int state, out byte[] response)
        {
            StringBuilder responseString = SqlRequests.GetMessages(userID, type, state);
            response = ConvertToBytes(string.Format("~{0}~", responseString.ToString()), ClientCommands.RequestForSystemMessages);
        }

        private void GetFriendsList(int userID, out byte[] response)
        {
            string list = SqlRequests.GetFriendsList(userID).ToString();
            response = ConvertToBytes(string.Format("~{0}~", list), ClientCommands.RequestForFriendsList);
        }

        private void SendHistory(int id1, ConnectionInfo connection)
        {
            byte[] response = new byte[bufferSize];
            StringBuilder responseString = SqlRequests.GetHistory(id1, connection.UserID);
            //send size and command
            response = ConvertToBytes(responseString.Length + 2, ClientCommands.GetHistory); // +2 beacause "~{0}~"
            SendBySocket(connection, response);
            response = ConvertToBytes("~");
            SendBySocket(connection, response);
            while (responseString.Length > 0)
            {
                response = ConvertToBytes(string.Format("{0}", responseString.ToString(0, Math.Min(responseString.Length, maxMessage))));
                SendBySocket(connection, response);
                responseString.Remove(0, Math.Min(responseString.Length, maxMessage));                
            }
            response = ConvertToBytes("~");
            SendBySocket(connection, response);
        }

        private void GetInformation(int id, out byte[] response)
        {
            StringBuilder responseString = SqlRequests.GetInformation(id);
            response = ConvertToBytes(string.Format("~{0}~", responseString.ToString()), ClientCommands.GetInformation);
        }

        #endregion
    }
}