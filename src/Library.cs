/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2006 Max Kellermann <max@duempel.org>
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
using System.Reflection;

namespace Server {
	public class Library {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private string name;
		private Assembly assembly;
		private Type[] types;
		private TypeTable typesByName, typesByFullName;
		private bool configured, initialized;
		private TypeCache typeCache;

		public Library(Config.Library libConfig, Assembly _assembly) {
			name = libConfig.Name;
			assembly = _assembly;

			ArrayList typeList = new ArrayList();
			assembly.GetTypes();
			foreach (Type type in assembly.GetTypes()) {
				if (libConfig == null || !libConfig.GetIgnoreType(type))
					typeList.Add(type);
			}
			types = (Type[])typeList.ToArray(typeof(Type));

			typesByName = new TypeTable(types.Length);
			typesByFullName = new TypeTable(types.Length);

			Type typeofTypeAliasAttribute = typeof(TypeAliasAttribute);

			foreach (Type type in types) {
				typesByName.Add(type.Name, type);
				typesByFullName.Add(type.FullName, type);

				if (type.IsDefined(typeofTypeAliasAttribute, false)) {
					object[] attrs = type.GetCustomAttributes(typeofTypeAliasAttribute, false);

					if (attrs != null && attrs.Length > 0 && attrs[0] != null) {
						TypeAliasAttribute attr = attrs[0] as TypeAliasAttribute;
						foreach (string alias in attr.Aliases)
							typesByFullName.Add(alias, type);
					}
				}
			}

			typeCache = new TypeCache(types, typesByName, typesByFullName);
		}

		public string Name {
			get { return name; }
		}
		public Assembly Assembly {
			get { return assembly; }
		}
		public TypeCache TypeCache {
			get { return typeCache; }
		}
		public Type[] Types {
			get { return types; }
		}
		public TypeTable TypesByName {
			get { return typesByName; }
		}
		public TypeTable TypesByFullName {
			get { return typesByFullName; }
		}

		public void Verify(ref int itemCount, ref int mobileCount) {
			Type[] ctorTypes = new Type[]{ typeof(Serial) };

			foreach (Type t in types) {
				bool isItem = t.IsSubclassOf(typeof(Item));
				bool isMobile = t.IsSubclassOf(typeof(Mobile));

				if (isItem || isMobile) {
					if (isItem)
						++itemCount;
					else
						++mobileCount;

					try {
						if ( t.GetConstructor( ctorTypes ) == null )
						{
							log.WarnFormat("{0} has no serialization constructor", t);
						}

						if ( t.GetMethod( "Serialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ) == null )
						{
							log.WarnFormat("{0} has no Serialize() method", t);
						}

						if ( t.GetMethod( "Deserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ) == null )
						{
							log.WarnFormat("{0} has no Deserialize() method", t);
						}
					}
					catch
					{
					}
				}
			}
		}

		private void InvokeAll(string methodName) {
			ArrayList invoke = new ArrayList();

			foreach (Type type in types) {
				MethodInfo m = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
				if (m != null)
					invoke.Add(m);
			}

			invoke.Sort(new CallPriorityComparer());

			foreach (MethodInfo m in invoke)
				m.Invoke(null, null);
		}

		public void Configure() {
			if (name == "core") {
				configured = true;
				return;
			}

			if (configured)
				throw new ApplicationException("already configured");

			InvokeAll("Configure");

			configured = true;
		}

		public void Initialize() {
			if (name == "core") {
				initialized = true;
				return;
			}

			if (!configured)
				throw new ApplicationException("not configured yet");
			if (initialized)
				throw new ApplicationException("already initialized");

			InvokeAll("Initialized");

			initialized = true;
		}

		public Type GetTypeByName(string name, bool ignoreCase) {
			return typesByName.Get(name, ignoreCase);
		}

		public Type GetTypeByFullName(string fullName, bool ignoreCase) {
			return typesByFullName.Get(fullName, ignoreCase);
		}

	}
}
