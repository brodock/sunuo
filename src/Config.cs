/***************************************************************************
 *                                 Config.cs
 *                            -------------------
 *   begin                : Jan 30, 2005
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
using System.IO;
using System.Xml;

namespace Server {
	public class LibraryConfig {
		private string name;
		private bool disabled = false;
		private string[] ignoreSources;
		private string[] ignoreTypes;

		private static string[] CollectStringArray(XmlElement parent,
												   string tag, string attr) {
			ArrayList al = new ArrayList();
			foreach (XmlElement el in parent.GetElementsByTagName(tag)) {
				string n = el.GetAttribute(attr);
				if (n != null)
					al.Add(n);
			}

			return al.Count == 0
				? null
				: (string[])al.ToArray(typeof(string));
		}

		public LibraryConfig(string _name) {
			name = _name;
		}

		public LibraryConfig(XmlElement libConfigEl) {
			name = libConfigEl.GetAttribute("name");

			ignoreSources = CollectStringArray(libConfigEl, "ignore-source", "name");
			ignoreTypes = CollectStringArray(libConfigEl, "ignore-source", "name");
		}

		public string Name {
			get { return name; }
		}
		public bool Disabled {
			get { return disabled; }
			set { disabled = value; }
		}

		public bool GetIgnoreSource(string filename) {
			if (ignoreSources == null)
				return false;

			foreach (string ign in ignoreSources)
				/* XXX: better check */
				if (filename.EndsWith(ign))
					return true;

			return false;
		}
		public bool GetIgnoreType(Type type) {
			if (ignoreTypes == null)
				return false;

			foreach (string ign in ignoreTypes)
				if (ign == type.FullName)
					return true;

			return false;
		}
	}

	public class LoginConfig {
		private bool ignoreAuthID;

		public LoginConfig() {
		}

		public LoginConfig(XmlElement el) {
			ignoreAuthID = el.GetElementsByTagName("ignore-auth-id").Count > 0;
		}

		public bool IgnoreAuthID {
			get { return ignoreAuthID; }
		}
	}

	public class Config {
		private string filename;
		private XmlDocument document;
		private ArrayList dataDirectories;
		private Hashtable libraryConfig;
		private LoginConfig loginConfig;

		public Config(string _filename) {
			filename = _filename;

			Load();
		}

		public ArrayList DataDirectories {
			get { return dataDirectories; }
		}

		public LibraryConfig GetLibraryConfig(string name) {
			return (LibraryConfig)libraryConfig[name];
		}

		public LoginConfig LoginConfig {
			get { return loginConfig; }
		}

		public XmlElement GetConfiguration(string path) {
			XmlElement el = document.DocumentElement;

			foreach (string seg in path.Split('/')) {
				XmlElement child = (XmlElement)el.SelectSingleNode(seg);
				if (child == null) {
					child = document.CreateElement(seg);
					el.AppendChild(child);
				}
				el = child;
			}

			return el;
		}

		public void Load() {
			document = new XmlDocument();
			dataDirectories = new ArrayList();
			libraryConfig = new Hashtable();

			if (File.Exists(filename)) {
				XmlTextReader reader = new XmlTextReader(filename);
				try {
					document.Load(reader);
				} finally {
					reader.Close();
				}
			} else {
				document.AppendChild(document.CreateElement("sunuo-config"));
			}

			XmlElement locations = GetConfiguration("locations");
			foreach (XmlElement dp in locations.GetElementsByTagName("data-path")) {
				string path = dp.InnerText;
				if (Directory.Exists(path))
					dataDirectories.Add(path);
			}

			XmlElement librariesEl = GetConfiguration("libraries");
			foreach (XmlElement el in librariesEl.GetElementsByTagName("library")) {
				LibraryConfig lc = new LibraryConfig(el);
				if (lc.Name != null)
					libraryConfig[lc.Name] = lc;
			}

			XmlElement loginEl = GetConfiguration("login");
			loginConfig = loginEl == null
				? new LoginConfig()
				: new LoginConfig(loginEl);
		}

		public void Save() {
			string tempFilename;
			if (filename.EndsWith(".xml")) {
				tempFilename = filename.Substring(0, filename.Length - 4) + ".new";
			} else {
				tempFilename = filename + ".new";
			}

			XmlElement locations = GetConfiguration("locations");
			XmlNodeList nl = locations.GetElementsByTagName("data-path");
			XmlNode[] children = new XmlNode[nl.Count];
			for (int i = 0; i < children.Length; i++)
				children[i] = nl.Item(i);
			foreach (XmlElement dp in children)
				dp.ParentNode.RemoveChild(dp);

			foreach (string path in	dataDirectories) {
				XmlElement el = document.CreateElement("data-path");
				el.InnerText = path;
				locations.AppendChild(el);
			}

			XmlTextWriter writer = new XmlTextWriter(tempFilename, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			try {
				document.Save(writer);
				writer.Close();
				File.Delete(filename);
				File.Move(tempFilename, filename);
			} catch {
				writer.Close();
				File.Delete(tempFilename);
				throw;
			}
		}
	}
}
