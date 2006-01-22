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
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.Network {
	public sealed class ServerStatus {
		public int age, clients, items, chars;
	}

	class ServerQuery {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Config.GameServer m_Config;
		private Socket m_Socket;
		private AsyncCallback m_Callback;
		private byte[] m_Buffer = new byte[512];
		private static byte[] m_SeedPacket = { 0x12, 0x34, 0x56, 0x78 };
		private static byte[] m_QueryPacket = { 0xf1, 0x00, 0x04, 0xff };

		public ServerQuery(Config.GameServer config) {
			m_Config = config;

			try {
				m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				/* connect socket */
				m_Callback = new AsyncCallback(OnConnect);
				m_Socket.BeginConnect(m_Config.Address, m_Callback, null);
			} catch (Exception ex) {
				log.Error(ex);
			}
		}

		private void OnConnect(IAsyncResult asyncResult) {
			/* socket connected */
			try {
				m_Socket.EndConnect(asyncResult);

				/* send seed */
				m_Callback = new AsyncCallback(OnSendSeed);
				m_Socket.BeginSend(m_SeedPacket, 0, m_SeedPacket.Length,
								   SocketFlags.None, m_Callback, null);
			} catch (Exception ex) {
				log.Error(ex);
			}
		}

		private void OnSendSeed(IAsyncResult asyncResult) {
			/* seed sent */
			try {
				m_Socket.EndSend(asyncResult);

				/* send UOGQuery packet */
				m_Callback = new AsyncCallback(OnSend);
				m_Socket.BeginSend(m_QueryPacket, 0, m_QueryPacket.Length,
								   SocketFlags.None, m_Callback, null);
			} catch (Exception ex) {
				log.Error(ex);
			}
		}

		private void OnSend(IAsyncResult asyncResult) {
			/* UOGQuery packet sent */
			try {
				m_Socket.EndSend(asyncResult);

				/* receive */
				m_Callback = new AsyncCallback(OnReceive);
				m_Socket.BeginReceive(m_Buffer, 0, m_Buffer.Length,
									  SocketFlags.None, m_Callback, null);
			} catch (Exception ex) {
				log.Error(ex);
			}
		}

		private void OnReceive(IAsyncResult asyncResult) {
			/* UOG status received */
			try {
				int byteCount = m_Socket.EndReceive(asyncResult);
				if (byteCount == 0)
					return;

				String response = new String(Encoding.ASCII.GetChars(m_Buffer, 0, byteCount - 1));

				ServerStatus status = new ServerStatus();
				foreach (string var in response.Split(',')) {
					string[] key_value = var.Split('=');
					if (key_value.Length == 2) {
						string key = key_value[0].Trim();
						string value = key_value[1].Trim();
						if (key == "Age")
							status.age = Int32.Parse(value);
						else if (key == "Clients")
							status.clients = Int32.Parse(value);
						else if (key == "Items")
							status.items = Int32.Parse(value);
						else if (key == "Chars")
							status.chars = Int32.Parse(value);
					}
				}

				ServerQueryTimer.SetStatus(m_Config, status);
			} catch (Exception ex) {
				log.Error(ex);
			}
		}
	}

	public class ServerQueryTimer : Timer {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static Hashtable m_Status;

		private ServerQueryTimer() : base(TimeSpan.Zero,
										  TimeSpan.FromMinutes(1.0)) {
			Priority = TimerPriority.FiveSeconds;
		}

		private static bool Enabled {
			get {
				Config.GameServerList gsl = Core.Config.GameServerList;
				if (gsl == null)
					return false;

				foreach (Config.GameServer gs in gsl.GameServers)
					if (gs.Query)
						return true;

				return false;
			}
		}

		public static void Initialize() {
			if (Enabled) {
				m_Status = new Hashtable();
				new ServerQueryTimer().Start();
			}
		}

		protected override void OnTick() {
			Config.GameServerList gsl = Core.Config.GameServerList;
			if (gsl == null)
				return;

			log.Info("Querying game servers");

			foreach (Config.GameServer gs in gsl.GameServers) {
				if (!gs.Query)
					continue;
				new ServerQuery(gs);
			}
		}

		public static void SetStatus(Config.GameServer config, ServerStatus status) {
			m_Status[config] = status;
		}

		public static ServerStatus GetStatus(Config.GameServer config) {
			return m_Status == null
				? null
				: (ServerStatus)m_Status[config];
		}
	}
}
