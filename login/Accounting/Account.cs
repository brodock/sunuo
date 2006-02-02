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

namespace Server.Accounting {
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

		public override string ToString() {
			return name;
		}
	}
}
