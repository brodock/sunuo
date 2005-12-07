/***************************************************************************
 *                                EventSink.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *   $Author: krrios $
 *   $Date$
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
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Server;
using Server.Accounting;
using Server.Network;

namespace Server
{
	public delegate void ServerListEventHandler( ServerListEventArgs e );
	public delegate void CrashedEventHandler( CrashedEventArgs e );
	public delegate void ShutdownEventHandler( ShutdownEventArgs e );
	public delegate void SocketConnectEventHandler( SocketConnectEventArgs e );
	public delegate void AccountLoginEventHandler( AccountLoginEventArgs e );
	public delegate void DeleteRequestEventHandler( DeleteRequestEventArgs e );
	public delegate void ServerStartedEventHandler();

	public class DeleteRequestEventArgs : EventArgs
	{
		private NetState m_State;
		private int m_Index;

		public NetState State{ get{ return m_State; } }
		public int Index{ get{ return m_Index; } }

		public DeleteRequestEventArgs( NetState state, int index )
		{
			m_State = state;
			m_Index = index;
		}
	}

	public class AccountLoginEventArgs : EventArgs
	{
		private NetState m_State;
		private string m_Username;
		private string m_Password;

		private bool m_Accepted;
		private ALRReason m_RejectReason;

		public NetState State{ get{ return m_State; } }
		public string Username{ get{ return m_Username; } }
		public string Password{ get{ return m_Password; } }
		public bool Accepted{ get{ return m_Accepted; } set{ m_Accepted = value; } }
		public ALRReason RejectReason{ get{ return m_RejectReason; } set{ m_RejectReason = value; } }

		public AccountLoginEventArgs( NetState state, string un, string pw )
		{
			m_State = state;
			m_Username = un;
			m_Password = pw;
		}
	}

	public class SocketConnectEventArgs : EventArgs
	{
		private Socket m_Socket;
		private bool m_AllowConnection;

		public Socket Socket{ get{ return m_Socket; } }
		public bool AllowConnection{ get { return m_AllowConnection; } set { m_AllowConnection = value; } }

		public SocketConnectEventArgs( Socket s )
		{
			m_Socket = s;
			m_AllowConnection = true;
		}
	}

	public class ShutdownEventArgs : EventArgs
	{
		public ShutdownEventArgs()
		{
		}
	}

	public class CrashedEventArgs : EventArgs
	{
		private Exception m_Exception;
		private bool m_Close;

		public Exception Exception{ get{ return m_Exception; } }
		public bool Close{ get{ return m_Close; } set{ m_Close = value; } }

		public CrashedEventArgs( Exception e )
		{
			m_Exception = e;
		}
	}

	public class ServerListEventArgs : EventArgs
	{
		private NetState m_State;
		private IAccount m_Account;
		private bool m_Rejected;
		private ArrayList m_Servers;

		public NetState State{ get{ return m_State; } }
		public IAccount Account{ get{ return m_Account; } }
		public bool Rejected{ get{ return m_Rejected; } set{ m_Rejected = value; } }
		public ArrayList Servers{ get{ return m_Servers; } }

		public void AddServer( string name, IPEndPoint address )
		{
			AddServer( name, 0, TimeZone.CurrentTimeZone, address );
		}

		public void AddServer( string name, int fullPercent, TimeZone tz, IPEndPoint address )
		{
			m_Servers.Add( new ServerInfo( name, fullPercent, tz, address ) );
		}

		public ServerListEventArgs( NetState state, IAccount account )
		{
			m_State = state;
			m_Account = account;
			m_Servers = new ArrayList();
		}
	}

	public class EventSink
	{
		public static event ServerListEventHandler ServerList;
		public static event CrashedEventHandler Crashed;
		public static event ShutdownEventHandler Shutdown;
		public static event SocketConnectEventHandler SocketConnect;
		public static event AccountLoginEventHandler AccountLogin;
		public static event DeleteRequestEventHandler DeleteRequest;
		public static event ServerStartedEventHandler ServerStarted;

		public static void InvokeServerStarted()
		{
			if ( ServerStarted != null )
				ServerStarted();
		}

		public static void InvokeAccountLogin( AccountLoginEventArgs e )
		{
			if ( AccountLogin != null )
				AccountLogin( e );
		}

		public static void InvokeSocketConnect( SocketConnectEventArgs e )
		{
			if ( SocketConnect != null )
				SocketConnect( e );
		}

		public static void InvokeShutdown( ShutdownEventArgs e )
		{
			if ( Shutdown != null )
				Shutdown( e );
		}

		public static void InvokeCrashed( CrashedEventArgs e )
		{
			if ( Crashed != null )
				Crashed( e );
		}

		public static void InvokeServerList( ServerListEventArgs e )
		{
			if ( ServerList != null )
				ServerList( e );
		}

		public static void Reset()
		{
			ServerList = null;
			Crashed = null;
			Shutdown = null;
			SocketConnect = null;
			AccountLogin = null;
			DeleteRequest = null;
		}
	}
}
