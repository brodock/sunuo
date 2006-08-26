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
	public class AccountHandler {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static AccountDB accountDB;

		public static void Initialize() {
			string connectString = Core.Config.Login.AccountDatabase;
			if (connectString == null)
				throw new Exception("login/account-database is not configured");

			EventSink.AccountLogin += new AccountLoginEventHandler( EventSink_AccountLogin );

			accountDB = new AccountDB(connectString);
		}

		private static bool CheckAccountCountPerIP(NetState ns) {
			int maxCreatedAccountsPerIP = Core.Config.Login.MaxCreatedAccountsPerIP;
			if (maxCreatedAccountsPerIP <= 0)
				return true;

			int createdAccounts = accountDB.GetAccountCountPerIP(ns.Address.ToString());
			if (createdAccounts < maxCreatedAccountsPerIP)
				return true;

			log.WarnFormat("Login: {0}: Account creation blocked, already {1}/{2} accounts registered for IP {3}",
						   ns, createdAccounts, maxCreatedAccountsPerIP, ns.Address.ToString());
			return false;
		}

		public static void EventSink_AccountLogin(AccountLoginEventArgs e) {
			e.Accepted = false;

			if (e.Username == "" || e.Password == "") {
				e.RejectReason = ALRReason.BadPass;
				return;
			}

			SunAccount account;
			try {
				account = accountDB.GetAccount(e.Username);
			} catch (Exception ex) {
				log.Error("AccountDB.GetAccount failed", ex);
				e.RejectReason = ALRReason.Blocked;
				return;
			}

			if (account == null) {
				if (Core.Config.Login.AutoCreateAccounts) {
					try {
						if (CheckAccountCountPerIP(e.State)) {
							e.RejectReason = ALRReason.Invalid;
						} else {
							log.InfoFormat("Login: {0}: Creating account '{1}'",
										   e.State, e.Username);
							e.State.Account = accountDB.CreateAccount(e.State, e.Username, e.Password);
							e.Accepted = true;
						}
					} catch (Exception ex) {
						log.Error("AccountDB.CreateAccount failed", ex);
						e.RejectReason = ALRReason.Blocked;
						return;
					}
				} else {
					log.WarnFormat("Login: {0}: Invalid username '{1}'",
								   e.State, e.Username);
					e.RejectReason = ALRReason.Invalid;
				}
			} else if (!account.CheckPassword(e.Password)) {
				log.WarnFormat("Login: {0}: Invalid password for '{1}'",
							   e.State, e.Username);
				e.RejectReason = ALRReason.BadPass;
			} else if (account.Banned) {
				log.WarnFormat("Login: {0}: Banned account '{1}'",
							   e.State, e.Username);
				e.RejectReason = ALRReason.Blocked;
			} else {
				log.InfoFormat("Login: {0}: Valid credentials for '{1}'",
							   e.State, e.Username);
				e.State.Account = account;
				e.Accepted = true;
			}
		}
	}
}

