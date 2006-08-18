using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Misc;

namespace Server
{
	public class AccessRestrictions
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void Initialize()
		{
			EventSink.SocketConnect += new SocketConnectEventHandler( EventSink_SocketConnect );
		}
			
		private static void EventSink_SocketConnect( SocketConnectEventArgs e )
		{
			try
			{
				IPAddress ip = ((IPEndPoint)e.Socket.RemoteEndPoint).Address;

				if ( Firewall.IsBlocked( ip ) )
				{
					log.Error(String.Format("Client: {0}: Firewall blocked connection attempt.", ip));
					e.AllowConnection = false;
					return;
				}
				else if ( IPLimiter.SocketBlock && !IPLimiter.Verify( ip ) )
				{
					log.Error(String.Format("Client: {0}: Past IP limit threshold", ip));
	
					e.AllowConnection = false;
					return;
				}
			}
			catch
			{
				e.AllowConnection = false;
			}
		}
	}
}