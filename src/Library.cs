/***************************************************************************
 *                                 Library.cs
 *                            -------------------
 *   begin                : Jan 28, 2005
 *   copyright            : (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author$
 *   $Date$
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Reflection;

namespace Server {
	public class Library {
		private string name;
		private Assembly assembly;
		private Type[] types;
		private bool configured, initialized;
		private TypeCache typeCache;

		public Library(string _name, Assembly _assembly) {
			name = _name;
			assembly = _assembly;
			types = assembly.GetTypes();
			/* XXX: type ignores from Core.Configuration? */
			typeCache = new TypeCache(assembly);
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

		public void Configure() {
			if (name == "core") {
				configured = true;
				return;
			}

			if (configured)
				throw new ApplicationException("already configured");

			ArrayList invoke = new ArrayList();

			foreach (Type type in types) {
				MethodInfo m = type.GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);
				if (m != null)
					invoke.Add(m);
			}

			invoke.Sort(new CallPriorityComparer());

			foreach (MethodInfo m in invoke)
				m.Invoke(null, null);

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

			ArrayList invoke = new ArrayList();

			foreach (Type type in types) {
				MethodInfo m = type.GetMethod("Initialized", BindingFlags.Static | BindingFlags.Public);
				if (m != null)
					invoke.Add(m);
			}

			invoke.Sort(new CallPriorityComparer());

			foreach (MethodInfo m in invoke)
				m.Invoke(null, null);

			initialized = true;
		}
	}
}
