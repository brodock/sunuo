using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace Server
{
	public class StringList
	{
		private Hashtable m_Table;
		private StringEntry[] m_Entries;
		private string m_Language;
		private static StringList m_Localization;

		public StringEntry[] Entries{ get{ return m_Entries; } }
		public Hashtable Table{ get{ return m_Table; } }
		public string Language{ get{ return m_Language; } }
		//public static StringList Localization{ get{ return ( m_Localization == null ) ? (m_Localization = new StringList( "ENU" )) : m_Localization; } }
		public static StringList Localization{ get{ if ( m_Localization == null ) m_Localization = new StringList(); return m_Localization; } set{ m_Localization = value; } }
		public string this[int number]{ get{ return (string)m_Table[number]; } }

		private static byte[] m_Buffer = new byte[1024];

		public static void Initialize()
		{
			if ( m_Localization == null )
				m_Localization = new StringList();
		}

		public StringList() : this( "ENU" )
		{
		}

		public StringList( string language ) : this( language, true )
		{
		}

		public StringList( string language, bool format )
		{
			m_Language = language;
			m_Table = new Hashtable();

			string path = Core.FindDataFile( String.Format( "cliloc.{0}", language ) );

			if ( path == null )
			{
				Console.WriteLine( "Warning: cliloc.{0} not found", language );
				m_Entries = new StringEntry[0];
				return;
			}

			Console.Write( "Localization strings: Loading..." );

			ArrayList list = new ArrayList();

			using ( BinaryReader bin = new BinaryReader( new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ) ) )
			{
				bin.ReadInt32();
				bin.ReadInt16();

				while ( bin.PeekChar() != -1 )
				{
					int number = bin.ReadInt32();
					bin.ReadByte();
					int length = bin.ReadInt16();

					if ( length > m_Buffer.Length )
						m_Buffer = new byte[(length + 1023) & ~1023];

					bin.Read( m_Buffer, 0, length );
					string text = Encoding.UTF8.GetString( m_Buffer, 0, length );

					if ( format )
						text = FormatArguments( text );

					list.Add( new StringEntry( number, text ) );
					m_Table[number] = text;
				}
			}

			m_Entries = (StringEntry[])list.ToArray( typeof( StringEntry ) );

			Console.WriteLine( "done" );
		}

		//C# argument support
		public static Regex FormatExpression = new Regex( @"~(\d)+_.*?~", RegexOptions.IgnoreCase );

		public static string MatchComparison( Match m )
		{
			return "{" + (Utility.ToInt32( m.Groups[1].Value ) - 1) + "}";
		}

		public static string FormatArguments( string entry )
		{
			return FormatExpression.Replace( entry, new MatchEvaluator( MatchComparison ) );
		}

		//UO tabbed argument conversion
		public static string CombineArguments( string str, string args )
		{
			return CombineArguments( str, args.Split( new char[]{ '\t' } ) );
		}

		public static string CombineArguments( string str, params object[] args )
		{
			return String.Format( str, args );
		}

		public static string CombineArguments( int number, string args )
		{
			return CombineArguments( number, args.Split( new char[]{ '\t' } ) );
		}

		public static string CombineArguments( int number, params object[] args )
		{
			return String.Format( StringList.Localization[number], args );
		}
	}

	public class StringEntry
	{
		private int m_Number;
		private string m_Text;

		public int Number{ get{ return m_Number; } }
		public string Text{ get{ return m_Text; } }

		public StringEntry( int number, string text )
		{
			m_Number = number;
			m_Text = text;
		}

		public StringEntry( GenericReader reader )
		{
			m_Number = reader.ReadInt();
			m_Text = reader.ReadString();
		}

		public void Serialize( GenericWriter writer )
		{
			writer.Write( m_Number );
			writer.Write( m_Text );
		}
	}
}
