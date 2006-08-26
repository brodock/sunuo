/***************************************************************************
 *                             PacketHandlers.cs
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
using Server.Accounting;
using CV = Server.ClientVersion;

namespace Server.Network
{
	public enum MessageType
	{
		Regular = 0x00,
		System = 0x01,
		Emote = 0x02,
		Label = 0x06,
		Focus = 0x07,
		Whisper = 0x08,
		Yell = 0x09,
		Spell = 0x0A,
		Encoded = 0xC0
	}

	public class PacketHandlers
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static PacketHandler[] m_Handlers;

		private static PacketHandler[] m_ExtendedHandlersLow;
		private static Hashtable m_ExtendedHandlersHigh;

		public static PacketHandler[] Handlers
		{
			get{ return m_Handlers; }
		}

		static PacketHandlers()
		{
			m_Handlers = new PacketHandler[0x100];

			m_ExtendedHandlersLow = new PacketHandler[0x100];
			m_ExtendedHandlersHigh = new Hashtable();

			Register( 0x73,   2, false, new OnPacketReceive( PingReq ) );
			Register( 0x80,  62, false, new OnPacketReceive( AccountLogin ) );
			Register( 0xA0,   3, false, new OnPacketReceive( PlayServer ) );
			Register( 0xCF,   0, false, new OnPacketReceive( AccountLogin ) );
			Register( 0xBF,   0, false, new OnPacketReceive( ExtendedCommand ) );
			Register( 0xD9, 0x10C, false, new OnPacketReceive( IgnorePacket ) );
			Register( 0xF1,	  0, false, new OnPacketReceive( UOGQuery ) );

			RegisterExtended( 0x5555, false, new OnPacketReceive( Emulator ) );
		}

		public static void Register( int packetID, int length, bool ingame, OnPacketReceive onReceive )
		{
			m_Handlers[packetID] = new PacketHandler( packetID, length, ingame, onReceive );
		}

		public static void RegisterExtended( int packetID, bool ingame, OnPacketReceive onReceive )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				m_ExtendedHandlersLow[packetID] = new PacketHandler( packetID, 0, ingame, onReceive );
			else
				m_ExtendedHandlersHigh[packetID] = new PacketHandler( packetID, 0, ingame, onReceive );
		}

		public static PacketHandler GetExtendedHandler( int packetID )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				return m_ExtendedHandlersLow[packetID];
			else
				return (PacketHandler)m_ExtendedHandlersHigh[packetID];
		}

		public static void RemoveExtendedHandler( int packetID )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				m_ExtendedHandlersLow[packetID] = null;
			else
				m_ExtendedHandlersHigh.Remove( packetID );
		}

		public static void PingReq( NetState state, PacketReader pvSrc )
		{
			state.Send( PingAck.Instantiate( pvSrc.ReadByte() ) );
		}

		private static int GenerateAuthID()
		{
			int authID = Utility.Random( 1, int.MaxValue - 1 );

			if ( Utility.RandomBool() )
				authID |= 1<<31;

			return authID;
		}

		public static void PlayServer( NetState state, PacketReader pvSrc )
		{
			int index = pvSrc.ReadInt16();
			ServerInfo[] info = state.ServerInfo;
			IAccount a = state.Account;

			if ( info == null || a == null || index < 0 || index >= info.Length )
			{
				state.Dispose();
			}
			else
			{
				ServerInfo si = info[index];

				PlayServerAck.m_AuthID = GenerateAuthID();

				/* send AuthID to game server? */
				Config.GameServer config = Core.Config.GameServers[si.Name];
				if (config != null && config.SendAuthID) {
					NetState ns2 = NetState.GameServerClient(config);
					if (ns2 != null)
						ns2.Send(new AddAuthID(PlayServerAck.m_AuthID, state.Account.ToString()));
				}

				state.SentFirstPacket = false;
				state.Send( new PlayServerAck( si ) );

				log.InfoFormat("Client {0} playing server '{1}'",
							   state, si.Name);
			}
		}

		public static void AccountLogin( NetState state, PacketReader pvSrc )
		{
			if ( state.SentFirstPacket )
			{
				state.Dispose();
				return;
			}

			state.SentFirstPacket = true;

			string username = pvSrc.ReadString( 30 );
			string password = pvSrc.ReadString( 30 );

			AccountLoginEventArgs e = new AccountLoginEventArgs( state, username, password );

			try {
				EventSink.InvokeAccountLogin(e);
			} catch (Exception ex) {
				log.Fatal(String.Format("Exception disarmed in AccountLogin {0}",
										username), ex);
			}

			if ( e.Accepted )
				AccountLogin_ReplyAck( state );
			else
				AccountLogin_ReplyRej( state, e.RejectReason );
		}

		public static void AccountLogin_ReplyAck( NetState state )
		{
			ServerListEventArgs e = new ServerListEventArgs( state, state.Account );

			try {
				EventSink.InvokeServerList(e);
			} catch (Exception ex) {
				log.Fatal("Exception disarmed in ServerList", ex);
				e.Rejected = true;
			}

			if ( e.Rejected )
			{
				state.Account = null;
				state.Send( new AccountLoginRej( ALRReason.BadComm ) );
				state.Dispose();
			}
			else
			{
				ServerInfo[] info = (ServerInfo[])e.Servers.ToArray( typeof( ServerInfo ) );

				state.ServerInfo = info;

				state.Send( new AccountLoginAck( info ) );
			}
		}

		public static void AccountLogin_ReplyRej( NetState state, ALRReason reason )
		{
			state.Send( new AccountLoginRej( reason ) );
			state.Dispose();
		}

		public static void ExtendedCommand( NetState state, PacketReader pvSrc )
		{
			int packetID = pvSrc.ReadUInt16();

			PacketHandler ph = GetExtendedHandler( packetID );

			if ( ph != null )
			{
				if ( ph.Ingame && state.Mobile == null )
				{
					log.WarnFormat("Client: {0}: Sent ingame packet (0xBFx{1:X2}) before having been attached to a mobile",
								   state, packetID);
					state.Dispose();
				}
				else if ( ph.Ingame && state.Mobile.Deleted )
				{
					state.Dispose();
				}
				else
				{
					ph.OnReceive( state, pvSrc );
				}
			}
			else
			{
				pvSrc.Trace( state );
			}
		}

		public static void IgnorePacket( NetState state, PacketReader pvSrc ) {
		}

		public static void UOGQuery( NetState state, PacketReader pvSrc )
		{
			byte cmd = pvSrc.ReadByte();
			if ( cmd != 0xFF )
				return;

			log.InfoFormat("Client {0} querying UOG status", state);

			string name = Core.Config.ServerName;
			if (name == null)
				name = "SunUO Login";

			int age = 0;
			int clients = NetState.Instances.Count;
			int items = 0;
			int chars = 0;

			/* add values from game servers */
			foreach (Config.GameServer gs in Core.Config.GameServers) {
				ServerStatus status = ServerQueryTimer.GetStatus(gs);
				if (status != null) {
					age += status.age;
					clients += status.clients;
					items += status.items;
					chars += status.chars;
				}
			}

			string statStr = String.Format( ", Name={0}, Age={1}, Clients={2}, Items={3}, Chars={4}, Mem={5}K", name, age, clients, items, chars, (int)(System.GC.GetTotalMemory(false)/1024) );
			state.Send( new UOGInfo( statStr ) );
			state.Dispose();
		}

		private static void Emulator( NetState state, PacketReader pvSrc )
		{
			int code = pvSrc.ReadUInt16();
			/*
			switch (code) {
			}
			*/
		}

		public static PacketHandler GetHandler( int packetID )
		{
			return m_Handlers[packetID];
		}
	}
}
