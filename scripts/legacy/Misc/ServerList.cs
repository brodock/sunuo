using System;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Network;

namespace Server.Misc
{
	public class ServerList
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static string ServerName {
			get {
				string name = Core.Config.ServerName;
				if (name == null)
					name = "SunUO Test";
				return name;
			}
		}

		public static void Initialize()
		{
			Listener.Port = 2593;

			EventSink.ServerList += new ServerListEventHandler( EventSink_ServerList );
		}

		public static void EventSink_ServerList( ServerListEventArgs e )
		{
			try
			{
				if (Core.Config.GameServers.Count == 0) {
					e.AddServer( ServerName, (IPEndPoint)e.State.Socket.LocalEndPoint );
				} else {
					foreach (Config.GameServer gs in Core.Config.GameServers)
						e.AddServer(gs.Name, gs.Address);
				}
			}
			catch (Exception ex)
			{
				log.Fatal(ex);
				e.Rejected = true;
			}
		}
	}
}