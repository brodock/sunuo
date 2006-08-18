using System;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Network;

namespace Server.Misc
{
	public class ServerList
	{
		public const string ServerName = "RunUO Test Center";

		public static void Initialize()
		{
			Listener.Port = 2593;

			EventSink.ServerList += new ServerListEventHandler( EventSink_ServerList );
		}

		public static void EventSink_ServerList( ServerListEventArgs e )
		{
			try
			{
				IPAddress ipAddr;

				if ( Resolve( Dns.GetHostName(), out ipAddr ) )
					e.AddServer( ServerName, new IPEndPoint( ipAddr, Listener.Port ) );
				else
					e.Rejected = true;
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