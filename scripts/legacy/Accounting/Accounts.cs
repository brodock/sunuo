using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Accounting
{
	public class Accounts
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static Hashtable m_Accounts = new Hashtable();

		public static void Configure()
		{
			EventSink.WorldLoad += new WorldLoadEventHandler( Load );
			EventSink.WorldSave += new WorldSaveEventHandler( Save );
		}

		static Accounts()
		{
		}

		public static Hashtable Table
		{
			get
			{
				return m_Accounts;
			}
		}

		public static Account GetAccount( string username )
		{
			lock (m_Accounts.SyncRoot) {
				return m_Accounts[username] as Account;
			}
		}

		public static Account AddAccount( string user, string pass )
		{
			lock (m_Accounts.SyncRoot) {
				Account a = new Account( user, pass );
				if ( m_Accounts.Count == 0 )
					a.AccessLevel = AccessLevel.Administrator;

				m_Accounts[a.Username] = a;

				return a;
			}
		}

		public static int GetInt32( string intString, int defaultValue )
		{
			try
			{
				return XmlConvert.ToInt32( intString );
			}
			catch
			{
				try
				{
					return Convert.ToInt32( intString );
				}
				catch
				{
					return defaultValue;
				}
			}
		}

		public static DateTime GetDateTime( string dateTimeString, DateTime defaultValue )
		{
			try
			{
				return XmlConvert.ToDateTime( dateTimeString );
			}
			catch
			{
				try
				{
					return DateTime.Parse( dateTimeString );
				}
				catch
				{
					return defaultValue;
				}
			}
		}

		public static TimeSpan GetTimeSpan( string timeSpanString, TimeSpan defaultValue )
		{
			try
			{
				return XmlConvert.ToTimeSpan( timeSpanString );
			}
			catch
			{
				return defaultValue;
			}
		}

		public static string GetAttribute( XmlElement node, string attributeName )
		{
			return GetAttribute( node, attributeName, null );
		}

		public static string GetAttribute( XmlElement node, string attributeName, string defaultValue )
		{
			if ( node == null )
				return defaultValue;

			XmlAttribute attr = node.Attributes[attributeName];

			if ( attr == null )
				return defaultValue;

			return attr.Value;
		}

		public static string GetText( XmlElement node, string defaultValue )
		{
			if ( node == null )
				return defaultValue;

			return node.InnerText;
		}

		public static void Save( WorldSaveEventArgs e )
		{
			string saveDirectory = Path.Combine(e.SaveDirectory, "Accounts");
			if (!Directory.Exists(saveDirectory))
				Directory.CreateDirectory(saveDirectory);

			string filePath = Path.Combine(saveDirectory, "accounts.xml");

			using ( StreamWriter op = new StreamWriter( filePath ) )
			{
				XmlTextWriter xml = new XmlTextWriter( op );

				xml.Formatting = Formatting.Indented;
				xml.IndentChar = '\t';
				xml.Indentation = 1;

				xml.WriteStartDocument( true );

				xml.WriteStartElement( "accounts" );

				xml.WriteAttributeString( "count", m_Accounts.Count.ToString() );

				foreach ( Account a in Accounts.Table.Values )
					a.Save( xml );

				xml.WriteEndElement();

				xml.Close();
			}
		}

		public static void Load()
		{
			m_Accounts = new Hashtable( 32, 1.0f, CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default );

			string saveDirectory = Path.Combine(Core.Config.SaveDirectory, "Accounts");
			string filePath = Path.Combine(saveDirectory, "accounts.xml");

			if ( !File.Exists( filePath ) )
				return;

			log.Debug("loading accounts");

			XmlDocument doc = new XmlDocument();
			XmlReader reader = new XmlTextReader(filePath);

			while (reader.Read()) {
				if (reader.NodeType != XmlNodeType.Element ||
					reader.Name != "account")
					continue;

				XmlElement account = (XmlElement)doc.ReadNode(reader);

				try
				{
					Account acct = new Account( account );

					m_Accounts[acct.Username] = acct;
				}
				catch(Exception e)
				{
					log.Error("Account instance load failed", e);
				}
			}
		}
	}
}
