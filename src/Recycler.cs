/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2006 Max Kellermann <max@duempel.org>
 * based on code (C) The RunUO Software Team
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

namespace Server.Network
{
	public interface IRecyclable {
		void Recycle();
	}

	public sealed class Recycler {
		private Type m_Type;
		private Stack m_Stack = new Stack();

		public Recycler(Type type) {
			m_Type = type;
		}

		public IRecyclable Get() {
			lock (m_Stack)
				if (m_Stack.Count > 0)
					return (IRecyclable)m_Stack.Pop();

			return (IRecyclable)Activator.CreateInstance(m_Type);
		}

		public void Put(IRecyclable x) {
			if (x.GetType() != m_Type)
				throw new ArgumentException();

			lock (m_Stack)
				if (!m_Stack.Contains(x))
					m_Stack.Push(x);
		}
	}
}
