/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2006 Max Kellermann <max@duempel.org>
 * Based on source from an unknown author.
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

namespace Server {
	public class Oldschool {
		public static string ConvertItemName( int locid )
		{
			return ConvertItemName( locid, 1 );
		}

		public static string ConvertItemName( int locid, int amount )
		{
			return ConvertItemName( StringList.Localization[locid], amount );
		}

		public static string ConvertItemName( string itemname )
		{
			return ConvertItemName( itemname, 1 );
		}

		public static string ConvertItemName( string itemname, int amount )
		{
			if ( itemname == null || itemname == "" )
				return "";

			itemname = itemname.ToLower();

			bool plurals = false;

			if ( itemname.IndexOf( "%s%" ) != -1 )
			{
				plurals = true;
				itemname = itemname.Substring( 0, itemname.IndexOf( "%s%" ) );
			}

			if ( itemname.IndexOf( "%s" ) != -1 )
			{
				plurals = true;
				itemname = itemname.Substring( 0, itemname.IndexOf( "%s" ) );
			}

			string name = null;
			if ( amount > 1 )
			{
				name = String.Format( "{0} {1}", amount, itemname );
				if ( plurals )
					name = name + "s";
			}
			else if ( itemname.Length >= 2 && ((itemname.Length >= 3 && itemname.IndexOf( "an " ) == 0 || itemname.IndexOf( "a " ) == 0) || (itemname[itemname.Length - 1] == 's' && itemname[itemname.Length - 2] != 's')) )
			{
				name = itemname;
			}
			else
			{
				char [] vowels = new char[] { 'a', 'e', 'i', 'o', 'u', 'y' };
				bool beginwithvowel = false;
				for ( int i = 0; i < vowels.Length; i++ )
				{
					if ( itemname[0] == vowels[i] )
					{
						beginwithvowel = true;
						break;
					}
				}
				if ( beginwithvowel )
					name = "an " + itemname;
				else
					name = "a " + itemname;
			}
			return name;
		}
	}
}
