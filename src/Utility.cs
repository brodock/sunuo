/***************************************************************************
 *                                Utility.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Utility.cs,v 1.7 2005/01/22 04:25:04 krrios Exp $
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
using System.Collections;
using System.Reflection;
using System.Net;
using System.IO;
using System.Text;
using Server.Network;
using Microsoft.Win32;

namespace Server
{
	public class Utility
	{
		private static Random m_Random = new Random();
		private static Encoding m_UTF8, m_UTF8WithEncoding;

		public static Encoding UTF8
		{
			get
			{
				if ( m_UTF8 == null )
					m_UTF8 = new UTF8Encoding( false, false );

				return m_UTF8;
			}
		}

		public static Encoding UTF8WithEncoding
		{
			get
			{
				if ( m_UTF8WithEncoding == null )
					m_UTF8WithEncoding = new UTF8Encoding( true, false );

				return m_UTF8WithEncoding;
			}
		}

		public static bool IsValidIP( string text )
		{
			bool valid = true;

			IPMatch( text, IPAddress.None, ref valid );

			return valid;
		}

		public static bool IPMatch( string val, IPAddress ip )
		{
			bool valid = true;

			return IPMatch( val, ip, ref valid );
		}

		public static string FixHtml( string str )
		{
			bool hasOpen  = ( str.IndexOf( '<' ) >= 0 );
			bool hasClose = ( str.IndexOf( '>' ) >= 0 );
			bool hasPound = ( str.IndexOf( '#' ) >= 0 );

			if ( !hasOpen && !hasClose && !hasPound )
				return str;

			StringBuilder sb = new StringBuilder( str );

			if ( hasOpen )
				sb.Replace( '<', '(' );

			if ( hasClose )
				sb.Replace( '>', ')' );

			if ( hasPound )
				sb.Replace( '#', '-' );

			return sb.ToString();
		}

		public static bool IPMatch( string val, IPAddress ip, ref bool valid )
		{
			valid = true;

			string[] split = val.Split( '.' );

			for ( int i = 0; i < 4; ++i )
			{
				int lowPart, highPart;

				if ( i >= split.Length )
				{
					lowPart = 0;
					highPart = 255;
				}
				else
				{
					string pattern = split[i];

					if ( pattern == "*" )
					{
						lowPart = 0;
						highPart = 255;
					}
					else
					{
						lowPart = 0;
						highPart = 0;

						bool highOnly = false;
						int lowBase = 10;
						int highBase = 10;

						for ( int j = 0; j < pattern.Length; ++j )
						{
							char c = (char)pattern[j];

							if ( c == '?' )
							{
								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += 0;
								}

								highPart *= highBase;
								highPart += highBase - 1;
							}
							else if ( c == '-' )
							{
								highOnly = true;
								highPart = 0;
							}
							else if ( c == 'x' || c == 'X' )
							{
								lowBase = 16;
								highBase = 16;
							}
							else if ( c >= '0' && c <= '9' )
							{
								int offset = c - '0';

								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += offset;
								}

								highPart *= highBase;
								highPart += offset;
							}
							else if ( c >= 'a' && c <= 'f' )
							{
								int offset = 10 + (c - 'a');

								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += offset;
								}

								highPart *= highBase;
								highPart += offset;
							}
							else if ( c >= 'A' && c <= 'F' )
							{
								int offset = 10 + (c - 'A');

								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += offset;
								}

								highPart *= highBase;
								highPart += offset;
							}
							else
							{
								valid = false;
							}
						}
					}
				}

				int b = (byte)(ip.Address >> (i * 8));

				if ( b < lowPart || b > highPart )
					return false;
			}

			return true;
		}

		public static bool IPMatchClassC( IPAddress ip1, IPAddress ip2 )
		{
			return ( (ip1.Address & 0xFFFFFF) == (ip2.Address & 0xFFFFFF) );
		}

		/*public static bool IPMatch( string val, IPAddress ip )
		{
			string[] split = val.Split( '.' );

			for ( int i = 0; i < split.Length; ++i )
			{
				int b = (byte)(ip.Address >> (i * 8));
				string s = split[i];

				if ( s == "*" )
					continue;

				if ( ToInt32( s ) != b )
					return false;
			}

			return true;
		}*/

		public static int InsensitiveCompare( string first, string second )
		{
			return Insensitive.Compare( first, second );
		}

		public static bool InsensitiveStartsWith( string first, string second )
		{
			return Insensitive.StartsWith( first, second );
		}

		public static bool ToBoolean( string value )
		{
			try
			{
				return Convert.ToBoolean( value );
			}
			catch
			{
				return false;
			}
		}

		public static double ToDouble( string value )
		{
			try
			{
				return Convert.ToDouble( value );
			}
			catch
			{
				return 0.0;
			}
		}

		public static TimeSpan ToTimeSpan( string value )
		{
			try
			{
				return TimeSpan.Parse( value );
			}
			catch
			{
				return TimeSpan.Zero;
			}
		}

		public static int ToInt32( string value )
		{
			try
			{
				if ( value.StartsWith( "0x" ) )
				{
					return Convert.ToInt32( value.Substring( 2 ), 16 );
				}
				else
				{
					return Convert.ToInt32( value );
				}
			}
			catch
			{
				return 0;
			}
		}

		public static double RandomDouble()
		{
			return m_Random.NextDouble();
		}

		public static bool InRange( Point3D p1, Point3D p2, int range )
		{
			return ( p1.m_X >= (p2.m_X - range) )
				&& ( p1.m_X <= (p2.m_X + range) )
				&& ( p1.m_Y >= (p2.m_Y - range) )
				&& ( p1.m_Y <= (p2.m_Y + range) );
		}

		public static bool InUpdateRange( Point3D p1, Point3D p2 )
		{
			return ( p1.m_X >= (p2.m_X - 18) )
				&& ( p1.m_X <= (p2.m_X + 18) )
				&& ( p1.m_Y >= (p2.m_Y - 18) )
				&& ( p1.m_Y <= (p2.m_Y + 18) );
		}

		public static bool InUpdateRange( Point2D p1, Point2D p2 )
		{
			return ( p1.m_X >= (p2.m_X - 18) )
				&& ( p1.m_X <= (p2.m_X + 18) )
				&& ( p1.m_Y >= (p2.m_Y - 18) )
				&& ( p1.m_Y <= (p2.m_Y + 18) );
		}

		public static bool InUpdateRange( IPoint2D p1, IPoint2D p2 )
		{
			return ( p1.X >= (p2.X - 18) )
				&& ( p1.X <= (p2.X + 18) )
				&& ( p1.Y >= (p2.Y - 18) )
				&& ( p1.Y <= (p2.Y + 18) );
		}

		public static Direction GetDirection( IPoint2D from, IPoint2D to )
		{
			int dx = to.X - from.X;
			int dy = to.Y - from.Y;

			int adx = Math.Abs( dx );
			int ady = Math.Abs( dy );

			if ( adx >= ady * 3 )
			{
				if ( dx > 0 )
					return Direction.East;
				else
					return Direction.West;
			}
			else if ( ady >= adx * 3 )
			{
				if ( dy > 0 )
					return Direction.South;
				else
					return Direction.North;
			}
			else if ( dx > 0 )
			{
				if ( dy > 0 )
					return Direction.Down;
				else
					return Direction.Right;
			}
			else
			{
				if ( dy > 0 )
					return Direction.Left;
				else
					return Direction.Up;
			}
		}

		public static bool CanMobileFit( int z, Tile[] tiles )
		{
			int checkHeight = 15;
			int checkZ = z;

			for ( int i = 0; i < tiles.Length; ++i )
			{
				Tile tile = tiles[i];

				if ( ((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))/* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*/ )
				{
					return false;
				}
				else if ( checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z )
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsInContact( Tile check, Tile[] tiles )
		{
			int checkHeight = check.Height;
			int checkZ = check.Z;

			for ( int i = 0; i < tiles.Length; ++i )
			{
				Tile tile = tiles[i];

				if ( ((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))/* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*/ )
				{
					return true;
				}
				else if ( checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z )
				{
					return true;
				}
			}

			return false;
		}

		public static object GetArrayCap( Array array, int index )
		{
			return GetArrayCap( array, index, null );
		}

		public static object GetArrayCap( Array array, int index, object emptyValue )
		{
			if ( array.Length > 0 )
			{
				if ( index < 0 )
				{
					index = 0;
				}
				else if ( index >= array.Length )
				{
					index = array.Length - 1;
				}

				return array.GetValue( index );
			}
			else
			{
				return emptyValue;
			}
		}

		//4d6+8 would be: Utility.Dice( 4, 6, 8 )
		public static int Dice( int numDice, int numSides, int bonus )
		{
			int total = 0;
			for (int i=0;i<numDice;++i)
				total += Random( numSides ) + 1;
			total += bonus;
			return total;
		}

		public static int RandomList( params int[] list )
		{
			return list[m_Random.Next( list.Length )];
		}

		public static bool RandomBool()
		{
			return ( m_Random.Next( 2 ) == 0 );
		}

		public static int RandomMinMax( int min, int max )
		{
			if ( min > max )
			{
				int copy = min;
				min = max;
				max = copy;
			}
			else if ( min == max )
			{
				return min;
			}

			return min + m_Random.Next( (max - min) + 1 );
		}

		public static int Random( int from, int count )
		{
			if ( count == 0 )
			{
				return from;
			}
			else if ( count > 0 )
			{
				return from + m_Random.Next( count );
			}
			else
			{
				return from - m_Random.Next( -count );
			}
		}

		public static int Random( int count )
		{
			return m_Random.Next( count );
		}

		public static int RandomNondyedHue()
		{
			switch ( Random( 6 ) )
			{
				case 0: return RandomPinkHue();
				case 1: return RandomBlueHue();
				case 2: return RandomGreenHue();
				case 3: return RandomOrangeHue();
				case 4: return RandomRedHue();
				case 5: return RandomYellowHue();
			}

			return 0;
		}

		public static int RandomPinkHue()
		{
			return Random( 1201, 54 );
		}

		public static int RandomBlueHue()
		{
			return Random( 1301, 54 );
		}

		public static int RandomGreenHue()
		{
			return Random( 1401, 54 );
		}

		public static int RandomOrangeHue()
		{
			return Random( 1501, 54 );
		}

		public static int RandomRedHue()
		{
			return Random( 1601, 54 );
		}

		public static int RandomYellowHue()
		{
			return Random( 1701, 54 );
		}

		public static int RandomNeutralHue()
		{
			return Random( 1801, 108 );
		}

		public static int RandomSnakeHue()
		{
			return Random( 2001, 18 );
		}

		public static int RandomBirdHue()
		{
			return Random( 2101, 30 );
		}

		public static int RandomSlimeHue()
		{
			return Random( 2201, 24 );
		}

		public static int RandomAnimalHue()
		{
			return Random( 2301, 18 );
		}

		public static int RandomMetalHue()
		{
			return Random( 2401, 30 );
		}

		public static int ClipDyedHue( int hue )
		{
			if ( hue < 2 )
				return 2;
			else if ( hue > 1001 )
				return 1001;
			else
				return hue;
		}

		public static int RandomDyedHue()
		{
			return Random( 2, 1000 );
		}

		public static int ClipSkinHue( int hue )
		{
			if ( hue < 1002 )
				return 1002;
			else if ( hue > 1058 )
				return 1058;
			else
				return hue;
		}

		public static int RandomSkinHue()
		{
			return Random( 1002, 57 ) | 0x8000;
		}

		public static int ClipHairHue( int hue )
		{
			if ( hue < 1102 )
				return 1102;
			else if ( hue > 1149 )
				return 1149;
			else
				return hue;
		}

		public static int RandomHairHue()
		{
			return Random( 1102, 48 );
		}

		private static SkillName[] m_AllSkills = new SkillName[]
			{
				SkillName.Alchemy,
				SkillName.Anatomy,
				SkillName.AnimalLore,
				SkillName.ItemID,
				SkillName.ArmsLore,
				SkillName.Parry,
				SkillName.Begging,
				SkillName.Blacksmith,
				SkillName.Fletching,
				SkillName.Peacemaking,
				SkillName.Camping,
				SkillName.Carpentry,
				SkillName.Cartography,
				SkillName.Cooking,
				SkillName.DetectHidden,
				SkillName.Discordance,
				SkillName.EvalInt,
				SkillName.Healing,
				SkillName.Fishing,
				SkillName.Forensics,
				SkillName.Herding,
				SkillName.Hiding,
				SkillName.Provocation,
				SkillName.Inscribe,
				SkillName.Lockpicking,
				SkillName.Magery,
				SkillName.MagicResist,
				SkillName.Tactics,
				SkillName.Snooping,
				SkillName.Musicianship,
				SkillName.Poisoning,
				SkillName.Archery,
				SkillName.SpiritSpeak,
				SkillName.Stealing,
				SkillName.Tailoring,
				SkillName.AnimalTaming,
				SkillName.TasteID,
				SkillName.Tinkering,
				SkillName.Tracking,
				SkillName.Veterinary,
				SkillName.Swords,
				SkillName.Macing,
				SkillName.Fencing,
				SkillName.Wrestling,
				SkillName.Lumberjacking,
				SkillName.Mining,
				SkillName.Meditation,
				SkillName.Stealth,
				SkillName.RemoveTrap,
				SkillName.Necromancy,
				SkillName.Focus,
				SkillName.Chivalry,
				SkillName.Bushido,
				SkillName.Ninjitsu
			};

		private static SkillName[] m_CombatSkills = new SkillName[]
			{
				SkillName.Archery,
				SkillName.Swords,
				SkillName.Macing,
				SkillName.Fencing,
				SkillName.Wrestling
			};

		private static SkillName[] m_CraftSkills = new SkillName[]
			{
				SkillName.Alchemy,
				SkillName.Blacksmith,
				SkillName.Fletching,
				SkillName.Carpentry,
				SkillName.Cartography,
				SkillName.Cooking,
				SkillName.Inscribe,
				SkillName.Tailoring,
				SkillName.Tinkering
			};

		public static SkillName RandomSkill()
		{
			return m_AllSkills[Utility.Random(m_AllSkills.Length - ( Core.SE ? 0 : Core.AOS ? 2 : 5 ) )];
		}

		public static SkillName RandomCombatSkill()
		{
			return m_CombatSkills[Utility.Random(m_CombatSkills.Length)];
		}

		public static SkillName RandomCraftSkill()
		{
			return m_CraftSkills[Utility.Random(m_CraftSkills.Length)];
		}

		public static void FixPoints( ref Point3D top, ref Point3D bottom )
		{
			if ( bottom.m_X < top.m_X )
			{
				int swap = top.m_X;
				top.m_X = bottom.m_X;
				bottom.m_X = swap;
			}

			if ( bottom.m_Y < top.m_Y )
			{
				int swap = top.m_Y;
				top.m_Y = bottom.m_Y;
				bottom.m_Y = swap;
			}

			if ( bottom.m_Z < top.m_Z )
			{
				int swap = top.m_Z;
				top.m_Z = bottom.m_Z;
				bottom.m_Z = swap;
			}
		}

		public static ArrayList BuildArrayList( IEnumerable enumerable )
		{
			IEnumerator e = enumerable.GetEnumerator();

			ArrayList list = new ArrayList();

			while ( e.MoveNext() )
			{
				list.Add( e.Current );
			}

			return list;
		}

		public static bool RangeCheck( IPoint2D p1, IPoint2D p2, int range )
		{
			return ( p1.X >= (p2.X - range) )
				&& ( p1.X <= (p2.X + range) )
				&& ( p1.Y >= (p2.Y - range) )
				&& ( p2.Y <= (p2.Y + range) );
		}

		public static void FormatBuffer( TextWriter output, Stream input, int length )
		{
			output.WriteLine( "        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F" );
			output.WriteLine( "       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --" );

			int byteIndex = 0;

			int whole = length >> 4;
			int rem = length & 0xF;

			for ( int i = 0; i < whole; ++i, byteIndex += 16 )
			{
				StringBuilder bytes = new StringBuilder( 49 );
				StringBuilder chars = new StringBuilder( 16 );

				for ( int j = 0; j < 16; ++j )
				{
					int c = input.ReadByte();

					bytes.Append( c.ToString( "X2" ) );

					if ( j != 7 )
					{
						bytes.Append( ' ' );
					}
					else
					{
						bytes.Append( "  " );
					}

					if ( c >= 0x20 && c < 0x80 )
					{
						chars.Append( (char)c );
					}
					else
					{
						chars.Append( '.' );
					}
				}

				output.Write( byteIndex.ToString( "X4" ) );
				output.Write( "   " );
				output.Write( bytes.ToString() );
				output.Write( "  " );
				output.WriteLine( chars.ToString() );
			}

			if ( rem != 0 )
			{
				StringBuilder bytes = new StringBuilder( 49 );
				StringBuilder chars = new StringBuilder( rem );

				for ( int j = 0; j < 16; ++j )
				{
					if ( j < rem )
					{
						int c = input.ReadByte();

						bytes.Append( c.ToString( "X2" ) );

						if ( j != 7 )
						{
							bytes.Append( ' ' );
						}
						else
						{
							bytes.Append( "  " );
						}

						if ( c >= 0x20 && c < 0x80 )
						{
							chars.Append( (char)c );
						}
						else
						{
							chars.Append( '.' );
						}
					}
					else
					{
						bytes.Append( "   " );
					}
				}

				output.Write( byteIndex.ToString( "X4" ) );
				output.Write( "   " );
				output.Write( bytes.ToString() );
				output.Write( "  " );
				output.WriteLine( chars.ToString() );
			}
		}

		public static bool NumberBetween( double num, int bound1, int bound2, double allowance )
		{
			if ( bound1 > bound2 )
			{
				int i = bound1;
				bound1 = bound2;
				bound2 = i;
			}

			return ( num<bound2+allowance && num>bound1-allowance );
		}
	}
}