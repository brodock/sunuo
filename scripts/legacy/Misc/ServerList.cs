using System;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Network;

namespace Server.Misc
{
	public class ServerList
	{
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
				GameServerListConfig gsl = Core.Config.GameServerListConfig;
				if (gsl == null || gsl.GameServers.Count == 0) {
					e.AddServer( ServerName, (IPEndPoint)e.State.Socket.LocalEndPoint );
				} else {
					foreach (GameServerConfig gs in gsl.GameServers)
						e.AddServer(gs.Name, gs.Address);
				}
			}
			catch
			{
				e.Rejected = true;
			}
		}
	}
}