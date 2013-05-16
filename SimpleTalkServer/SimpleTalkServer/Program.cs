using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace SimpleTalkServer
{
    class Program
    {
        private static Server server;

        static void Main(string[] args)
        {
            try
            {
                SqlRequests.SetAllUsersOffline();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(Server_Closing);
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(Server_Closing);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Server_Closing);

                server = new Server();
                server.Start();
                Console.ReadKey();
                server.CloseAllConnections();
            }
            catch
            {
                SqlRequests.SetAllUsersOffline();
            }
            finally
            {
                SqlRequests.CloseConnection();
            }
        }

        private static void Server_Closing(object sender, EventArgs e)
        {
            server.CloseAllConnections();
            SqlRequests.SetAllUsersOffline();
        }

    }
}
