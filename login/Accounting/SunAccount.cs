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
		private String m_Name, m_Password;
		private bool m_Banned, m_Young;

		public SunAccount(String name, String password, bool banned, bool young) {
			m_Name = name;
			m_Password = password;
			m_Banned = banned;
			m_Young = young;
		}

		public bool CheckPassword(String password2) {
			return m_Password == Hash.HashPassword(password2);
		}

		public bool Banned {
			get {
				return m_Banned;
			}
		}

		public bool Young {
			get {
				return m_Young;
			}
		}

		public override string ToString() {
			return m_Name;
		}
	}
}
