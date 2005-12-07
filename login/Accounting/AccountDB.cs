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
using System.Text;

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

	public class SunAccount : IAccount {
		private String name, password;
		private bool banned;

		public SunAccount(String _name, String _password, bool _banned) {
			name = _name;
			password = _password;
			banned = _banned;
		}

		public bool CheckPassword(String password2) {
			return password == Hash.HashPassword(password2);
		}

		public bool Banned {
			get {
				return banned;
			}
		}
	}

	public class AccountDB {
		private IDbConnection dbcon;

		public AccountDB(String connectString) {
			dbcon = new OdbcConnection(connectString);
			dbcon.Open();
		}

		private bool FindAccountRecord(string username, ref string password,
									   ref bool banned) {
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "SELECT Password,Flags FROM users WHERE Username=?";
			IDataParameter p = (IDataParameter)dbcmd.CreateParameter();
			p.ParameterName = "Username";
			p.DbType = DbType.String;
			p.Value = username;
			dbcmd.Parameters.Add(p); 

			IDataReader reader = dbcmd.ExecuteReader();
			if (reader.Read()) {
				password = reader.GetString(0);
				int flags = reader.GetInt32(1);
				banned = (flags & 0x01) != 0;
				reader.Close();
				return true;
			} else {
				reader.Close();
				return false;
			}
		}

		private void UpdateAccountRecord(string username) {
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "UPDATE users SET LastLogin=NOW() WHERE Username=?";
			IDataParameter p = dbcmd.CreateParameter();
			p.ParameterName = "Username";
			p.DbType = DbType.String;
			p.Value = username;
			dbcmd.Parameters.Add(p); 
			dbcmd.ExecuteScalar();
		}

		private void CreateAccountRecord(NetState state, string username, string password) {
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = "INSERT INTO users(Username, Password, Flags, Created, LastLogin, CreationIP) VALUES(?, ?, 2, NOW(), NOW(), ?)";
			IDataParameter p = dbcmd.CreateParameter();
			p.ParameterName = "Username";
			p.DbType = DbType.String;
			p.Value = username;
			dbcmd.Parameters.Add(p);

			p = dbcmd.CreateParameter();
			p.ParameterName = "Password";
			p.DbType = DbType.String;
			p.Value = password;
			dbcmd.Parameters.Add(p);

			p = dbcmd.CreateParameter();
			p.ParameterName = "CreationIP";
			p.DbType = DbType.String;
			p.Value = state.Address.ToString();
			dbcmd.Parameters.Add(p);

			dbcmd.ExecuteScalar();
		}

		public SunAccount CreateAccount(NetState state, string username, string password) {
			password = Hash.HashPassword(password);
			CreateAccountRecord(state, username, password);

			return new SunAccount(username, password, false);
		}

		public SunAccount GetAccount(string username) {
			string password = null;
			bool banned = true;
			if (FindAccountRecord(username, ref password, ref banned)) {
				UpdateAccountRecord(username);
				return new SunAccount(username, password, banned);
			} else {
				return null;
			}
		}
	}
}
