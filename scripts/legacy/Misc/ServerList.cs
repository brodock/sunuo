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
					IPAddress ipAddr;

					if ( Resolve( Dns.GetHostName(), out ipAddr ) )
						e.AddServer( ServerName, new IPEndPoint( ipAddr, Listener.Port ) );
					else
						e.Rejected = true;
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

		public static bool Resolve( string addr, out IPAddress outValue )
		{
			try
			{
				outValue = IPAddress.Parse( addr );
				return true;
			}
			catch
			{
				try
				{
					IPHostEntry iphe = Dns.Resolve( addr );

					if ( iphe.AddressList.Length > 0 )
					{
						outValue = iphe.AddressList[iphe.AddressList.Length - 1];
						return true;
					}
				}
				catch
				{
				}
			}

			outValue = IPAddress.None;
			return false;
		}
	}
}