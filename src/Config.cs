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
using System.IO;
using System.Xml;
using System.Net;

namespace Server.Config {
	class Parser {
		public static bool ParseBool(string value, bool defaultValue) {
			if (value == null || value == "")
				return defaultValue;

			if (value == "true" || value == "on" || value == "yes")
				return true;

			if (value == "false" || value == "off" || value == "no")
				return false;

			throw new FormatException("Cannot parse boolean value");
		}

		public static string GetElementString(XmlElement parent, string tag) {
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return null;
			return nl[0].InnerText;
		}

		public static bool GetElementBool(XmlElement parent, string tag,
										  bool defaultValue) {
			if (parent == null)
				return defaultValue;
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return defaultValue;
			string value = ((XmlElement)nl[0]).GetAttribute("value");
			return ParseBool(value, true);
		}

		public static int GetElementInt(XmlElement parent, string tag, int defaultValue)
		{
			if (parent == null)
				return defaultValue;
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return defaultValue;
			string value = ((XmlElement)nl[0]).GetAttribute("value");
			try
			{
				return Int32.Parse(value);
			}
			catch
			{
				return defaultValue;
			}
		}
	}

	public class Features {
		private Hashtable m_Table = new Hashtable();

		public bool this[string name] {
			get {
				return m_Table.Contains(name);
			}
			set {
				if (value)
					m_Table[name] = true;
				else
					m_Table.Remove(name);
			}
		}
	}

	public class Library {
		private string name;
		private DirectoryInfo sourcePath;
		private FileInfo binaryPath;
		private bool disabled = false;
		private string[] ignoreSources;
		private string[] ignoreTypes;
		private string[] depends;
		private string[] overlays;
		private int warningLevel = -1;

		private static string[] CollectStringArray(XmlElement parent,
												   string tag, string attr) {
			ArrayList al = new ArrayList();
			foreach (XmlElement el in parent.GetElementsByTagName(tag)) {
				string n = el.GetAttribute(attr);
				if (n != null && n != "")
					al.Add(n);
			}

			return al.Count == 0
				? null
				: (string[])al.ToArray(typeof(string));
		}
		private static string[] LowerStringArray(string[] src) {
			if (src == null)
				return null;

			string[] dst = new string[src.Length];
			for (uint i = 0; i < src.Length; i++)
				dst[i] = src[i].ToLower();

			return dst;
		}

		public Library(string _name) {
			name = _name;
		}

		public Library(string _name, DirectoryInfo _path) {
			name = _name;
			sourcePath = _path;
		}

		public Library(string _name, FileInfo _path) {
			name = _name;
			binaryPath = _path;
		}

		public void Load(XmlElement libConfigEl) {
			string sourcePathString = Parser.GetElementString(libConfigEl, "path");
			if (sourcePathString != null) {
				if (sourcePathString.EndsWith(".dll")) {
					sourcePath = null;
					binaryPath = new FileInfo(sourcePathString);
				} else {
					sourcePath = new DirectoryInfo(sourcePathString);
					binaryPath = null;
				}
			}

			ignoreSources = CollectStringArray(libConfigEl, "ignore-source", "name");
			ignoreTypes = CollectStringArray(libConfigEl, "ignore-type", "name");
			overlays = LowerStringArray(CollectStringArray(libConfigEl, "overlay", "name"));
			depends = LowerStringArray(CollectStringArray(libConfigEl, "depends", "name"));

			string disabledString = libConfigEl.GetAttribute("disabled");
			disabled = Parser.ParseBool(disabledString, false);

			string warnString = libConfigEl.GetAttribute("warn");
			if (warnString != null && warnString != "")
				warningLevel = Int32.Parse(warnString);
		}

