/*
 * SunUO
 * $Id$
 *
 * (c) 2005 Max Kellermann <max@duempel.org>
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
using Server;
using Server.Network;

namespace Server {
	public class ServerList {
		public static void Initialize() {
			EventSink.ServerList += new ServerListEventHandler(EventSink_ServerList);
		}

		public static void EventSink_ServerList(ServerListEventArgs e) {
			Config.GameServerList gsl = Core.Config.GameServerList;
			if (gsl == null) {
				e.Rejected = true;
				return;
			}

			foreach (Config.GameServer gs in gsl.GameServers)
				e.AddServer(gs.Name, gs.Address);
		}
	}
}
