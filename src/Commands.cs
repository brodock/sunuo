/***************************************************************************
 *                                Commands.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Commands.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
 *   $Author: krrios $
 *   $Date: 2005/01/22 04:25:04 $
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
using System.IO;
using System.Collections;
using Server.Network;
using Server.Gumps;
using Server.Guilds;
using Server.Targeting;
using Server.Menus;
using Server.Menus.Questions;
using Server.Menus.ItemLists;
using Server.Items;

namespace Server
{
	public delegate void CommandEventHandler( CommandEventArgs e );

	public class CommandEventArgs : EventArgs
	{
		private Mobile m_Mobile;
		private string m_Command, m_ArgString;
		private string[] m_Arguments;

		public Mobile Mobile
		{
			get
			{
				return m_Mobile;
			}
		}

		public string Command
		{
			get
			{
				return m_Command;
			}
		}

		public string ArgString
		{
			get
			{
				return m_ArgString;
			}
		}

		public string[] Arguments
		{
			get
			{
				return m_Arguments;
			}
		}

		public int Length
		{
			get
			{
				return m_Arguments.Length;
			}
		}

		public string GetString( int index )
		{
			if ( index < 0 || index >= m_Arguments.Length )
				return "";

			return m_Arguments[index];
		}

		public int GetInt32( int index )
		{
			if ( index < 0 || index >= m_Arguments.Length )
				return 0;

			return Utility.ToInt32( m_Arguments[index] );
		}

		public bool GetBoolean( int index )
		{
			if ( index < 0 || index >= m_Arguments.Length )
				return false;

			return Utility.ToBoolean( m_Arguments[index] );
		}

		public double GetDouble( int index )
		{
			if ( index < 0 || index >= m_Arguments.Length )
				return 0.0;

			return Utility.ToDouble( m_Arguments[index] );
		}

		public TimeSpan GetTimeSpan( int index )
		{
			if ( index < 0 || index >= m_Arguments.Length )
				return TimeSpan.Zero;

			return Utility.ToTimeSpan( m_Arguments[index] );
		}

		public CommandEventArgs( Mobile mobile, string command, string argString, string[] arguments )
		{
			m_Mobile = mobile;
			m_Command = command;
			m_ArgString = argString;
			m_Arguments = arguments;
		}
	}

	public class CommandEntry : IComparable
	{
		private string m_Command;
		private CommandEventHandler m_Handler;
		private AccessLevel m_AccessLevel;

		public string Command
		{
			get
			{
				return m_Command;
			}
		}

		public CommandEventHandler Handler
		{
			get
			{
				return m_Handler;
			}
		}

		public AccessLevel AccessLevel
		{
			get
			{
				return m_AccessLevel;
			}
		}

		public CommandEntry( string command, CommandEventHandler handler, AccessLevel accessLevel )
		{
			m_Command = command;
			m_Handler = handler;
			m_AccessLevel = accessLevel;
		}

		public int CompareTo( object obj )
		{
			if ( obj == this )
				return 0;
			else if ( obj == null )
				return 1;

			CommandEntry e = obj as CommandEntry;

			if ( e == null )
				throw new ArgumentException();

			return m_Command.CompareTo( e.m_Command );
		}
	}

	public class Commands
	{
		private static string m_CommandPrefix = "[";

		public static string CommandPrefix
		{
			get
			{
				return m_CommandPrefix;
			}
			set
			{
				m_CommandPrefix = value;
			}
		}

		public static string[] Split( string value )
		{
			char[] array = value.ToCharArray();
			ArrayList list = new ArrayList();

			int start = 0, end = 0;

			while ( start < array.Length )
			{
				char c = array[start];

				if ( c == '"' )
				{
					++start;
					end = start;

					while ( end < array.Length )
					{
						if ( array[end] != '"' || array[end - 1] == '\\' )
							++end;
						else
							break;
					}

					list.Add( value.Substring( start, end - start ) );

					start = end + 2;
				}
				else if ( c != ' ' )
				{
					end = start;

					while ( end < array.Length )
					{
						if ( array[end] != ' ' )
							++end;
						else
							break;
					}

					list.Add( value.Substring( start, end - start ) );

					start = end + 1;
				}
				else
				{
					++start;
				}
			}

			return (string[])list.ToArray( typeof( string ) );
		}

		private static Hashtable m_Entries;

		public static Hashtable Entries
		{
			get
			{
				return m_Entries;
			}
		}

		static Commands()
		{
			m_Entries = new Hashtable( CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default );
		}

		public static void Register( string command, AccessLevel access, CommandEventHandler handler )
		{
			m_Entries[command] = new CommandEntry( command, handler, access );
		}

		private static AccessLevel m_BadCommandIngoreLevel = AccessLevel.Player;

		public static AccessLevel BadCommandIgnoreLevel{ get{ return m_BadCommandIngoreLevel; } set{ m_BadCommandIngoreLevel = value; } }

		public static bool Handle( Mobile from, string text )
		{
			if ( text.StartsWith( m_CommandPrefix ) )
			{
				text = text.Substring( m_CommandPrefix.Length );

				int indexOf = text.IndexOf( ' ' );

				string command;
				string[] args;
				string argString;

				if ( indexOf >= 0 )
				{
					argString = text.Substring( indexOf + 1 );

					command = text.Substring( 0, indexOf );
					args = Split( argString );
				}
				else
				{
					argString = "";
					command = text.ToLower();
					args = new string[0];
				}

				CommandEntry entry = (CommandEntry)m_Entries[command];

				if ( entry != null )
				{
					if ( from.AccessLevel >= entry.AccessLevel )
					{
						if ( entry.Handler != null )
						{
							CommandEventArgs e = new CommandEventArgs( from, command, argString, args );
							entry.Handler( e );
							EventSink.InvokeCommand( e );
						}
					}
					else
					{
						if ( from.AccessLevel <= m_BadCommandIngoreLevel )
							return false;

						from.SendMessage( "You do not have access to that command." );
					}
				}
				else
				{
					if ( from.AccessLevel <= m_BadCommandIngoreLevel )
						return false;

					from.SendMessage( "That is not a valid command." );
				}

				return true;
			}

			return false;
		}
	}
}