using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Accounting
{
	public class Accounts
	{
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
			return m_Accounts[username] as Account;
		}

		public static Account AddAccount( string user, string pass )
		{
			Account a = new Account( user, pass );
			if ( m_Accounts.Count == 0 )
				a.AccessLevel = AccessLevel.Administrator;

			m_Accounts[a.Username] = a;

			return a;
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
			if ( !Directory.Exists( "Saves/Accounts" ) )
				Directory.CreateDirectory( "Saves/Accounts" );

			string filePath = Path.Combine( "Saves/Accounts", "accounts.xml" );

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

			string filePath = Path.Combine( "Saves/Accounts", "accounts.xml" );

			if ( !File.Exists( filePath ) )
				return;

			XmlDocument doc = new XmlDocument();
			doc.Load( filePath );

			XmlElement root = doc["accounts"];

			foreach ( XmlElement account in root.GetElementsByTagName( "account" ) )
			{
				try
				{
					Account acct = new Account( account );

					m_Accounts[acct.Username] = acct;
				}
				catch
				{
					Console.WriteLine( "Warning: Account instance load failed" );
				}
			}
		}
	}
}