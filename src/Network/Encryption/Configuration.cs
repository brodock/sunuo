using System;
using System.Globalization;
using System.Collections;
using System.Xml;
using Server;

namespace Server.Network.Encryption 
{
	public class Configuration 
	{
		private static XmlElement MyConfig {
			get {
				return Core.Config.GetConfiguration("encryption");
			}
		}

		// Set this to true to enable this subsystem.
		public static bool Enabled {
			get {
				return Config.Parser.GetElementBool(MyConfig, "enabled", false);
			}
		}

		// Set this to false to disconnect unencrypted connections.
		public static bool AllowUnencryptedClients {
			get {
				return Config.Parser.GetElementBool(MyConfig, "allow-unencrypted", true);
			}
		}

		// This is the list of supported game encryption keys.
		// You can use the utility found at http://www.hartte.de/ExtractKeys.exe
		// to extract the neccesary keys from any client version.
		public static LoginKey[] DefaultLoginKeys = new LoginKey[]
		{
			new LoginKey("5.0.1", 0x2eaba7ec, 0xa2417e7f),
			new LoginKey("5.0.0", 0x2E93A5FC, 0xA25D527F),
			new LoginKey("4.0.11", 0x2C7B574C, 0xA32D9E7F),
			new LoginKey("4.0.10", 0x2C236D5C, 0xA300A27F),
			new LoginKey("4.0.9", 0x2FEB076C, 0xA2E3BE7F),
			new LoginKey("4.0.8", 0x2FD3257C, 0xA2FF527F),
			new LoginKey("4.0.7", 0x2F9BC78D, 0xA2DBFE7F),
			new LoginKey("4.0.6", 0x2F43ED9C, 0xA2B4227F),
			new LoginKey("4.0.5", 0x2F0B97AC, 0xA290DE7F),
			new LoginKey("4.0.4", 0x2EF385BC, 0xA26D127F),
			new LoginKey("4.0.3", 0x2EBBB7CC, 0xA2495E7F),
			new LoginKey("4.0.2", 0x2E63ADDC, 0xA225227F),
			new LoginKey("4.0.1", 0x2E2BA7EC, 0xA2017E7F),
			new LoginKey("4.0.0", 0x2E13A5FC, 0xA21D527F),
			new LoginKey("3.0.8", 0x2C53257C, 0xA33F527F),
			new LoginKey("3.0.7", 0x2C1BC78C, 0xA31BFE7F),
			new LoginKey("3.0.6", 0x2CC3ED9C, 0xA374227F),
			new LoginKey("3.0.5", 0x2C8B97AC, 0xA350DE7F),
		};

		public static LoginKey[] LoginKeys {
			get {
				Hashtable keys = new Hashtable();
				foreach (LoginKey key in DefaultLoginKeys) {
					keys[key.Name] = key;
				}

				foreach (XmlElement el in MyConfig.GetElementsByTagName("login-key")) {
					String name = el.GetAttribute("name");
					String key1 = el.GetAttribute("key1");
					String key2 = el.GetAttribute("key2");

					if (name == null || key1 == null || key2 == null) {
						Console.Error.WriteLine("login-key parameter missing");
						continue;
					}

					if (!key1.StartsWith("0x") || !key2.StartsWith("0x")) {
						Console.Error.WriteLine("login-key data must start with '0x', followed by hexadecimal digits");
						continue;
					}

					uint key1b = UInt32.Parse(key1.Substring(2), NumberStyles.HexNumber);
					uint key2b = UInt32.Parse(key2.Substring(2), NumberStyles.HexNumber);

					keys[name] = new LoginKey(name, key1b, key2b);
				}

				return (LoginKey[])new ArrayList(keys.Values).ToArray(typeof(LoginKey));
			}
		}
	}
}
