using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace SimpleTalkServer
{
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

    public enum MessageState
    {
        Sent,
        Received
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
        DeleteFriend,
        AskSendFile,
        ResponseAskSendFile,
        SendPacket,
        Offline,
        Knock
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
        DeleteFriend,
        AskSendFile,
        ResponseAskSendFile,
        SendPacket,
        Knock
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

    public enum ClientChangeStatusResponses
    {
        OK,
        CloseConnection
    }

    public enum ServerAskSendFileResponses
    {
        Yes,
        No
    }

    #endregion

    internal static class ServerHelper
    {
        internal static ServerConnectResponses DoLogin(string login, string password, out int id)
        {
            ServerConnectResponses result = ServerConnectResponses.Unsuccessful;
            id = SqlRequests.FindUser(login, password);
            if (id > 0)
                result = ServerConnectResponses.Successful;
            return result;
        }

        internal static ServerCheckResponses DoCheckLogin(string login)
        {
            ServerCheckResponses result = ServerCheckResponses.YES;
            if (SqlRequests.FindUser(login) > 0)
                result = ServerCheckResponses.NO;
            return result;
        }

        internal static ServerGetUsersResponses GetUsersByPattern(string pattern, ref StringBuilder responseString)
        {
            ServerGetUsersResponses result = ServerGetUsersResponses.OK;
            responseString.Append(((int)result).ToString());
            responseString.Append('?');
            try
            {
                responseString.Append(SqlRequests.GetUsersByPattern(pattern));
            }
            catch
            {
                result = ServerGetUsersResponses.Error;
            }
            return result;
        }

        internal static void SetFriendRequest(int userID, int toID, int state, int type)
        {
            if (!SqlRequests.IsExistMessage(userID, toID, state, type, "")) 
                SqlRequests.AddMessage(userID, toID, state, type, "");
        }

        internal static bool SetMessage(int userID, int toID, int state, int type, string message)
        {
            return SqlRequests.AddMessage(userID, toID, state, type, message);
        }

        internal static void WorkWithAcceptedFriendRequests(int userID, string idString, out List<int> IDs)
        {
            IDs = new List<int>();
            string[] listOfID = idString.Split(',');
            foreach (string s in listOfID)
            {
                int id = int.Parse(s);
                if (!SqlRequests.IsExistFriendship(userID, id))
                {
                    SqlRequests.AddFriendship(userID, id);
                    IDs.Add(id);
                }
            }
        }

    }
}
