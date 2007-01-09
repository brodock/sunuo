/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2007 Max Kellermann <max@duempel.org>
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

namespace Server.Misc
{
	public class ServerList
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static string ServerName {
			get {
				string name = Core.Config.ServerName;
				if (name == null)
					name = "SunUO Test";
				return name;
			}
		}

		public static void Initialize()
		{
			Listener.Port = 2593;

			EventSink.ServerList += new ServerListEventHandler( EventSink_ServerList );
		}

		public static void EventSink_ServerList( ServerListEventArgs e )
		{
			try
			{
				if (Core.Config.GameServers.Count == 0) {
					e.AddServer( ServerName, (IPEndPoint)e.State.Socket.LocalEndPoint );
				} else {
					foreach (Config.GameServer gs in Core.Config.GameServers)
						e.AddServer(gs.Name, gs.Address);
				}
			}
			catch (Exception ex)
			{
				log.Fatal(ex);
				e.Rejected = true;
			}
		}
	}
}
