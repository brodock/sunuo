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

		public LibraryConfig(XmlElement libConfigEl) {
			name = libConfigEl.GetAttribute("name");
		}

		public string Name {
			get { return name; }
		}

		public bool IgnoreSource(string filename) {
			return false;
		}
		public bool IgnoreType(Type type) {
			return false;
		}
	}

	public class Config {
		private string filename;
		private XmlDocument document;
		private ArrayList dataDirectories;
		private Hashtable libraryConfig;

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

			if (!File.Exists(filename)) {
				document.AppendChild(document.CreateElement("sunuo-config"));
				return;
			}

			XmlTextReader reader = new XmlTextReader(filename);
			try {
				document.Load(reader);
			} finally {
				reader.Close();
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