		public string Name {
			get { return name; }
		}
		public DirectoryInfo SourcePath {
			get { return sourcePath; }
		}
		public FileInfo BinaryPath {
			get { return binaryPath; }
		}
		public bool Exists {
			get {
				return (sourcePath != null && sourcePath.Exists) ||
					(binaryPath != null && binaryPath.Exists);
			}
		}
		public bool Disabled {
			get { return disabled; }
			set { disabled = value; }
		}
		public int WarningLevel {
			get { return warningLevel; }
		}
		public string[] Overlays {
			get { return overlays; }
		}
		public string[] Depends {
			get { return depends; }
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

	public class Network {
		private IPEndPoint[] m_Bind;

		public Network() {
		}

		public Network(XmlElement networkEl) {
			XmlNodeList nl = networkEl.GetElementsByTagName("bind");
			if (nl.Count == 0) {
				DefaultBind();
			} else {
				m_Bind = new IPEndPoint[nl.Count];
				for (int i = 0; i < nl.Count; ++i)
					m_Bind[i] = ParseEndPoint((XmlElement)nl[i]);
			}
		}

		private void DefaultBind() {
			m_Bind = new IPEndPoint[1];
			m_Bind[0] = new IPEndPoint(IPAddress.Any, 2593);
		}

		private static IPEndPoint ParseEndPoint(XmlElement el) {
			IPAddress address = IPAddress.Any;
			int port = 2593;

			string addressString = el.GetAttribute("address");
			if (addressString != null && addressString != "")
				address = IPAddress.Parse(addressString);

			string portString = el.GetAttribute("port");
			if (portString != null && portString != "")
				port = Int32.Parse(portString);

			return new IPEndPoint(address, port);
		}

		public IPEndPoint[] Bind {
			get { return m_Bind; }
		}
	}

	public class Login {
		private bool ignoreAuthID, autoCreateAccounts;
		private string accountDatabase;
		private int maxCreatedAccountsPerIP, maxLoginsPerIP;
		private Hashtable superClients = new Hashtable();

		public Login() {
		}

		public Login(XmlElement el) {
			ignoreAuthID = Parser.GetElementBool(el, "ignore-auth-id", false);
			autoCreateAccounts = Parser.GetElementBool(el, "auto-create-accounts", true);
			accountDatabase = Parser.GetElementString(el, "account-database");

			maxCreatedAccountsPerIP = Parser.GetElementInt(el, "max-created-accounts-per-ip", 0);
			maxLoginsPerIP = Parser.GetElementInt(el, "max-logins-per-ip", 0);

			foreach (XmlElement priv in el.GetElementsByTagName("super-client")) {
				string ip = priv.InnerText;
				if (ip == null)
					continue;
				ip = ip.Trim();
				if (ip == "")
					continue;
				superClients[ip] = true;
			}
		}

		public bool IgnoreAuthID {
			get { return ignoreAuthID; }
		}

		public bool AutoCreateAccounts {
			get { return autoCreateAccounts; }
		}

		public string AccountDatabase {
			get { return accountDatabase; }
		}

		public int MaxCreatedAccountsPerIP {
		    get { return maxCreatedAccountsPerIP; }
		}

		public int MaxLoginsPerIP {
		    get { return maxLoginsPerIP; }
		}

		public bool IsSuperClient(string ip) {
			return ip != null && superClients.ContainsKey(ip);
		}
	}

	public class GameServer {
		private String name;
		private IPEndPoint address;
		private bool sendAuthID, query, optional;

		public GameServer(String _name, IPEndPoint _address,
						  bool _sendAuthID, bool _query, bool _optional) {
			name = _name;
			address = _address;
			sendAuthID = _sendAuthID;
			query = _query;
			optional = _optional;
		}

		public String Name {
			get { return name; }
		}

		public IPEndPoint Address {
			get { return address; }
		}

		public bool SendAuthID {
			get { return sendAuthID; }
		}

		public bool Query {
			get { return query; }
		}

		public bool Optional {
			get { return optional; }
		}
	}

	public class GameServerList : IEnumerable {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private ArrayList servers = new ArrayList();

		public GameServerList() {
		}

		public GameServerList(XmlElement el) {
			foreach (XmlElement gs in el.GetElementsByTagName("game-server")) {
				string name = Parser.GetElementString(gs, "name");
				if (name == null) {
					log.Warn("Game server without name ignored");
					continue;
				}

				string addressString = Parser.GetElementString(gs, "address");
				if (addressString == null) {
					log.Warn("Game server without address ignored");
					continue;
				}

				string[] splitted = addressString.Split(new char[]{':'}, 2);
				if (splitted.Length != 2) {
					log.Warn("Game server without port ignored");
					continue;
				}

				IPAddress ip;
				try {
					IPHostEntry he = Dns.Resolve(splitted[0]);
					if (he.AddressList.Length == 0) {
						log.WarnFormat("Failed to resolve {0}", splitted[0]);
						continue;
					}
					ip = he.AddressList[he.AddressList.Length - 1];
				} catch (Exception e) {
					log.Warn(String.Format("Failed to resolve {0}", splitted[0]), e);
					continue;
				}

				short port;
				try {
					port = Int16.Parse(splitted[1]);
				} catch {
					log.Warn("Invalid game server port ignored");
					continue;
				}

				IPEndPoint address = new IPEndPoint(ip, port);

				bool sendAuthID = Parser.GetElementBool(gs, "send-auth-id", false);
				bool query = Parser.GetElementBool(gs, "query", false);
				bool optional = Parser.GetElementBool(gs, "optional", false);

				servers.Add(new GameServer(name, address, sendAuthID,
										   query, optional));
			}
		}

		public IEnumerator GetEnumerator() {
			return servers.GetEnumerator();
		}

		public int Count {
			get { return servers.Count; }
		}

		public GameServer this[int index] {
			get { return (GameServer)servers[index]; }
		}

		public GameServer this[string name] {
			get {
				foreach (GameServer gs in servers) {
					if (gs.Name == name)
						return gs;
				}
				return null;
			}
		}
	}

	public class Root {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private string filename;
		private XmlDocument document;
		private string serverName;
		private Features m_Features = new Features();
		private string m_BaseDirectory, m_ConfigDirectory,
			m_SaveDirectory, m_BackupDirectory, m_LogDirectory,
			m_CacheDirectory;
		private ArrayList m_DataDirectories;
		private Hashtable libraryConfig = new Hashtable();
		private Network m_Network;
		private Login loginConfig;
		private GameServerList gameServers;

		public Root(string _baseDirectory, string _filename) {
			m_BaseDirectory = _baseDirectory;
			m_ConfigDirectory = Path.Combine(m_BaseDirectory, "Data");
			m_SaveDirectory = Path.Combine(m_BaseDirectory, "Saves");
			m_BackupDirectory = Path.Combine(m_BaseDirectory, "Backups");

			DirectoryInfo base_dir = new DirectoryInfo(m_BaseDirectory);
			DirectoryInfo var_dir = base_dir.CreateSubdirectory("var");
			m_LogDirectory = var_dir.CreateSubdirectory("log").FullName;
			m_CacheDirectory = var_dir.CreateSubdirectory("cache").FullName;

			filename = _filename;

			Defaults();
			Load();
		}

		public bool Exists {
			get { return File.Exists(filename); }
		}

		public string ServerName {
			get { return serverName; }
		}

		public Features Features {
			get {
				return m_Features;
			}
		}

		public string BaseDirectory {
			get {
				return m_BaseDirectory;
			}
		}

		public string ConfigDirectory {
			get { return m_ConfigDirectory; }
		}

		public string SaveDirectory {
			get { return m_SaveDirectory; }
		}

		public string BackupDirectory {
			get { return m_BackupDirectory; }
		}

		public string LogDirectory {
			get { return m_LogDirectory; }
		}

		public string CacheDirectory {
			get { return m_CacheDirectory; }
		}

		public ArrayList DataDirectories {
			get { return m_DataDirectories; }
		}

		public Library GetLibrary(string name) {
			return (Library)libraryConfig[name];
		}
		public ICollection Libraries {
			get { return libraryConfig.Values; }
		}

		public Network Network {
			get { return m_Network; }
		}

		public Login Login {
			get { return loginConfig; }
		}

		public GameServerList GameServers {
			get { return gameServers; }
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

		private void Defaults() {
			Library coreConfig = new Library("core");
			libraryConfig["core"] = coreConfig;

			DirectoryInfo local = new DirectoryInfo(BaseDirectory)
				.CreateSubdirectory("local");

			/* find source libraries in ./local/src/ */
			DirectoryInfo src = local
				.CreateSubdirectory("src");
			foreach (DirectoryInfo sub in src.GetDirectories()) {
				string libName = sub.Name.ToLower();
				if (libraryConfig.ContainsKey(libName)) {
					log.WarnFormat("duplicate library '{0}' in '{1}'",
								   libName, sub.FullName);
					continue;
				}

				libraryConfig[libName] = new Library(libName, sub);
			}

			/* find binary libraries in ./local/lib/ */
			DirectoryInfo lib = local
				.CreateSubdirectory("lib");
			foreach (FileInfo libFile in lib.GetFiles("*.dll")) {
				string fileName = libFile.Name;
				string libName = fileName.Substring(0, fileName.Length - 4).ToLower();
				if (libraryConfig.ContainsKey(libName)) {
					log.WarnFormat("duplicate library '{0}' in '{1}'",
								   libName, libFile);
					continue;
				}

				libraryConfig[libName] = new Library(libName, libFile);
			}

			/* if the 'legacy' library was not found until now, load
			   the legacy scripts from ./Scripts/ */
			if (!libraryConfig.ContainsKey("legacy")) {
				DirectoryInfo legacy = new DirectoryInfo(Path.Combine(BaseDirectory,
																	  "Scripts"));
				if (legacy.Exists) {
					Library legacyConfig = new Library("legacy", legacy);
					libraryConfig[legacyConfig.Name] = legacyConfig;
				}
			}
		}

		public static void RemoveElement(XmlElement parent, string tag) {
			if (parent == null)
				return;
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			XmlNode[] children = new XmlNode[nl.Count];
			for (int i = 0; i < children.Length; i++)
				children[i] = nl.Item(i);
			foreach (XmlNode child in children)
				parent.RemoveChild(child);
		}

		public static void SetElementBool(XmlElement parent, string tag,
										  bool value) {
			XmlElement el;

			RemoveElement(parent, tag);

			el = parent.OwnerDocument.CreateElement(tag);
			el.SetAttribute("value", value ? "on" : "off");
			parent.AppendChild(el);
		}

		private void Load() {
			document = new XmlDocument();
			m_DataDirectories = new ArrayList();

			if (Exists) {
				XmlTextReader reader = new XmlTextReader(filename);
				try {
					document.Load(reader);
				} finally {
					reader.Close();
				}
			} else {
				document.AppendChild(document.CreateElement("sunuo-config"));
			}

			// section "global"
			XmlElement global = GetConfiguration("global");
			if (global != null) {
				foreach (XmlNode node in global.ChildNodes) {
					if (node.NodeType != XmlNodeType.Element)
						continue;

					XmlElement el = (XmlElement)node;

					switch (node.Name) {
					case "server-name":
						serverName = el.InnerText;
						break;

					case "multi-threading":
						m_Features[node.Name]
							= Parser.ParseBool(el.GetAttribute("value"), true);
						break;

					case "feature":
						m_Features[el.GetAttribute("name")]
							= Parser.ParseBool(el.GetAttribute("value"), true);
						break;

					default:
						log.WarnFormat("Invalid element global/{0}", node.Name);
						break;
					}
				}
			}

			// section "locations"
			XmlElement locations = GetConfiguration("locations");
			foreach (XmlNode node in locations.ChildNodes) {
				XmlElement el = node as XmlElement;
				if (el != null) {
					string path = el.InnerText;
					switch (el.Name) {
					case "config-dir":
						m_ConfigDirectory = path;
						break;

					case "save-dir":
						m_SaveDirectory = path;
						break;

					case "backup-dir":
						m_BackupDirectory = path;
						break;

					case "data-path":
						if (Directory.Exists(path))
							m_DataDirectories.Add(path);
						break;

					case "log-dir":
						m_LogDirectory = path;
						break;

					case "cache-dir":
						m_CacheDirectory = path;
						break;

					default:
						log.WarnFormat("Ignoring unknown location tag in {0}: {1}",
									   filename, el.Name);
						break;
					}
				}
			}

			// section "libraries"
			XmlElement librariesEl = GetConfiguration("libraries");
			foreach (XmlElement el in librariesEl.GetElementsByTagName("library")) {
				string name = el.GetAttribute("name");
				if (name == null || name == "") {
					log.Warn("library element without name attribute");
					continue;
				}

				name = name.ToLower();

				Library libConfig = (Library)libraryConfig[name];
				if (libConfig == null)
					libraryConfig[name] = libConfig = new Library(name);

				libConfig.Load(el);
			}

			if (!libraryConfig.ContainsKey("legacy"))
				libraryConfig["legacy"] = new Library("legacy");

			// section "network"
			XmlElement networkEl = GetConfiguration("network");
			m_Network = networkEl == null
				? new Network()
				: new Network(networkEl);

			// section "login"
			XmlElement loginEl = GetConfiguration("login");
			loginConfig = loginEl == null
				? new Login()
				: new Login(loginEl);

			// section "server-list"
			XmlElement serverListEl = GetConfiguration("server-list");
			if (serverListEl != null)
				gameServers = new GameServerList(serverListEl);
		}

		public void Save() {
			string tempFilename;
			if (filename.EndsWith(".xml")) {
				tempFilename = filename.Substring(0, filename.Length - 4) + ".new";
			} else {
				tempFilename = filename + ".new";
			}

			// section "locations"
			XmlElement locations = GetConfiguration("locations");
			RemoveElement(locations, "data-path");

			Hashtable dirHash = new Hashtable();

			foreach (string path in	m_DataDirectories) {
				/* check for double path */
				if (dirHash.ContainsKey(path))
					continue;
				dirHash[path] = true;

				XmlElement el = document.CreateElement("data-path");
				el.InnerText = path;
				locations.AppendChild(el);
			}

			// section "login"
			XmlElement login = GetConfiguration("login");

			SetElementBool(login, "ignore-auth-id", loginConfig.IgnoreAuthID);

			// write to file
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
