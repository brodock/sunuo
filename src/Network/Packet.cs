/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2006 Max Kellermann <max@duempel.org>
 * based on code (C) The RunUO Software Team
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; version 2 of the License.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 */

using System;
using System.IO;

namespace Server.Network
{
	public interface IPacket
	{
		int PacketID { get; }
		byte[] Compile(bool compress);
	}

	public abstract class Packet : IPacket
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected PacketWriter m_Stream;
		private int m_PacketID;
		private int m_Length;

		public int PacketID
		{
			get{ return m_PacketID; }
		}

		public Packet( int packetID )
		{
			Initialize(packetID, 0);
		}

		public void EnsureCapacity( int length )
		{
			m_Stream = PacketWriter.CreateInstance( length );// new PacketWriter( length );
			m_Stream.Write( (byte) m_PacketID );
			m_Stream.Write( (short) 0 );
		}

		public Packet( int packetID, int length )
		{
			Initialize(packetID, length);
		}

		protected void Initialize(int packetID, int length) {
			m_PacketID = packetID;
			m_Length = length;

			if (m_Length > 0) {
				m_Stream = PacketWriter.CreateInstance(length);
				m_Stream.Write((byte)packetID);
			}

			PacketProfile prof = PacketProfile.GetOutgoingProfile( (byte)packetID );

			if ( prof != null )
				prof.RegConstruct();
		}

		protected void Clear() {
			m_CompiledBuffer = null;

			if (m_Stream != null) {
				PacketWriter.ReleaseInstance(m_Stream);
				m_Stream = null;
			}
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

				log.ErrorFormat("Packet: 0x{0:X2}: Bad packet length! ({1}{2} bytes)",
								m_PacketID, diff >= 0 ? "+" : "", diff );
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
					log.WarnFormat("Compression buffer overflowed on packet 0x{0:X2} ('{1}') (length={2})",
								   m_PacketID, GetType().Name, length );

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
