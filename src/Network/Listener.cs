/***************************************************************************
 *                                Listener.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Listener.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
 *   $Author: krrios $
 *   $Date: 2005/01/22 04:25:04 $
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Network;

namespace Server.Network
{
	public class Listener : IDisposable
	{
		private Socket m_Listener;
		private bool m_Disposed;
		private int m_ThisPort;

		private Queue m_Accepted;

		private AsyncCallback m_OnAccept;

		private static Socket[] m_EmptySockets = new Socket[0];

		public int UsedPort
		{
			get{ return m_ThisPort; }
		}

		private static int m_Port = 2593;

		public static int Port
		{
			get
			{
				return m_Port;
			}
			set
			{
				m_Port = value;
			}
		}

		public Listener( int port )
		{
			m_ThisPort = port;
			m_Disposed = false;
			m_Accepted = new Queue();
			m_OnAccept = new AsyncCallback( OnAccept );

			m_Listener = Bind( IPAddress.Any, port );

			try
			{
				IPHostEntry iphe = Dns.Resolve( Dns.GetHostName() );

				ArrayList list = new ArrayList();
				list.Add( IPAddress.Loopback );

				Console.WriteLine( "Address: {0}:{1}", IPAddress.Loopback, port );

				IPAddress[] ips = iphe.AddressList;

				for ( int i = 0; i < ips.Length; ++i )
				{
					if ( !list.Contains( ips[i] ) )
					{
						list.Add( ips[i] );

						Console.WriteLine( "Address: {0}:{1}", ips[i], port );
					}
				}
			}
			catch
			{
			}
		}

		private Socket Bind( IPAddress ip, int port )
		{
			IPEndPoint ipep = new IPEndPoint( ip, port );

			Socket s = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

			try
			{
				s.Bind( ipep );
				s.Listen( 300 );

				s.BeginAccept( m_OnAccept, s );

				return s;
			}
			catch
			{
				try { s.Shutdown( SocketShutdown.Both ); } 
				catch{}

				try { s.Close(); }
				catch{}

				return null;
			}
		}

		private void OnAccept( IAsyncResult asyncResult )
		{
			try
			{
				Socket s = m_Listener.EndAccept( asyncResult );
				
				SocketConnectEventArgs e = new SocketConnectEventArgs( s );
				EventSink.InvokeSocketConnect( e );

				if ( e.AllowConnection )
				{
					lock ( m_Accepted.SyncRoot )
						m_Accepted.Enqueue( s );
				}
				else
				{
					try{ s.Shutdown( SocketShutdown.Both ); }
					catch{}

					try{ s.Close(); }
					catch{}
				}
			}
			catch
			{
			}
			finally
			{
				m_Listener.BeginAccept( m_OnAccept, m_Listener );
			}
		}

		public Socket[] Slice()
		{
			lock ( m_Accepted.SyncRoot )
			{
				if ( m_Accepted.Count == 0 )
					return m_EmptySockets;

				object[] array = m_Accepted.ToArray();
				m_Accepted.Clear();

				Socket[] sockets = new Socket[array.Length];

				Array.Copy( array, sockets, array.Length );

				return sockets;
			}
		}

		public void Dispose()
		{
			if ( !m_Disposed )
			{
				m_Disposed = true;

				if ( m_Listener != null )
				{
					try { m_Listener.Shutdown( SocketShutdown.Both ); }
					catch {}

					try { m_Listener.Close(); }
					catch {}

					m_Listener = null;
				}
			}
		}
	}
}