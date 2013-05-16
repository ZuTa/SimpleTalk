using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;

namespace SimpleTalkServer
{
    internal static class SqlRequests
    {
        #region Fields and Properties

        private static OleDbConnection dataBase;

        private static readonly string sqlFindUser = "SELECT [UserID] FROM Users WHERE ([Login]='{0}') AND ([Password]='{1}')"; // AND ([Status]=1)
        private static readonly string sqlFindUserLogin = "SELECT UserID FROM Users WHERE (Login='{0}')";
        private static readonly string sqlAddUserDetails = "INSERT INTO Details(FirstName, LastName) VALUES ('{0}', '{1}')";
        private static readonly string sqlGetDetailID = "SELECT DetailID FROM Details WHERE (FirstName='{0}' AND LastName='{1}')";
        private static readonly string sqlAddUser = "INSERT INTO Users([DetailID], [Login], [Password], [Status]) VALUES ({0}, '{1}', '{2}', {3})";
        private static readonly string sqlGetUsersByPattern = @"
            SELECT UserID, Login, FirstName, LastName, Status 
            FROM Users, Details
            WHERE ((Users.DetailID=Details.DetailID) AND ((Login LIKE '{0}%') OR (FirstName LIKE '{0}%') OR (LastName LIKE '{0}%')))";
        private static readonly string sqlUpdateUserStatus = "UPDATE Users SET Status={0} WHERE UserID={1}";
        private static readonly string sqlAddMessage = "INSERT INTO Messages([FromID], [ToID], [State], [DateTime], [Message], [Type]) VALUES ({0}, {1}, {2}, '{3}', '{4}' ,{5})";
        private static readonly string sqlGetMessages = @"
            SELECT MessageID, FromID, Login, DateTime, Message 
            FROM Users, Messages 
            WHERE ((Messages.ToID={0}) AND (Users.UserID=Messages.FromID) AND (Messages.Type={1}) AND (Messages.State={2}))
            ORDER BY Messages.DateTime";
        private static readonly string sqlUpdateMessagesState = "UPDATE Messages SET State={1} WHERE MessageID IN ({0})";
        private static readonly string sqlAddFriendship = "INSERT INTO Contacts([ID1], [ID2]) VALUES ({0}, {1})";
        private static readonly string sqlGetUserLogin = "SELECT Login FROM Users WHERE UserID={0}";
        private static readonly string sqlGetFriendsList = @"
            SELECT Users.UserID, Users.Login, Users.Status
            FROM Contacts
            INNER JOIN Users ON ((({0}=Contacts.ID1) AND (Users.UserID=Contacts.ID2)) OR (({0}=Contacts.ID2) AND (Users.UserID=Contacts.ID1)))
            ORDER BY Users.Status";
        private static readonly string sqlDeleteMessages = "DELETE FROM Messages WHERE MessageID IN ({0})";
        private static readonly string sqlGetUserStatus = "SELECT Status FROM Users WHERE UserID={0}";
        private static readonly string sqlGetHistory = @"
            SELECT FromID, ToID, DateTime, Message 
            FROM Messages 
            WHERE (((FromID={0}) AND (ToID={1})) OR ((FromID={1}) AND (ToID={0}))) AND (Type=1)
            ORDER BY Messages.DateTime";
        private static readonly string sqlGetInformationAboutUser = "SELECT FirstName, LastName FROM Users, Details WHERE (UserID={0}) AND (Users.DetailID=Details.DetailID)";
        private static readonly string sqlDeleteFriend = "DELETE FROM Contacts WHERE (((ID1={0}) AND (ID2={1})) OR ((ID1={1}) AND (ID2={0})))";
        private static readonly string sqlExistFriendShip = "SELECT ContactID FROM Contacts WHERE (((ID1={0}) AND (ID2={1})) OR ((ID1={1}) AND (ID2={0})))";
        private static readonly string sqlExistMessage = "SELECT MessageID FROM Messages WHERE ((FromID={0}) AND(ToID={1}) AND (State={2}) AND (Message='{3}') AND (Type={4}))";
        private static readonly string sqlSetOfflineToAll = @"
            UPDATE Users
            SET Status=1";
        #endregion

