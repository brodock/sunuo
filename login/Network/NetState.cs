/***************************************************************************
 *                                NetState.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author: make $
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
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Accounting;
using Server.Network;

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
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Socket m_Socket;
		private IPEndPoint m_EndPoint;
		private IPAddress m_Address;
		private ByteQueue m_Buffer;
		private byte[] m_RecvBuffer;
		private SendQueue m_SendQueue;
		private bool m_Seeded;
		private bool m_Running;
		private bool m_Super;
		private bool m_Client = false;
		private bool m_Connecting = false;
		private AsyncCallback m_OnConnect, m_OnReceive, m_OnSend;
		private MessagePump m_MessagePump;
		private ServerInfo[] m_ServerInfo;
		private IAccount m_Account;
		private bool m_CompressionEnabled;
		private string m_ToString;
		private ClientVersion m_Version;
		private bool m_SentFirstPacket;

		internal int m_Seed;

		public IPAddress Address
		{
			get{ return m_Address; }
		}

		private int m_Flags;

		private IPacketEncoder m_Encoder;

		public IPacketEncoder PacketEncoder
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

				if ( value >= m_Version6017 )
					m_Post6017 = true;
				else
					m_Post6017 = false;
			}
		}

		private static ClientVersion m_Version6017 = new ClientVersion( "6.0.1.7" );

		private bool m_Post6017;

		public bool IsPost6017 {
			get { 
				return m_Post6017; 
			}
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

		public Mobile Mobile
		{
			get
			{
				return null;
			}
			set
			{
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

		private static Hashtable m_GameServers = new Hashtable();

		public static NetState GameServerClient(Config.GameServer config) {
			if (config == null)
				return null;

			String address = config.Address.ToString();
			NetState ns = (NetState)m_GameServers[address];
			if (ns != null)
				return ns;

			try {
				ns = new NetState(config.Address, Core.MessagePump);
				ns.Start();
				ns.Send(new SendSeed());
				m_GameServers[address] = ns;
				return ns;
			} catch (Exception e) {
				log.Error(String.Format("Exception while trying to connect to game server {0} ({1}): {2}",
										config.Name, address),
						  e);
				return null;
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

			m_SendQueue = new SendQueue();

			m_NextCheckActivity = Core.Now + TimeSpan.FromMinutes( 0.5 );

			m_Instances.Add( this );

			try{ m_Address = ((IPEndPoint)m_Socket.RemoteEndPoint).Address; m_ToString = m_Address.ToString(); }
			catch{ m_Address = IPAddress.None; m_ToString = "(error)"; }

			m_Super = Core.Config.Login.IsSuperClient(m_ToString);

			if ( m_CreatedCallback != null )
				m_CreatedCallback( this );
		}

		/** client constructor, used to connect to other UO servers */
		public NetState( IPEndPoint connectTo, MessagePump messagePump )
		{
			m_Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			m_EndPoint = connectTo;
			m_Buffer = new ByteQueue();
			m_Seeded = true;
			m_Running = false;
			m_Client = true;
			m_RecvBuffer = m_ReceiveBufferPool.AquireBuffer();
			m_MessagePump = messagePump;

			m_SendQueue = new SendQueue();

			m_NextCheckActivity = Core.Now + TimeSpan.FromMinutes( 0.5 );

			m_Instances.Add( this );

			try {
				m_Address = m_EndPoint.Address;
				m_ToString = "[To GameServer " + m_EndPoint.ToString() + "]";
			} catch{ m_Address = IPAddress.None; m_ToString = "(error)"; }

			if ( m_CreatedCallback != null )
				m_CreatedCallback( this );
		}

		public PacketHandler GetHandler( int packetID )
		{
			if ( IsPost6017 )
				return PacketHandlers.Get6017Handler( packetID );
			else
				return PacketHandlers.GetHandler( packetID );
		}

		public void Send( Packet p )
		{
			if ( m_Socket == null )
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

				if (m_Connecting)
					shouldBegin = false;

				if ( shouldBegin )
				{
					int sendLength = 0;
					byte[] sendBuffer = m_SendQueue.Peek( ref sendLength );

					try
					{
						m_Socket.BeginSend( sendBuffer, 0, sendLength, SocketFlags.None, m_OnSend, null );
						m_Sending = true;
						//Console.WriteLine( "Send: {0}: Begin send of {1} bytes", this, sendLength );
					}
					catch (Exception ex)
					{
						log.Error(ex);
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
					m_Sending = true;
					//m_Socket.Send( buffer, 0, length, SocketFlags.None );
					return true;
					//Console.WriteLine( "Flush: {0}: Begin send of {1} bytes", this, length );
				}
				catch (Exception ex)
				{
					log.Error(ex);
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
			m_Sending = false;

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

				if (m_Disposing && !m_DisposeFinished) {
					FinishDispose();
					return;
				}

				//Console.WriteLine( "OnSend: {0}: Complete send of {1} bytes", this, bytes );

				m_NextCheckActivity = Core.Now + TimeSpan.FromMinutes( 1.2 );

				if ( m_CoalesceSleep >= 0 )
					System.Threading.Thread.Sleep( m_CoalesceSleep );

				int length = 0;
				byte[] queued;

				lock ( m_SendQueue )
					queued = m_SendQueue.Dequeue( ref length );

				if ( queued != null )
				{
					m_Socket.BeginSend( queued, 0, length, SocketFlags.None, m_OnSend, null );
					m_Sending = true;
					//Console.WriteLine( "OnSend: {0}: Begin send of {1} bytes", this, length );
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
				Dispose( false );
			}
		}

		public void Start()
		{
			m_OnReceive = new AsyncCallback( OnReceive );
			m_OnSend = new AsyncCallback( OnSend );
			if (m_Client)
				m_OnConnect = new AsyncCallback( OnConnect );

			m_Running = true;

			if ( m_Socket == null )
				return;

			try
			{
				if (m_Client && !m_Connecting) {
					m_Socket.BeginConnect( m_EndPoint, m_OnConnect, null );
					m_Connecting = true;
				} else {
					m_Socket.BeginReceive( m_RecvBuffer, 0, 4, SocketFlags.None, m_OnReceive, null );
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
				Dispose( false );
			}
		}

		private DateTime m_NextCheckActivity;

		public bool CheckAlive()
		{
			if (m_Disposing && !m_DisposeFinished) {
				FinishDispose();
				return false;
			}

			if ( m_Socket == null )
				return false;

			if ( Core.Now < m_NextCheckActivity )
				return true;

			log.InfoFormat("Client: {0}: Disconnecting due to inactivity...", this);

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
			catch (Exception ex)
			{
				log.Error(ex);
				Dispose( false );
			}
		}

		private void OnConnect( IAsyncResult asyncResult )
		{
			lock ( this )
			{
				if ( m_Socket == null )
					return;

				try
				{
					m_Socket.EndConnect( asyncResult );
					m_Connecting = false;
					m_Socket.BeginReceive( m_RecvBuffer, 0, 4, SocketFlags.None, m_OnReceive, null );
				}
				catch ( Exception ex )
				{
					log.Error(ex);
					Dispose( false );
					return;
				}
			}

			// After a successful connect, send what may be already in the queue
			Flush();

			Core.WakeUp();
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
						m_NextCheckActivity = Core.Now + TimeSpan.FromMinutes( 1.2 );

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
				catch (Exception ex)
				{
					log.Error(ex);
					Dispose( false );
				}
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}

		private bool m_Disposing, m_DisposeFinished, m_Sending;

		public void Dispose( bool flush )
		{
			if (m_Client && m_EndPoint != null) {
				string endpoint = m_EndPoint.ToString();
				NetState old = (NetState)m_GameServers[endpoint];
				if (old == this)
					m_GameServers.Remove(endpoint);
			}

			if (m_Disposing && !m_DisposeFinished) {
				/* the second call forces disposal */
				FinishDispose();
				return;
			}

			if ( m_Socket == null || m_Disposing )
				return;

			m_Disposing = true;

			if ( flush )
				flush = Flush();

			/* if we're currently sending the last packet, schedule
			   the "real" dispose for later */
			if (!m_Sending)
				FinishDispose();
		}

		public void FinishDispose() {
			if (m_DisposeFinished)
				return;

			m_DisposeFinished = true;

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
			catch (Exception ex)
			{
				log.Error(ex);
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
					log.InfoFormat("Client: {0}: Disconnected. [{1} Online] [{2}]",
								   ns, m_Instances.Count, ns.m_Account);
				else
					log.InfoFormat("Client: {0}: Disconnected. [{1} Online]",
								   ns, m_Instances.Count);

				ns.m_Account = null;
				ns.m_ServerInfo = null;

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

		public bool Super
		{
			get
			{
				return m_Super;
			}
		}
	}
}
