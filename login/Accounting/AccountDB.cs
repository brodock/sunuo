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
using System.Security;
using System.Security.Cryptography;
using System.Data;
using System.Data.Odbc;
using Server.Network;
using System.Globalization;
using System.Text;
using MySql.Data.MySqlClient;

namespace Server.Accounting {
	class Hash {
		private static MD5CryptoServiceProvider m_HashProvider = new MD5CryptoServiceProvider();

		public static string HashPassword(string plainPassword) {
			byte[] m_HashBuffer = new byte[256];

			int length = Encoding.ASCII.GetBytes(plainPassword, 0,
												  plainPassword.Length > 256
												 ? 256
												 : plainPassword.Length,
												 m_HashBuffer, 0);
			byte[] hashed = m_HashProvider.ComputeHash( m_HashBuffer, 0, length );

			return BitConverter.ToString(hashed).Replace("-", "").ToLower();
		}
	}

	public class AccountDB {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private string connectString;
		private IDbConnection dbcon;

		public static IFormatProvider m_Culture = new CultureInfo("en-GB", true);

		public AccountDB(String _connectString) {
			connectString = _connectString;
			Reconnect();
		}

		private void Reconnect() {
			//dbcon = new OdbcConnection(connectString);
			dbcon = new MySqlConnection(connectString);
			dbcon.Open();
		}

		private bool FindAccountRecord(string username, ref string password,
									   ref bool banned, ref bool young) {
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "SELECT Password,Flags FROM users WHERE Username=?Username";
			IDataParameter p = (IDataParameter)dbcmd.CreateParameter();
			p.ParameterName = "?Username";
			p.DbType = DbType.String;
			p.Value = username;
			dbcmd.Parameters.Add(p); 

			IDataReader reader = dbcmd.ExecuteReader();
			if (reader.Read()) {
				password = reader.GetString(0);
				int flags = reader.GetInt32(1);
				banned = (flags & 0x01) != 0;
				young = (flags & 0x02) == 0;
				reader.Close();
				return true;
			} else {
				reader.Close();
				return false;
			}
		}

		/** same as FindAccountRecord(), but auto-reconnects when the
			DB connection is lost */
		private bool FindAccountRecordAutoR(string username, ref string password,
											ref bool banned, ref bool young) {
			try {
				return FindAccountRecord(username, ref password,
										 ref banned, ref young);
			} catch (Exception e) {
				log.Warn("Connection to DB lost?", e);
				Reconnect();
				log.Info("Reconnected DB.");

				return FindAccountRecord(username, ref password,
										 ref banned, ref young);
			}
		}

		private void UpdateAccountRecord(string username) {
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "UPDATE users SET LastLogin=?LastLogin WHERE Username=?Username";

			IDataParameter p = dbcmd.CreateParameter();
			p.ParameterName = "?LastLogin";
			p.DbType = DbType.String;
			p.Value = DateTime.Now.ToString(m_Culture);
			dbcmd.Parameters.Add(p); 

			p = dbcmd.CreateParameter();
			p.ParameterName = "?Username";
			p.DbType = DbType.String;
			p.Value = username;
			dbcmd.Parameters.Add(p); 

			dbcmd.ExecuteScalar();
		}

		private void CreateAccountRecord(NetState state, string username, string password) {
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "INSERT INTO users(Username, Password, MagicWord, Flags, ExtraFlags, AccessLevel, Created, LastLogin, DonationStarted, DonationDuration, LatestUpdate, CreationIP, DeleteThis) VALUES(?Username, ?Password, ?MagicWord, 0, 0, 0, ?Created, ?LastLogin, ?DonationStarted, ?DonationDuration, CURRENT_TIMESTAMP+0, ?CreationIP, 0)";
			IDataParameter p = dbcmd.CreateParameter();
			p.ParameterName = "?Username";
			p.DbType = DbType.String;
			p.Value = username;
			dbcmd.Parameters.Add(p);

			p = dbcmd.CreateParameter();
			p.ParameterName = "?Password";
			p.DbType = DbType.String;
			p.Value = password;
			dbcmd.Parameters.Add(p);

			p = dbcmd.CreateParameter();
			p.ParameterName = "?MagicWord";
			p.DbType = DbType.String;
			p.Value = Hash.HashPassword("");
			dbcmd.Parameters.Add(p);

			p = dbcmd.CreateParameter();
			p.ParameterName = "?Created";
			p.DbType = DbType.String;
			p.Value = DateTime.Now.ToString(m_Culture);
			dbcmd.Parameters.Add(p); 

			p = dbcmd.CreateParameter();
			p.ParameterName = "?LastLogin";
			p.DbType = DbType.String;
			p.Value = DateTime.MinValue.ToString(m_Culture);
			dbcmd.Parameters.Add(p); 

			p = dbcmd.CreateParameter();
			p.ParameterName = "?DonationStarted";
			p.DbType = DbType.String;
			p.Value = DateTime.MinValue.ToString(m_Culture);
			dbcmd.Parameters.Add(p); 

			p = dbcmd.CreateParameter();
			p.ParameterName = "?DonationDuration";
			p.DbType = DbType.String;
			p.Value = TimeSpan.Zero.ToString();
			dbcmd.Parameters.Add(p); 

			p = dbcmd.CreateParameter();
			p.ParameterName = "?CreationIP";
			p.DbType = DbType.String;
			p.Value = state.Address.ToString();
			dbcmd.Parameters.Add(p);

			dbcmd.ExecuteScalar();
		}

		public SunAccount CreateAccount(NetState state, string username, string password) {
			password = Hash.HashPassword(password);
			CreateAccountRecord(state, username, password);

			return new SunAccount(username, password, false, true);
		}

		public SunAccount GetAccount(string username) {
			string password = null;
			bool banned = true, young = false;
			if (FindAccountRecordAutoR(username, ref password,
									   ref banned, ref young)) {
				UpdateAccountRecord(username);
				return new SunAccount(username, password, banned, young);
			} else {
				return null;
			}
		}
	}
}
