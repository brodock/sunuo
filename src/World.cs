/***************************************************************************
 *                                 World.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: World.cs,v 1.4 2005/01/22 04:25:04 krrios Exp $
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
using System.Reflection;
using System.Threading;
using Server;
using Server.Mobiles;
using Server.Accounting;
using Server.Network;
using Server.Guilds;

namespace Server
{
	public class World
	{
		public enum SaveOption
		{
			Normal,
			Threaded
		}

		public static SaveOption SaveType = SaveOption.Normal;

		private static Hashtable m_Mobiles;
		private static Hashtable m_Items;

		private static bool m_Loading;
		private static bool m_Loaded;
		private static bool m_Saving;
		private static ArrayList m_DeleteList;

		public static bool Saving{ get { return m_Saving; } }
		public static bool Loaded{ get { return m_Loaded; } }
		public static bool Loading{ get { return m_Loading; } }

		private static string mobIdxPath = Path.Combine( "Saves/Mobiles/", "Mobiles.idx" );
		private static string mobTdbPath = Path.Combine( "Saves/Mobiles/", "Mobiles.tdb" );
		private static string mobBinPath = Path.Combine( "Saves/Mobiles/", "Mobiles.bin" );

		private static string itemIdxPath = Path.Combine( "Saves/Items/", "Items.idx" );
		private static string itemTdbPath = Path.Combine( "Saves/Items/", "Items.tdb" );
		private static string itemBinPath = Path.Combine( "Saves/Items/", "Items.bin" );

		private static string regionIdxPath = Path.Combine( "Saves/Regions/", "Regions.idx" );
		private static string regionBinPath = Path.Combine( "Saves/Regions/", "Regions.bin" );

		private static string guildIdxPath = Path.Combine( "Saves/Guilds/", "Guilds.idx" );
		private static string guildBinPath = Path.Combine( "Saves/Guilds/", "Guilds.bin" );

		//static World()
		//{
		//	Load();
		//}

		public static Hashtable Mobiles
		{
			get
			{
				return m_Mobiles;
			}
		}

		public static Hashtable Items
		{
			get
			{
				return m_Items;
			}
		}

		public static bool OnDelete( object o )
		{
			if ( !m_Loading )
				return true;

			m_DeleteList.Add( o );

			return false;
		}

		public static void Broadcast( int hue, bool ascii, string text )
		{
			Packet p;

			if ( ascii )
				p = new AsciiMessage( Serial.MinusOne, -1, MessageType.Regular, hue, 3, "System", text );
			else
				p = new UnicodeMessage( Serial.MinusOne, -1, MessageType.Regular, hue, 3, "ENU", "System", text );

			ArrayList list = NetState.Instances;

			for ( int i = 0; i < list.Count; ++i )
			{
				if ( ((NetState)list[i]).Mobile != null )
					((NetState)list[i]).Send( p );
			}

			NetState.FlushAll();
		}

		public static void Broadcast( int hue, bool ascii, string format, params object[] args )
		{
			Broadcast( hue, ascii, String.Format( format, args ) );
		}

		private interface IEntityEntry
		{
			Serial Serial{ get; }
			int TypeID{ get; }
			long Position{ get; }
			int Length{ get; }
			object Object{ get; }
		}

		private sealed class RegionEntry : IEntityEntry
		{
			private Region m_Region;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Region;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Region == null ? 0 : m_Region.UId;
				}
			}

			public int TypeID
			{
				get
				{
					return 0;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public RegionEntry( Region r, long pos, int length )
			{
				m_Region = r;
				m_Position = pos;
				m_Length = length;
			}
		}

		private sealed class GuildEntry : IEntityEntry
		{
			private BaseGuild m_Guild;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Guild;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Guild == null ? 0 : m_Guild.Id;
				}
			}

			public int TypeID
			{
				get
				{
					return 0;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public GuildEntry( BaseGuild g, long pos, int length )
			{
				m_Guild = g;
				m_Position = pos;
				m_Length = length;
			}
		}

		private sealed class ItemEntry : IEntityEntry
		{
			private Item m_Item;
			private int m_TypeID;
			private string m_TypeName;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Item;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Item == null ? Serial.MinusOne : m_Item.Serial;
				}
			}

			public int TypeID
			{
				get
				{
					return m_TypeID;
				}
			}

			public string TypeName
			{
				get
				{	
					return m_TypeName;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public ItemEntry( Item item, int typeID, string typeName, long pos, int length )
			{
				m_Item = item;
				m_TypeID = typeID;
				m_TypeName = typeName;
				m_Position = pos;
				m_Length = length;
			}
		}

		private sealed class MobileEntry : IEntityEntry
		{
			private Mobile m_Mobile;
			private int m_TypeID;
			private string m_TypeName;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Mobile;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Mobile == null ? Serial.MinusOne : m_Mobile.Serial;
				}
			}

			public int TypeID
			{
				get
				{
					return m_TypeID;
				}
			}

			public string TypeName
			{
				get
				{
					return m_TypeName;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public MobileEntry( Mobile mobile, int typeID, string typeName, long pos, int length )
			{
				m_Mobile = mobile;
				m_TypeID = typeID;
				m_TypeName = typeName;
				m_Position = pos;
				m_Length = length;
			}
		}

		private static string m_LoadingType;

		public static string LoadingType
		{
			get{ return m_LoadingType; }
		}

		public static void Load()
		{
			if ( m_Loaded )
				return;

			m_Loaded = true;
			m_LoadingType = null;

			Console.Write( "World: Loading..." );

			DateTime start = DateTime.Now;

			m_Loading = true;
			m_DeleteList = new ArrayList();

			int mobileCount = 0, itemCount = 0, guildCount = 0, regionCount = 0;

			object[] ctorArgs = new object[1];
			Type[] ctorTypes = new Type[1]{ typeof( Serial ) };

			ArrayList items = new ArrayList();
			ArrayList mobiles = new ArrayList();
			ArrayList guilds = new ArrayList();
			ArrayList regions = new ArrayList();

			if ( File.Exists( mobIdxPath ) && File.Exists( mobTdbPath ) )
			{
				using ( FileStream idx = new FileStream( mobIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader idxReader = new BinaryReader( idx );

					using ( FileStream tdb = new FileStream( mobTdbPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						BinaryReader tdbReader = new BinaryReader( tdb );

						int count = tdbReader.ReadInt32();

						ArrayList types = new ArrayList( count );

						for ( int i = 0; i < count; ++i )
						{
							string typeName = tdbReader.ReadString();

							Type t = ScriptCompiler.FindTypeByFullName( typeName );

							if ( t == null )
							{
								Console.WriteLine( "failed" );
								Console.WriteLine( "Error: Type '{0}' was not found. Delete all of those types? (y/n)", typeName );

								if ( Console.ReadLine() == "y" )
								{
									types.Add( null );
									Console.Write( "World: Loading..." );
									continue;
								}

								Console.WriteLine( "Types will not be deleted. An exception will be thrown when you press return" );

								throw new Exception( String.Format( "Bad type '{0}'", typeName ) );
							}

							ConstructorInfo ctor = t.GetConstructor( ctorTypes );

							if ( ctor != null )
							{
								types.Add( new object[]{ ctor, null } );
							}
							else
							{
								throw new Exception( String.Format( "Type '{0}' does not have a serialization constructor", t ) );
							}
						}

						mobileCount = idxReader.ReadInt32();

						m_Mobiles = new Hashtable( mobileCount );

						for ( int i = 0; i < mobileCount; ++i )
						{
							int typeID = idxReader.ReadInt32();
							int serial = idxReader.ReadInt32();
							long pos = idxReader.ReadInt64();
							int length = idxReader.ReadInt32();

							object[] objs = (object[])types[typeID];

							if ( objs == null )
								continue;

							Mobile m = null;
							ConstructorInfo ctor = (ConstructorInfo)objs[0];
							string typeName = (string)objs[1];

							try
							{
								ctorArgs[0] = (Serial)serial;
								m = (Mobile)(ctor.Invoke( ctorArgs ));
							}
							catch
							{
							}

							if ( m != null )
							{
								mobiles.Add( new MobileEntry( m, typeID, typeName, pos, length ) );
								AddMobile( m );
							}
						}

						tdbReader.Close();
					}

					idxReader.Close();
				}
			}
			else
			{
				m_Mobiles = new Hashtable();
			}

			if ( File.Exists( itemIdxPath ) && File.Exists( itemTdbPath ) )
			{
				using ( FileStream idx = new FileStream( itemIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader idxReader = new BinaryReader( idx );

					using ( FileStream tdb = new FileStream( itemTdbPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						BinaryReader tdbReader = new BinaryReader( tdb );

						int count = tdbReader.ReadInt32();

						ArrayList types = new ArrayList( count );

						for ( int i = 0; i < count; ++i )
						{
							string typeName = tdbReader.ReadString();

							Type t = ScriptCompiler.FindTypeByFullName( typeName );

							if ( t == null )
							{
								Console.WriteLine( "failed" );
								Console.WriteLine( "Error: Type '{0}' was not found. Delete all of those types? (y/n)", typeName );

								if ( Console.ReadLine() == "y" )
								{
									types.Add( null );
									Console.Write( "World: Loading..." );
									continue;
								}

								Console.WriteLine( "Types will not be deleted. An exception will be thrown when you press return" );

								throw new Exception( String.Format( "Bad type '{0}'", typeName ) );
							}

							ConstructorInfo ctor = t.GetConstructor( ctorTypes );

							if ( ctor != null )
							{
								types.Add( new object[]{ ctor, typeName } );
							}
							else
							{
								throw new Exception( String.Format( "Type '{0}' does not have a serialization constructor", t ) );
							}
						}

						itemCount = idxReader.ReadInt32();

						m_Items = new Hashtable( itemCount );

						for ( int i = 0; i < itemCount; ++i )
						{
							int typeID = idxReader.ReadInt32();
							int serial = idxReader.ReadInt32();
							long pos = idxReader.ReadInt64();
							int length = idxReader.ReadInt32();

							object[] objs = (object[])types[typeID];

							if ( objs == null )
								continue;

							Item item = null;
							ConstructorInfo ctor = (ConstructorInfo)objs[0];
							string typeName = (string)objs[1];

							try
							{
								ctorArgs[0] = (Serial)serial;
								item = (Item)(ctor.Invoke( ctorArgs ));
							}
							catch
							{
							}

							if ( item != null )
							{
								items.Add( new ItemEntry( item, typeID, typeName, pos, length ) );
								AddItem( item );
							}
						}

						tdbReader.Close();
					}

					idxReader.Close();
				}
			}
			else
			{
				m_Items = new Hashtable();
			}

			if ( File.Exists( guildIdxPath ) )
			{
				using ( FileStream idx = new FileStream( guildIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader idxReader = new BinaryReader( idx );

					guildCount = idxReader.ReadInt32();

					CreateGuildEventArgs createEventArgs = new CreateGuildEventArgs( -1 );
					for ( int i = 0; i < guildCount; ++i )
					{
						idxReader.ReadInt32();//no typeid for guilds
						int id = idxReader.ReadInt32();
						long pos = idxReader.ReadInt64();
						int length = idxReader.ReadInt32();

						createEventArgs.Id = id;
						BaseGuild guild = EventSink.InvokeCreateGuild( createEventArgs );//new Guild( id );
						if ( guild != null )
							guilds.Add( new GuildEntry( guild, pos, length ) );
					}

					idxReader.Close();
				}
			}

			if ( File.Exists( regionIdxPath ) )
			{
				using ( FileStream idx = new FileStream( regionIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader idxReader = new BinaryReader( idx );

					regionCount = idxReader.ReadInt32();

					for ( int i = 0; i < regionCount; ++i )
					{
						int typeID = idxReader.ReadInt32();
						int serial = idxReader.ReadInt32();
						long pos = idxReader.ReadInt64();
						int length = idxReader.ReadInt32();

						Region r = Region.FindByUId( serial );

						if ( r != null )
						{
							regions.Add( new RegionEntry( r, pos, length ) );
							Region.AddRegion( r );
							regionCount++;
						}
					}

					idxReader.Close();
				}
			}

			bool failedMobiles = false, failedItems = false, failedGuilds = false, failedRegions = false;
			Type failedType = null;
			Serial failedSerial = Serial.Zero;
			Exception failed = null;
			int failedTypeID = 0;

			if ( File.Exists( mobBinPath ) )
			{
				using ( FileStream bin = new FileStream( mobBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < mobiles.Count; ++i )
					{
						MobileEntry entry = (MobileEntry)mobiles[i];
						Mobile m = (Mobile)entry.Object;

						if ( m != null )
						{
							reader.Seek( entry.Position, SeekOrigin.Begin );

							try
							{
								m_LoadingType = entry.TypeName;
								m.Deserialize( reader );

								if ( reader.Position != (entry.Position + entry.Length) )
									throw new Exception( String.Format( "***** Bad serialize on {0} *****", m.GetType() ) );
							}
							catch ( Exception e )
							{
								mobiles.RemoveAt( i );

								failed = e;
								failedMobiles = true;
								failedType = m.GetType();
								failedTypeID = entry.TypeID;
								failedSerial = m.Serial;

								break;
							}
						}
					}

					reader.Close();
				}
			}

			if ( !failedMobiles && File.Exists( itemBinPath ) )
			{
				using ( FileStream bin = new FileStream( itemBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < items.Count; ++i )
					{
						ItemEntry entry = (ItemEntry)items[i];
						Item item = (Item)entry.Object;

						if ( item != null )
						{
							reader.Seek( entry.Position, SeekOrigin.Begin );

							try
							{
								m_LoadingType = entry.TypeName;
								item.Deserialize( reader );

								if ( reader.Position != (entry.Position + entry.Length) )
									throw new Exception( String.Format( "***** Bad serialize on {0} *****", item.GetType() ) );
							}
							catch ( Exception e )
							{
								items.RemoveAt( i );

								failed = e;
								failedItems = true;
								failedType = item.GetType();
								failedTypeID = entry.TypeID;
								failedSerial = item.Serial;

								break;
							}
						}
					}

					reader.Close();
				}
			}

			m_LoadingType = null;

			if ( !failedMobiles && !failedItems && File.Exists( guildBinPath ) )
			{
				using ( FileStream bin = new FileStream( guildBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < guilds.Count; ++i )
					{
						GuildEntry entry = (GuildEntry)guilds[i];
						BaseGuild g = (BaseGuild)entry.Object;

						if ( g != null )
						{
							reader.Seek( entry.Position, SeekOrigin.Begin );

							try
							{
								g.Deserialize( reader );

								if ( reader.Position != (entry.Position + entry.Length) )
									throw new Exception( String.Format( "***** Bad serialize on Guild {0} *****", g.Id ) );
							}
							catch ( Exception e )
							{
								guilds.RemoveAt( i );

								failed = e;
								failedGuilds = true;
								failedType = typeof( BaseGuild );
								failedTypeID = g.Id;
								failedSerial = g.Id;

								break;
							}
						}
					}

					reader.Close();
				}
			}

			if ( !failedMobiles && !failedItems && File.Exists( regionBinPath ) )
			{
				using ( FileStream bin = new FileStream( regionBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i <regions.Count; ++i )
					{
						RegionEntry entry = (RegionEntry)regions[i];
						Region r = (Region)entry.Object;

						if ( r != null )
						{
							reader.Seek( entry.Position, SeekOrigin.Begin );

							try
							{
								r.Deserialize( reader );

								if ( reader.Position != (entry.Position + entry.Length) )
									throw new Exception( String.Format( "***** Bad serialize on {0} *****", r.GetType() ) );
							}
							catch ( Exception e )
							{
								regions.RemoveAt( i );

								failed = e;
								failedRegions = true;
								failedType = r.GetType();
								failedTypeID = entry.TypeID;
								failedSerial = r.UId;

								break;
							}
						}
					}

					reader.Close();
				}
			}

			if ( failedItems || failedMobiles || failedGuilds || failedRegions )
			{
				Console.WriteLine( "An error was encountered while loading a saved object" );

				Console.WriteLine( " - Type: {0}", failedType );
				Console.WriteLine( " - Serial: {0}", failedSerial );

				Console.WriteLine( "Delete the object? (y/n)" );

				if ( Console.ReadLine() == "y" )
				{
					if ( failedType != typeof( BaseGuild ) && !failedType.IsSubclassOf( typeof( Region ) ) )
					{
						Console.WriteLine( "Delete all objects of that type? (y/n)" );

						if ( Console.ReadLine() == "y" )
						{
							if ( failedMobiles )
							{
								for ( int i = 0; i < mobiles.Count; )
								{
									if ( ((MobileEntry)mobiles[i]).TypeID == failedTypeID )
										mobiles.RemoveAt( i );
									else
										++i;
								}
							}
							else if ( failedItems )
							{
								for ( int i = 0; i < items.Count; )
								{
									if ( ((ItemEntry)items[i]).TypeID == failedTypeID )
										items.RemoveAt( i );
									else
										++i;
								}
							}
						}
					}

					SaveIndex( mobiles, mobIdxPath );
					SaveIndex( items, itemIdxPath );
					SaveIndex( guilds, guildIdxPath );
					SaveIndex( regions, regionIdxPath );
				}

				Console.WriteLine( "After pressing return an exception will be thrown and the server will terminate" );
				Console.ReadLine();

				throw new Exception( String.Format( "Load failed (items={0}, mobiles={1}, guilds={2}, regions={3}, type={4}, serial={5})", failedItems, failedMobiles, failedGuilds, failedRegions, failedType, failedSerial ), failed );
			}

			EventSink.InvokeWorldLoad();

			m_Loading = false;

			for ( int i = 0; i < m_DeleteList.Count; ++i )
			{
				object o = m_DeleteList[i];

				if ( o is Item )
					((Item)o).Delete();
				else if ( o is Mobile )
					((Mobile)o).Delete();
			}

			m_DeleteList.Clear();

			foreach ( Item item in m_Items.Values )
			{
				if ( item.Parent == null )
					item.UpdateTotals();

				item.ClearProperties();
			}

			ArrayList list = new ArrayList( m_Mobiles.Values );

			foreach ( Mobile m in list )
			{
				m.ForceRegionReEnter( true );
				m.UpdateTotals();

				m.ClearProperties();
			}

			Console.WriteLine( "done ({1} items, {2} mobiles) ({0:F1} seconds)", (DateTime.Now-start).TotalSeconds, m_Items.Count, m_Mobiles.Count );
		}

		public static void SaveIndex( ArrayList list, string path )
		{
			if ( !Directory.Exists( "Saves/Mobiles/" ) )
				Directory.CreateDirectory( "Saves/Mobiles/" );

			if ( !Directory.Exists( "Saves/Items/" ) )
				Directory.CreateDirectory( "Saves/Items/" );

			if ( !Directory.Exists( "Saves/Guilds/" ) )
				Directory.CreateDirectory( "Saves/Guilds/" );

			if ( !Directory.Exists( "Saves/Regions/" ) )
				Directory.CreateDirectory( "Saves/Regions/" );

			using ( FileStream idx = new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.None ) )
			{
				BinaryWriter idxWriter = new BinaryWriter( idx );

				idxWriter.Write( list.Count );

				for ( int i = 0; i < list.Count; ++i )
				{
					IEntityEntry e = (IEntityEntry)list[i];

					idxWriter.Write( e.TypeID );
					idxWriter.Write( e.Serial );
					idxWriter.Write( e.Position );
					idxWriter.Write( e.Length );
				}
				
				idxWriter.Close();
			}
		}

		internal static int m_Saves;

		public static void Save()
		{
			++m_Saves;
			Save( true );
		}

		private static bool m_MultiProcessor = false;

		public static bool MultiProcessor{ get{ return m_MultiProcessor; } set{ m_MultiProcessor = value; } }

		public static void Save( bool message )
		{
			if ( m_Saving || AsyncWriter.ThreadCount > 0 ) 
				return;

			NetState.FlushAll();
			
			m_Saving = true;

			if ( message )
				Broadcast( 0x35, true, "The world is saving, please wait." );

			Console.Write( "World: Saving..." );

			DateTime startTime = DateTime.Now;

			if ( !Directory.Exists( "Saves/Mobiles/" ) )
				Directory.CreateDirectory( "Saves/Mobiles/" );
			if ( !Directory.Exists( "Saves/Items/" ) )
				Directory.CreateDirectory( "Saves/Items/" );
			if ( !Directory.Exists( "Saves/Guilds/" ) )
				Directory.CreateDirectory( "Saves/Guilds/" );
			if ( !Directory.Exists( "Saves/Regions/" ) )
				Directory.CreateDirectory( "Saves/Regions/" );

			if ( m_MultiProcessor )
			{
				Thread saveThread = new Thread( new ThreadStart( SaveItems ) );

				saveThread.Name = "Item Save Subset";
				saveThread.Start();

				SaveMobiles();
				SaveGuilds();
				SaveRegions();

				saveThread.Join();
			}
			else
			{
				SaveMobiles();
				SaveItems();
				SaveGuilds();
				SaveRegions();
			}

			//Accounts.Save();

			try
			{
				EventSink.InvokeWorldSave( new WorldSaveEventArgs( message ) );
			}
			catch ( Exception e )
			{
				throw new Exception( "World Save event threw an exception.  Save failed!", e );
			}

			//System.GC.Collect();

			DateTime endTime = DateTime.Now;
			Console.WriteLine( "done in {0:F1} seconds.", (endTime - startTime).TotalSeconds );

			if ( message )
				Broadcast( 0x35, true, "World save complete. The entire process took {0:F1} seconds.", (endTime - startTime).TotalSeconds );

			m_Saving = false;
		}

		private static void SaveMobiles()
		{
			ArrayList restock = new ArrayList();

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if ( SaveType == SaveOption.Normal )
			{
				idx = new BinaryFileWriter( mobIdxPath, false );
				tdb = new BinaryFileWriter( mobTdbPath, false );
				bin = new BinaryFileWriter( mobBinPath, true );
			} 
			else
			{
				idx = new AsyncWriter( mobIdxPath, false );
				tdb = new AsyncWriter( mobTdbPath, false );
				bin = new AsyncWriter( mobBinPath, true );
			}

			idx.Write( (int) m_Mobiles.Count );
			foreach ( Mobile m in m_Mobiles.Values )
			{
				Type t = m.GetType();

				long start = bin.Position;

				idx.Write( (int) m.m_TypeRef );
				idx.Write( (int) m.Serial );
				idx.Write( (long) start );

				m.Serialize( bin );

				idx.Write( (int) (bin.Position - start) );

				if ( m is IVendor )
				{
					if ( ((IVendor)m).LastRestock + ((IVendor)m).RestockDelay < DateTime.Now )
						restock.Add( m );
				}

				m.FreeCache();
			}

			tdb.Write( (int) m_MobileTypes.Count );
			for ( int i = 0; i < m_MobileTypes.Count; ++i )
				tdb.Write( ((Type)m_MobileTypes[i]).FullName );

			for (int i=0;i<restock.Count;i++)
			{
				IVendor vend = (IVendor)restock[i];
				vend.Restock();
				vend.LastRestock = DateTime.Now;
			}

			idx.Close();
			tdb.Close();
			bin.Close();
		}

		internal static ArrayList m_ItemTypes = new ArrayList();
		internal static ArrayList m_MobileTypes = new ArrayList();

		private static void SaveItems()
		{
			ArrayList decaying = new ArrayList();

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if ( SaveType == SaveOption.Normal )
			{
				idx = new BinaryFileWriter( itemIdxPath, false );
				tdb = new BinaryFileWriter( itemTdbPath, false );
				bin = new BinaryFileWriter( itemBinPath, true );
			} 
			else
			{
				idx = new AsyncWriter( itemIdxPath, false );
				tdb = new AsyncWriter( itemTdbPath, false );
				bin = new AsyncWriter( itemBinPath, true );
			}

			idx.Write( (int) m_Items.Count );
			foreach ( Item item in m_Items.Values )
			{
				if ( item.Decays && item.Parent == null && item.Map != Map.Internal && (item.LastMoved + item.DecayTime) <= DateTime.Now )
					decaying.Add( item );

				long start = bin.Position;

				idx.Write( (int) item.m_TypeRef );
				idx.Write( (int) item.Serial );
				idx.Write( (long) start );

				item.Serialize( bin );

				idx.Write( (int) (bin.Position - start) );

				item.FreeCache();
			}

			tdb.Write( (int) m_ItemTypes.Count );
			for ( int i = 0; i < m_ItemTypes.Count; ++i )
				tdb.Write( ((Type)m_ItemTypes[i]).FullName );

			idx.Close();
			tdb.Close();
			bin.Close();

			for ( int i = 0; i < decaying.Count; ++i )
			{
				Item item = (Item)decaying[i];

				if ( item.OnDecay() )
					item.Delete();
			}
		}

		private static void SaveGuilds()
		{
			GenericWriter idx;
			GenericWriter bin;

			if ( SaveType == SaveOption.Normal )
			{
				idx = new BinaryFileWriter( guildIdxPath, false );
				bin = new BinaryFileWriter( guildBinPath, true );
			} 
			else
			{
				idx = new AsyncWriter( guildIdxPath, false );
				bin = new AsyncWriter( guildBinPath, true );
			}

			idx.Write( (int) BaseGuild.List.Count );
			foreach ( BaseGuild guild in BaseGuild.List.Values )
			{
				long start = bin.Position;

				idx.Write( (int)0 );//guilds have no typeid
				idx.Write( (int)guild.Id );
				idx.Write( (long)start );

				guild.Serialize( bin );

				idx.Write( (int) (bin.Position - start) );
			}

			idx.Close();
			bin.Close();
		}

		private static void SaveRegions()
		{
			int count = 0;

			GenericWriter bin;

			if ( SaveType == SaveOption.Normal )
				bin = new BinaryFileWriter( regionBinPath, true );
			else
				bin = new AsyncWriter( regionBinPath, true );

			MemoryStream mem = new MemoryStream( 4 + (20*Region.Regions.Count) );
			BinaryWriter memIdx = new BinaryWriter( mem );

			memIdx.Write( (int)0 );

			for ( int i = 0; i < Region.Regions.Count; ++i )
			{
				Region region = (Region)Region.Regions[i];

				if ( region.Saves )
				{
					++count;
					long start = bin.Position;

					memIdx.Write( (int)0 );//typeid
					memIdx.Write( (int) region.UId );
					memIdx.Write( (long) start );

					region.Serialize( bin );

					memIdx.Write( (int) (bin.Position - start) );
				}
			}

			bin.Close();
			
			memIdx.Seek( 0, SeekOrigin.Begin );
			memIdx.Write( (int)count );

			if ( SaveType == SaveOption.Threaded )
			{
				AsyncWriter asyncIdx = new AsyncWriter( regionIdxPath, false );
				asyncIdx.MemStream = mem;
				asyncIdx.Close();
			}
			else
			{
				FileStream fs = new FileStream( regionIdxPath, FileMode.Create, FileAccess.Write, FileShare.None );
				mem.WriteTo( fs );
				fs.Close();
				mem.Close();
			}
			
			// mem is closed only in non threaded saves, as its reference is copied to asyncIdx.MemStream
			memIdx.Close();
		}

		public static IEntity FindEntity( Serial serial )
		{
			if ( serial.IsItem )
			{
				return (Item)m_Items[serial];
			}
			else if ( serial.IsMobile )
			{
				return (Mobile)m_Mobiles[serial];
			}
			else
			{
				return null;
			}
		}

		public static Mobile FindMobile( Serial serial )
		{
			return (Mobile)m_Mobiles[serial];
		}

		public static void AddMobile( Mobile m )
		{
			m_Mobiles[m.Serial] = m;
		}

		public static Item FindItem( Serial serial )
		{
			return (Item)m_Items[serial];
		}

		public static void AddItem( Item item )
		{
			m_Items[item.Serial] = item;
		}

		public static void RemoveMobile( Mobile m )
		{
			m_Mobiles.Remove( m.Serial );
		}

		public static void RemoveItem( Item item )
		{
			m_Items.Remove( item.Serial );
		}
	}
}
