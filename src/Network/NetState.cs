/***************************************************************************
 *                                NetState.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: NetState.cs,v 1.8 2005/01/22 04:25:04 krrios Exp $
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Accounting;
using Server.Network;
using Server.Items;
using Server.Gumps;
using Server.Menus;
using Server.HuePickers;

namespace Server.Network
{
	public interface IPacketEncoder
	{
		void EncodeOutgoingPacket( NetState to, ref byte[] buffer, ref int length );
		void DecodeIncomingPacket( NetState from, ref byte[] buffer, ref int length );
	}

	public delegate void NetStateCreatedCallback( NetState ns );

	public class NetState
	{
		private Socket m_Socket;
		private IPAddress m_Address;
		private ByteQueue m_Buffer;
		private byte[] m_RecvBuffer;
		private SendQueue m_SendQueue;
		private bool m_Seeded;
		private bool m_Running;
		private AsyncCallback m_OnReceive, m_OnSend;
		private MessagePump m_MessagePump;
		private ServerInfo[] m_ServerInfo;
		private IAccount m_Account;
		private Mobile m_Mobile;
		private CityInfo[] m_CityInfo;
		private GumpCollection m_Gumps;
		private HuePickerCollection m_HuePickers;
		private MenuCollection m_Menus;
		private int m_Sequence;
		private bool m_CompressionEnabled;
		private string m_ToString;
		private ArrayList m_Trades;
		private ClientVersion m_Version;
		private bool m_SentFirstPacket;
		private bool m_BlockAllPackets;

		internal int m_Seed;
		internal int m_AuthID;

		public IPAddress Address
		{
			get{ return m_Address; }
		}

		private int m_Flags;

		private static IPacketEncoder m_Encoder;

		public static IPacketEncoder PacketEncoder
		{
			get{ return m_Encoder; }
			set{ m_Encoder = value; }
		}

		private static NetStateCreatedCallback m_CreatedCallback;

		public static NetStateCreatedCallback CreatedCallback
		{
			get{ return m_CreatedCallback; }
			set{ m_CreatedCallback = value; }
		}

		public bool SentFirstPacket{ get{ return m_SentFirstPacket; } set{ m_SentFirstPacket = value; } }

		public bool BlockAllPackets
		{
			get
			{
				return m_BlockAllPackets;
			}
			set
			{
				m_BlockAllPackets = value;
			}
		}

		public int Flags
		{
			get
			{
				return m_Flags;
			}
			set
			{
				m_Flags = value;
			}
		}

		public ClientVersion Version
		{
			get
			{
				return m_Version;
			}
			set
			{
				m_Version = value;
			}
		}

		public ArrayList Trades
		{
			get
			{
				return m_Trades;
			}
		}

		public void ValidateAllTrades()
		{
			for ( int i = m_Trades.Count - 1; i >= 0; --i )
			{
				if ( i >= m_Trades.Count )
					continue;

				SecureTrade trade = (SecureTrade)m_Trades[i];

				if ( trade.From.Mobile.Deleted || trade.To.Mobile.Deleted || !trade.From.Mobile.Alive || !trade.To.Mobile.Alive || !trade.From.Mobile.InRange( trade.To.Mobile, 2 ) || trade.From.Mobile.Map != trade.To.Mobile.Map )
					trade.Cancel();
			}
		}

		public void CancelAllTrades()
		{
			for ( int i = m_Trades.Count - 1; i >= 0; --i )
				if ( i < m_Trades.Count )
					((SecureTrade)m_Trades[i]).Cancel();
		}

		public void RemoveTrade( SecureTrade trade )
		{
			m_Trades.Remove( trade );
		}

		public SecureTrade FindTrade( Mobile m )
		{
			for ( int i = 0; i < m_Trades.Count; ++i )
			{
				SecureTrade trade = (SecureTrade)m_Trades[i];

				if ( trade.From.Mobile == m || trade.To.Mobile == m )
					return trade;
			}

			return null;
		}

		public SecureTradeContainer FindTradeContainer( Mobile m )
		{
			for ( int i = 0; i < m_Trades.Count; ++i )
			{
				SecureTrade trade = (SecureTrade)m_Trades[i];

				SecureTradeInfo from = trade.From;
				SecureTradeInfo to = trade.To;

				if ( from.Mobile == m_Mobile && to.Mobile == m )
					return from.Container;
				else if ( from.Mobile == m && to.Mobile == m_Mobile )
					return to.Container;
			}

			return null;
		}

		public SecureTradeContainer AddTrade( NetState state )
		{
			SecureTrade newTrade = new SecureTrade( m_Mobile, state.m_Mobile );

			m_Trades.Add( newTrade );
			state.m_Trades.Add( newTrade );

			return newTrade.From.Container;
		}

		public bool CompressionEnabled
		{
			get
			{
				return m_CompressionEnabled;
			}
			set
			{
				m_CompressionEnabled = value;
			}
		}

		public int Sequence
		{
			get
			{
				return m_Sequence;
			}
			set
			{
				m_Sequence = value;
			}
		}

		public GumpCollection Gumps{ get{ return m_Gumps; } }
		public HuePickerCollection HuePickers{ get{ return m_HuePickers; } }
		public MenuCollection Menus{ get{ return m_Menus; } }

		private static int m_GumpCap = 512, m_HuePickerCap = 512, m_MenuCap = 512;

		public static int GumpCap{ get{ return m_GumpCap; } set{ m_GumpCap = value; } }
		public static int HuePickerCap{ get{ return m_HuePickerCap; } set{ m_HuePickerCap = value; } }
		public static int MenuCap{ get{ return m_MenuCap; } set{ m_MenuCap = value; } }

		public void AddMenu( IMenu menu )
		{
			if ( m_Menus == null )
				return;

			if ( m_Menus.Count >= m_MenuCap )
			{
				Console.WriteLine( "Client: {0}: Exceeded menu cap, disconnecting...", this );
				Dispose();
			}
			else
			{
				m_Menus.Add( menu );
			}
		}

		public void RemoveMenu( int index )
		{
			if ( m_Menus == null )
				return;

			m_Menus.RemoveAt( index );
		}

		public void AddHuePicker( HuePicker huePicker )
		{
			if ( m_HuePickers == null ) 
				return;

			if ( m_HuePickers.Count >= m_HuePickerCap )
			{
				Console.WriteLine( "Client: {0}: Exceeded hue picker cap, disconnecting...", this );
				Dispose();
			}
			else
			{
				m_HuePickers.Add( huePicker );
			}
		}

		public void RemoveHuePicker( int index )
		{
			if ( m_HuePickers == null )
				return;

			m_HuePickers.RemoveAt( index );
		}

		public void AddGump( Gump g )
		{
			if ( m_Gumps == null )
				return;

			if ( m_Gumps.Count >= m_GumpCap )
			{
				Console.WriteLine( "Client: {0}: Exceeded gump cap, disconnecting...", this );
				Dispose();
			}
			else
			{
				m_Gumps.Add( g );
			}
		}

		public void RemoveGump( int index )
		{
			if ( m_Gumps == null )
				return;

			m_Gumps.RemoveAt( index );
		}

		public CityInfo[] CityInfo
		{
			get
			{
				return m_CityInfo;
			}
			set
			{
				m_CityInfo = value;
			}
		}

		public Mobile Mobile
		{
			get
			{
				return m_Mobile;
			}
			set
			{
				m_Mobile = value;
			}
		}

		public ServerInfo[] ServerInfo
		{
			get
			{
				return m_ServerInfo;
			}
			set
			{
				m_ServerInfo = value;
			}
		}

		public IAccount Account
		{
			get
			{
				return m_Account;
			}
			set
			{
				m_Account = value;
			}
		}

		public override string ToString()
		{
			return m_ToString;
		}

		private static ArrayList m_Instances = new ArrayList();

		public static ArrayList Instances
		{
			get
			{
				return m_Instances;
			}
		}

		private static BufferPool m_ReceiveBufferPool = new BufferPool( 1024, 2048 );

		public NetState( Socket socket, MessagePump messagePump )
		{
			m_Socket = socket;
			m_Buffer = new ByteQueue();
			m_Seeded = false;
			m_Running = false;
			m_RecvBuffer = m_ReceiveBufferPool.AquireBuffer();
			m_MessagePump = messagePump;
			m_Gumps = new GumpCollection();
			m_HuePickers = new HuePickerCollection();
			m_Menus = new MenuCollection();
			m_Trades = new ArrayList();

			m_SendQueue = new SendQueue();

			m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes( 0.5 );

			m_Instances.Add( this );

			try{ m_Address = ((IPEndPoint)m_Socket.RemoteEndPoint).Address; m_ToString = m_Address.ToString(); }
			catch{ m_Address = IPAddress.None; m_ToString = "(error)"; }

			if ( m_CreatedCallback != null )
				m_CreatedCallback( this );
		}

		public void Send( Packet p )
		{
			if ( m_Socket == null || m_BlockAllPackets )
				return;

			PacketProfile prof = PacketProfile.GetOutgoingProfile( (byte)p.PacketID );
			DateTime start = ( prof == null ? DateTime.MinValue : DateTime.Now );

			byte[] buffer = p.Compile( m_CompressionEnabled );

			if ( buffer != null )
			{
				if ( buffer.Length <= 0 )
					return;

				int length = buffer.Length;

				if ( m_Encoder != null )
					m_Encoder.EncodeOutgoingPacket( this, ref buffer, ref length );

				bool shouldBegin = false;

				lock ( m_SendQueue )
					shouldBegin = ( m_SendQueue.Enqueue( buffer, length ) );

				if ( shouldBegin )
				{
					int sendLength = 0;
					byte[] sendBuffer = m_SendQueue.Peek( ref sendLength );

					try
					{
						m_Socket.BeginSend( sendBuffer, 0, sendLength, SocketFlags.None, m_OnSend, null );
						//Console.WriteLine( "Send: {0}: Begin send of {1} bytes", this, sendLength );
					}
					catch // ( Exception ex )
					{
						//Console.WriteLine(ex);
						Dispose( false );
					}
				}

				if ( prof != null )
					prof.Record( length, DateTime.Now - start );
			}
			else
			{
				Dispose();
			}
		}

		public static void FlushAll()
		{
			if ( !SendQueue.CoalescePerSlice )
				return;

			for ( int i = 0; i < m_Instances.Count; ++i )
			{
				NetState ns = (NetState)m_Instances[i];

				ns.Flush();
			}
		}

		public bool Flush()
		{
			if ( m_Socket == null || !m_SendQueue.IsFlushReady )
				return false;

			int length = 0;
			byte[] buffer;

			lock ( m_SendQueue )
				buffer = m_SendQueue.CheckFlushReady( ref length );

			if ( buffer != null )
			{
				try
				{
					m_Socket.BeginSend( buffer, 0, length, SocketFlags.None, m_OnSend, null );
					return true;
					//Console.WriteLine( "Flush: {0}: Begin send of {1} bytes", this, length );
				}
				catch // ( Exception ex )
				{
					//Console.WriteLine(ex);
					Dispose( false );
				}
			}

			return false;
		}

		private static int m_CoalesceSleep = -1;

		public static int CoalesceSleep
		{
			get{ return m_CoalesceSleep; }
			set{ m_CoalesceSleep = value; }
		}

		private void OnSend( IAsyncResult asyncResult )
		{
			if ( m_Socket == null )
				return;

			try
			{
				int bytes = m_Socket.EndSend( asyncResult );

				if ( bytes <= 0 )
				{
					Dispose( false );
					return;
				}

				//Console.WriteLine( "OnSend: {0}: Complete send of {1} bytes", this, bytes );

				m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes( 1.2 );

				if ( m_CoalesceSleep >= 0 )
					System.Threading.Thread.Sleep( m_CoalesceSleep );

				int length = 0;
				byte[] queued;

				lock ( m_SendQueue )
					queued = m_SendQueue.Dequeue( ref length );

				if ( queued != null )
				{
					m_Socket.BeginSend( queued, 0, length, SocketFlags.None, m_OnSend, null );
					//Console.WriteLine( "OnSend: {0}: Begin send of {1} bytes", this, length );
				}
			}
			catch // ( Exception ex )
			{
				//Console.WriteLine(ex);
				Dispose( false );
			}
		}

		public void Start()
		{
			m_OnReceive = new AsyncCallback( OnReceive );
			m_OnSend = new AsyncCallback( OnSend );

			m_Running = true;

			if ( m_Socket == null )
				return;

			try
			{
				m_Socket.BeginReceive( m_RecvBuffer, 0, 4, SocketFlags.None, m_OnReceive, null );
			}
			catch // ( Exception ex )
			{
				//Console.WriteLine(ex);
				Dispose( false );
			}
		}

		public void LaunchBrowser( string url )
		{
			Send( new MessageLocalized( Serial.MinusOne, -1, MessageType.Label, 0x35, 3, 501231, "", "" ) );
			Send( new LaunchBrowser( url ) );
		}

		private DateTime m_NextCheckActivity;

		public bool CheckAlive()
		{
			if ( m_Socket == null )
				return false;

			if ( DateTime.Now < m_NextCheckActivity )
				return true;

			Console.WriteLine( "Client: {0}: Disconnecting due to inactivity...", this );

			Dispose();
			return false;
		}

		public void Continue()
		{
			if ( m_Socket == null )
				return;

			try
			{
				m_Socket.BeginReceive( m_RecvBuffer, 0, 2048, SocketFlags.None, m_OnReceive, null );
			}
			catch // ( Exception ex )
			{
				//Console.WriteLine(ex);
				Dispose( false );
			}
		}

		private void OnReceive( IAsyncResult asyncResult )
		{
			lock ( this )
			{
				if ( m_Socket == null )
					return;

				try
				{
					int byteCount = m_Socket.EndReceive( asyncResult );

					if ( byteCount > 0 )
					{
						m_NextCheckActivity = DateTime.Now + TimeSpan.FromMinutes( 1.2 );

						byte[] buffer = m_RecvBuffer;

						if ( m_Encoder != null )
							m_Encoder.DecodeIncomingPacket( this, ref buffer, ref byteCount );

						m_Buffer.Enqueue( buffer, 0, byteCount );

						m_MessagePump.OnReceive( this );
					}
					else
					{
						Dispose( false );
					}
				}
				catch // ( Exception ex )
				{
					//Console.WriteLine(ex);
					Dispose( false );
				}
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}

		private bool m_Disposing;

		public void Dispose( bool flush )
		{
			if ( m_Socket == null || m_Disposing )
				return;

			m_Disposing = true;

			if ( flush )
				flush = Flush();

			try { m_Socket.Shutdown( SocketShutdown.Both ); }
			catch {}

			try { m_Socket.Close(); }
			catch {}

			if ( m_RecvBuffer != null )
				m_ReceiveBufferPool.ReleaseBuffer( m_RecvBuffer );

			m_Socket = null;

			m_Buffer = null;
			m_RecvBuffer = null;
			m_OnReceive = null;
			m_OnSend = null;
			m_Running = false;

			m_Disposed.Enqueue( this );

			if ( /*!flush &&*/ !m_SendQueue.IsEmpty )
			{
				lock ( m_SendQueue )
					m_SendQueue.Clear();
			}
		}

		public static void Initialize()
		{
			Timer.DelayCall( TimeSpan.FromMinutes( 1.0 ), TimeSpan.FromMinutes( 1.5 ), new TimerCallback( CheckAllAlive ) );
		}

		public static void CheckAllAlive()
		{
			try
			{
				for ( int i = 0; i < m_Instances.Count; ++i )
					((NetState)m_Instances[i]).CheckAlive();
			}
			catch // ( Exception ex )
			{
				//Console.WriteLine(ex);
			}
		}

		private static Queue m_Disposed = Queue.Synchronized( new Queue() );

		public static void ProcessDisposedQueue()
		{
			int breakout = 0;

			while ( breakout < 200 && m_Disposed.Count > 0 )
			{
				++breakout;

				NetState ns = (NetState)m_Disposed.Dequeue();

				if ( ns.m_Account != null )
					Console.WriteLine( "Client: {0}: Disconnected. [{1} Online] [{2}]", ns, m_Instances.Count, ns.m_Account );
				else
					Console.WriteLine( "Client: {0}: Disconnected. [{1} Online]", ns, m_Instances.Count );

				Mobile m = ns.m_Mobile;

				if ( m != null )
				{
					m.NetState = null;
					ns.m_Mobile = null;
				}

				ns.m_Gumps.Clear();
				ns.m_Menus.Clear();
				ns.m_HuePickers.Clear();
				ns.m_Account = null;
				ns.m_ServerInfo = null;
				ns.m_CityInfo = null;

				m_Instances.Remove( ns );
			}
		}

		public bool Running
		{
			get
			{
				return m_Running;
			}
		}

		public bool Seeded
		{
			get
			{
				return m_Seeded;
			}
			set
			{
				m_Seeded = value;
			}
		}

		public Socket Socket
		{
			get
			{
				return m_Socket;
			}
		}

		public ByteQueue Buffer
		{
			get
			{
				return m_Buffer;
			}
		}
	}
}