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
using Server.Network;

namespace Server.Accounting {
	public class SunAccount : IAccount {
		public bool CheckPassword(String password) {
			/* XXX */
			return true;
		}

		public bool Banned {
			get {
				/* XXX */
				return false;
			}
		}
	}

	public class AccountHandler {
		private static bool AutoAccountCreation = true;

		public static void Initialize() {
			EventSink.AccountLogin += new AccountLoginEventHandler( EventSink_AccountLogin );
		}

		private static SunAccount CreateAccount(NetState state, string username, string password) {
			/* XXX */
			return new SunAccount();
		}

		private static SunAccount GetAccount(string username) {
			/* XXX */
			return new SunAccount();
		}

		public static void EventSink_AccountLogin(AccountLoginEventArgs e) {
			e.Accepted = false;
			SunAccount account = GetAccount(e.Username);

			if (account == null) {
				if (AutoAccountCreation) {
					e.State.Account = account = CreateAccount(e.State, e.Username, e.Password);
					e.Accepted = true;
				} else {
					Console.WriteLine( "Login: {0}: Invalid username '{1}'", e.State, e.Username);
					e.RejectReason = ALRReason.Invalid;
				}
			} else if (!account.CheckPassword(e.Password)) {
				Console.WriteLine( "Login: {0}: Invalid password for '{1}'", e.State, e.Username);
				e.RejectReason = ALRReason.BadPass;
			} else if (account.Banned) {
				Console.WriteLine("Login: {0}: Banned account '{1}'", e.State, e.Username);
				e.RejectReason = ALRReason.Blocked;
			} else {
				Console.WriteLine("Login: {0}: Valid credentials for '{1}'", e.State, e.Username);
				e.State.Account = account;
				e.Accepted = true;
			}
		}
	}
}
