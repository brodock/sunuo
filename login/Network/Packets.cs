/***************************************************************************
 *                                Packets.cs
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
using System.IO;
using System.Net;

namespace Server.Network
{
	public enum LRReason : byte
	{
		CannotLift = 0,
		OutOfRange = 1,
		OutOfSight = 2,
		TryToSteal = 3,
		AreHolding = 4,
		Inspecific = 5
	}

	public sealed class AsciiMessage : Packet
	{
		public AsciiMessage( Serial serial, int graphic, MessageType type, int hue, int font, string name, string text ) : base( 0x1C )
		{
			if ( name == null )
				name = "";

			if ( text == null )
				text = "";

			if ( hue == 0 )
				hue = 0x3B2;

			this.EnsureCapacity( 45 + text.Length );

			m_Stream.Write( (int) serial );
			m_Stream.Write( (short) graphic );
			m_Stream.Write( (byte) type );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (short) font );
			m_Stream.WriteAsciiFixed( name, 30 );
			m_Stream.WriteAsciiNull( text );
		}
	}

	public sealed class PingAck : Packet
	{
		private static PingAck[] m_Cache = new PingAck[0x100];

		public static PingAck Instantiate( byte ping )
		{
			PingAck p = m_Cache[ping];

			if ( p == null )
				m_Cache[ping] = p = new PingAck( ping );

			return p;
		}

		public PingAck( byte ping ) : base( 0x73, 2 )
		{
			m_Stream.Write( ping );
		}
	}

	public enum ALRReason : byte
	{
		Invalid = 0x00,
		InUse = 0x01,
		Blocked = 0x02,
		BadPass = 0x03,
		Idle = 0xFE,
		BadComm = 0xFF
	}

	public sealed class AccountLoginRej : Packet
	{
		public AccountLoginRej( ALRReason reason ) : base( 0x82, 2 )
		{
			m_Stream.Write( (byte)reason );
		}
	}

	public sealed class ServerInfo
	{
		private string m_Name;
		private int m_FullPercent;
		private int m_TimeZone;
		private IPEndPoint m_Address;

		public string Name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;
			}
		}

		public int FullPercent
		{
			get
			{
				return m_FullPercent;
			}
			set
			{
				m_FullPercent = value;
			}
		}

		public int TimeZone
		{
			get
			{
				return m_TimeZone;
			}
			set
			{
				m_TimeZone = value;
			}
		}

		public IPEndPoint Address
		{
			get
			{
				return m_Address;
			}
			set
			{
				m_Address = value;
			}
		}

		public ServerInfo( string name, int fullPercent, TimeZone tz, IPEndPoint address )
		{
			m_Name = name;
			m_FullPercent = fullPercent;
			m_TimeZone = tz.GetUtcOffset( DateTime.Now ).Hours;
			m_Address = address;
		}
	}

	public sealed class AccountLoginAck : Packet
	{
		public AccountLoginAck( ServerInfo[] info ) : base( 0xA8 )
		{
			this.EnsureCapacity( 6 + (info.Length * 40) );

			m_Stream.Write( (byte) 0x5D ); // Unknown

			m_Stream.Write( (ushort) info.Length );

			for ( int i = 0; i < info.Length; ++i )
			{
				ServerInfo si = info[i];

				m_Stream.Write( (ushort) i );
				m_Stream.WriteAsciiFixed( si.Name, 32 );
				m_Stream.Write( (byte) si.FullPercent );
				m_Stream.Write( (sbyte) si.TimeZone );
				m_Stream.Write( (int) si.Address.Address.Address );
			}
		}
	}

	public sealed class PlayServerAck : Packet
	{
		internal static int m_AuthID = -1;

		public PlayServerAck( ServerInfo si ) : base( 0x8C, 11 )
		{
			int addr = (int)si.Address.Address.Address;

			m_Stream.Write( (byte) addr );
			m_Stream.Write( (byte)(addr >> 8) );
			m_Stream.Write( (byte)(addr >> 16) );
			m_Stream.Write( (byte)(addr >> 24) );

			m_Stream.Write( (short) si.Address.Port );
			m_Stream.Write( (int) m_AuthID );
		}
	}

	public sealed class SendSeed : Packet
	{
		public SendSeed() : base( 0xde, 4 )
		{
			m_Stream.Write( (byte) 0xad );
			m_Stream.Write( (byte) 0xbe );
			m_Stream.Write( (byte) 0xef );
		}
	}

	public sealed class AddAuthID : Packet
	{
		public AddAuthID( int authID, string username ) : base( 0xBF )
		{
			EnsureCapacity( 7 + 4 + 30 );

			m_Stream.Write( (ushort) 0x5555 );
			m_Stream.Write( (ushort) 0x0001 );
			m_Stream.Write( (int) authID );
			m_Stream.WriteAsciiFixed( username, 30 );
		}
	}

	public abstract class Packet
	{
		protected PacketWriter m_Stream;
		private int m_PacketID;
		private int m_Length;

		public int PacketID
		{
			get{ return m_PacketID; }
		}

		public Packet( int packetID )
		{
			m_PacketID = packetID;

			PacketProfile prof = PacketProfile.GetOutgoingProfile( (byte)packetID );

			if ( prof != null )
				prof.RegConstruct();
		}

		public void EnsureCapacity( int length )
		{
			m_Stream = PacketWriter.CreateInstance( length );// new PacketWriter( length );
			m_Stream.Write( (byte) m_PacketID );
			m_Stream.Write( (short) 0 );
		}

		public Packet( int packetID, int length )
		{
			m_PacketID = packetID;
			m_Length = length;

			m_Stream = PacketWriter.CreateInstance( length );// new PacketWriter( length );
			m_Stream.Write( (byte) packetID );

			PacketProfile prof = PacketProfile.GetOutgoingProfile( (byte)packetID );

			if ( prof != null )
				prof.RegConstruct();
		}

		public PacketWriter UnderlyingStream
		{
			get
			{
				return m_Stream;
			}
		}

		private byte[] m_CompiledBuffer;

		public byte[] Compile( bool compress )
		{
			if ( m_CompiledBuffer == null )
				InternalCompile( compress );

			return m_CompiledBuffer;
		}

		private void InternalCompile( bool compress )
		{
			if ( m_Length == 0 )
			{
				long streamLen = m_Stream.Length;

				m_Stream.Seek( 1, SeekOrigin.Begin );
				m_Stream.Write( (ushort) streamLen );
			}
			else if ( m_Stream.Length != m_Length )
			{
				int diff = (int)m_Stream.Length - m_Length;

				Console.WriteLine( "Packet: 0x{0:X2}: Bad packet length! ({1}{2} bytes)", m_PacketID, diff >= 0 ? "+" : "", diff );
			}

			MemoryStream ms = m_Stream.UnderlyingStream;

			int length;

			m_CompiledBuffer = ms.GetBuffer();
			length = (int)ms.Length;

			if ( compress )
			{
				try
				{
					Compression.Compress( m_CompiledBuffer, length, out m_CompiledBuffer, out length );
				}
				catch ( IndexOutOfRangeException )
				{
					Console.WriteLine( "Warning: Compression buffer overflowed on packet 0x{0:X2} ('{1}') (length={2})", m_PacketID, GetType().Name, length );

					m_CompiledBuffer = null;
				}
			}

			if ( m_CompiledBuffer != null )
			{
				byte[] old = m_CompiledBuffer;

				m_CompiledBuffer = new byte[length];

				Buffer.BlockCopy( old, 0, m_CompiledBuffer, 0, length );
			}

			PacketWriter.ReleaseInstance( m_Stream );
			m_Stream = null;
		}
	}
}