        #region Constructors
        static SqlRequests()
        {
            ConnectToDatabase();
        }
        #endregion

        #region Methods

        private static void ConnectToDatabase()
        {
            string cnStr = @"Provider=Microsoft.ACE.OLEDB.12.0; Data Source=SimpleTalk.accdb";
            try
            {
                dataBase = new OleDbConnection(cnStr);
                dataBase.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void CloseConnection()
        {
            dataBase.Close();
        }

        private static int GetDetailID(string firstName, string lastName)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlGetDetailID, firstName, lastName), dataBase);
            OleDbDataReader reader = comm.ExecuteReader();
            return (reader.Read()) ? (int)reader[0] : -1;
        }

        private static int AddUserDetails(string firstName, string lastName)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlAddUserDetails, firstName, lastName), dataBase);
            return comm.ExecuteNonQuery();
        }

        internal static int FindUser(string login, string password)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlFindUser, login, password), dataBase);
            OleDbDataReader reader = comm.ExecuteReader();
            if(reader.Read())
                return int.Parse(reader[0].ToString());
            else 
                return -1;
        }

        internal static int FindUser(string login)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlFindUserLogin, login), dataBase);
            OleDbDataReader reader = comm.ExecuteReader();
            return (reader.Read()) ? (int)reader[0] : -1;
        }

        internal static int AddUser(string login, string password, string firstName, string lastName)
        {
            AddUserDetails(firstName, lastName);
            int detailID = GetDetailID(firstName, lastName);
            // TODO : add status logic
            OleDbCommand comm = new OleDbCommand(string.Format(sqlAddUser, detailID, login, password, (int)UserStatuses.Offline), dataBase);
            comm.ExecuteNonQuery();
            return FindUser(login);
        }

        internal static StringBuilder GetUsersByPattern(string pattern)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlGetUsersByPattern, pattern), dataBase);
            OleDbDataReader reader = comm.ExecuteReader();
            StringBuilder users = new StringBuilder();
            int count = 0;
            while (reader.Read())
            {
                count++;
                users.Append(count.ToString() + " ");
                users.Append(reader[0].ToString() + " ");
                users.Append(reader[1].ToString() + " ");
                users.Append(reader[2].ToString() + " ");
                users.Append(reader[3].ToString() + " ");
                users.Append(reader[4].ToString());
                users.Append(':');
            }
            return users;
        }

        internal static void ChangeUserStatus(UserStatuses userStatus, int userID)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlUpdateUserStatus, (int)userStatus, userID), dataBase);
                comm.ExecuteNonQuery();
            }
            catch
            {
                throw new Exception("Error updating user status!");
            }            
        }

        internal static bool AddMessage(int fromID, int toID, int state, int type, string message)
        {
            bool result = false;
            try
            {
                OleDbCommand comm =new OleDbCommand(string.Format(sqlAddMessage, fromID, toID, state, DateTime.Now, message, type), dataBase);
                comm.ExecuteNonQuery();
                result = true;
            }
            catch(Exception ex)
            {
                result = false;
                Console.WriteLine("Error adding message to Messages. " + ex.Message);
            }
            return result;
        }

        internal static StringBuilder GetMessages(int userID, int type, int state)
        {
            StringBuilder messages = new StringBuilder();
            messages.Append('?');
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlGetMessages, userID, type, state), dataBase);
                OleDbDataReader reader = comm.ExecuteReader();
                while (reader.Read())
                {
                    messages.Append(reader[0].ToString() + " ");
                    messages.Append(reader[1].ToString() + " ");
                    messages.Append(reader[2].ToString() + " ");
                    messages.Append(reader[3].ToString() + " ");
                    messages.Append(reader[4].ToString());
                    messages.Append(";");
                }
                return messages;
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting message to Messages. " + ex.Message);
            }
        }

        internal static void SetStateForMessages(string messages, int state)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlUpdateMessagesState, messages, state), dataBase);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating message's states " + ex.Message);
            }
        }

        internal static void AddFriendship(int ID1, int ID2)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlAddFriendship, ID1, ID2), dataBase);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding new friendship " + ex.Message);
            }
        }

        internal static string GetUserLogin(int userID)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlGetUserLogin, userID), dataBase);
                OleDbDataReader reader = comm.ExecuteReader();
                if (reader.Read())
                    return reader[0].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro getting user login " + ex.Message);
            }
            return "";
        }

        internal static StringBuilder GetFriendsList(int userID)
        {
            StringBuilder list = new StringBuilder();
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlGetFriendsList, userID), dataBase);
                OleDbDataReader reader = comm.ExecuteReader();
                while (reader.Read())
                {
                    list.Append(reader[0].ToString() + " ");
                    list.Append(reader[1].ToString() + " ");
                    list.Append(reader[2].ToString() + ";");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting friend's list " + ex.Message);
            }
            return list;
        }

        internal static void DeleteMessages(string messages)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlDeleteMessages, messages), dataBase);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting messages " + ex.Message);
            }
        }

        internal static UserStatuses GetUserStatus(int userID)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlGetUserStatus, userID), dataBase);
                OleDbDataReader reader = comm.ExecuteReader();
                if (reader.Read())
                    return (UserStatuses)int.Parse(reader[0].ToString());
                return UserStatuses.Offline;
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting messages " + ex.Message);
            }
        }

        internal static StringBuilder GetHistory(int id1, int id2)
        {
            StringBuilder list = new StringBuilder();
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlGetHistory, id1, id2), dataBase);
                OleDbDataReader reader = comm.ExecuteReader();
                while (reader.Read())
                {
                    list.Append(reader[0].ToString() + " "); // FromID
                    list.Append(reader[1].ToString() + " "); // ToID
                    list.Append(reader[2].ToString() + " "); // DateTime
                    list.Append((reader[3].ToString()).Length + " "); // size of message
                    list.Append(reader[3].ToString()); // message
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting history " + ex.Message);
            }
            return list;
        }

        internal static StringBuilder GetInformation(int id)
        {
            StringBuilder list = new StringBuilder();
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlGetInformationAboutUser, id), dataBase);
                OleDbDataReader reader = comm.ExecuteReader();
                while (reader.Read())
                {
                    list.Append((reader[0].ToString()).Length + " "); // size of FirstName
                    list.Append(reader[0].ToString()); // FirstName
                    list.Append((reader[1].ToString()).Length + " "); // size of LastName
                    list.Append(reader[1].ToString()); // LastName
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting information " + ex.Message);
            }
            return list;
        }

        internal static void DeleteFriend(int ID1,int ID2)
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(string.Format(sqlDeleteFriend, ID1, ID2), dataBase);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting friend " + ex.Message);
            }
        }

        internal static bool IsExistFriendship(int id1, int id2)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlExistFriendShip, id1, id2), dataBase);
            OleDbDataReader reader = comm.ExecuteReader();
            if (reader.Read())
                return true;
            else
                return false;
        }

        internal static bool IsExistMessage(int fromID, int toID, int state, int type, string message)
        {
            OleDbCommand comm = new OleDbCommand(string.Format(sqlExistMessage, fromID, toID, state, message, type), dataBase);
            OleDbDataReader reader = comm.ExecuteReader();
            if (reader.Read())
                return true;
            else
                return false;
        }

        internal static void SetAllUsersOffline()
        {
            try
            {
                OleDbCommand comm = new OleDbCommand(sqlSetOfflineToAll, dataBase);
                comm.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error setting offline for all users "+ ex.Message);
            }
        }

        #endregion
    }
}
