/*
 * $Id$
 *
 * Query UOGateway status from a RunUO/SunUO server.
 *
 * (c) 2005 Max Kellermann (max@duempel.org)
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
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UOGQuery {
	public static void Main(string[] args) {
		if (args.Length != 1) {
			Console.Error.WriteLine("Usage: UOGQuery hostname[:port]");
			Console.Error.WriteLine("Query UOGateway status from an UO freeshard.");
			return;
		}

		string[] splitted = args[0].Split(new char[]{':'}, 2);
		int port = splitted.Length >= 2
			? Int32.Parse(splitted[1])
			: 2593;

		IPAddress addr = Dns.Resolve(splitted[0]).AddressList[0];
		IPEndPoint endPoint = new IPEndPoint(addr, port);

		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		socket.Connect(endPoint);

		byte[] query_uog_info_packet = { 0x12, 0x34, 0x56, 0x78, /* seed */
										 0xf1, /* RemoteAdmin */
										 0x00, 0x04, /* length */
										 0xff, /* UOGateway info */ };
		socket.Send(query_uog_info_packet);

		byte[] packet = new byte[4096];
		int length = socket.Receive(packet);

		if (length < 2 || packet[length - 1] != 0) {
			Console.Error.WriteLine("malformed response packet");
			return;
		}

		char[] response = Encoding.ASCII.GetChars(packet, 0, length - 1);
		Console.WriteLine(response);
	}
}
