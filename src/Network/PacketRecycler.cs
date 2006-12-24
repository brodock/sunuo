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

namespace Server.Network
{
	public interface IRecyclablePacket : IPacket, IDisposable, IRecyclable {
	}

	public sealed class GenericPacket : Packet, IRecyclablePacket
	{
		private static readonly Recycler m_Recycler = new Recycler(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public GenericPacket() : base(-1) {
		}

		public void Dispose() {
			Clear();
			m_Recycler.Put(this);
		}

		public void Recycle() {
		}

		public static GenericPacket GetInstance(int packetID, int length) {
			GenericPacket packet = (GenericPacket)m_Recycler.Get();
			packet.Initialize(packetID, length);
			return packet;
		}
	}

	public abstract class CompatPacket : IPacket {
		private GenericPacket m_Packet;

		public CompatPacket(int packetID, int length) {
			m_Packet = GenericPacket.GetInstance(packetID, length);
		}

		protected Packet Packet {
			get { return m_Packet; }
		}

		public int PacketID {
			get { return m_Packet.PacketID; }
		}

		public byte[] Compile(bool compress) {
			return m_Packet.Compile(compress);
		}
	}
}
