/***************************************************************************
 *                                 World.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2005 Max Kellermann <max@duempel.org>
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
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

		private struct EntityType {
			private string name;
			private ConstructorInfo ctor;

			public EntityType(string name, ConstructorInfo ctor) {
				this.name = name;
				this.ctor = ctor;
			}

			public string Name {
				get { return name; }
			}

			public ConstructorInfo Constructor {
				get { return ctor; }
			}
		}

		private interface IEntityEntry
		{
			Serial Serial{ get; }
			int TypeID{ get; }
			long Position{ get; }
			int Length{ get; }
			object Object{ get; }
		}

		private struct RegionEntry : IEntityEntry
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

			public void Clear() {
				m_Region = null;
			}
		}

		private struct GuildEntry : IEntityEntry
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

			public void Clear() {
				m_Guild = null;
			}
		}

		private struct ItemEntry : IEntityEntry
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

			public void Clear() {
				m_Item = null;
				m_TypeName = null;
			}
		}

		private struct MobileEntry : IEntityEntry
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

			public void Clear() {
				m_Mobile = null;
				m_TypeName = null;
			}
		}

		private static EntityType[] LoadTypes(BinaryReader tdbReader) {
			int count = tdbReader.ReadInt32();

			Type[] ctorTypes = new Type[1]{ typeof( Serial ) };

			EntityType[] types = new EntityType[count];

			for (int i = 0; i < count; ++i) {
				string typeName = tdbReader.ReadString();
				if (typeName == null || typeName == "")
					continue;

				typeName = string.Intern(typeName);

				Type t = ScriptCompiler.FindTypeByFullName(typeName);

				if (t == null) {
					Console.WriteLine( "failed" );
					Console.WriteLine( "Error: Type '{0}' was not found. Delete all of those types? (y/n)", typeName );

					if ( Console.ReadLine() == "y" ) {
						Console.Write( "World: Loading..." );
						continue;
					}

					Console.WriteLine( "Types will not be deleted. An exception will be thrown when you press return" );
					throw new Exception( String.Format( "Bad type '{0}'", typeName ) );
				}

				ConstructorInfo ctor = t.GetConstructor( ctorTypes );

				if ( ctor != null ) {
					types[i] = new EntityType(typeName, ctor);
				} else {
					throw new Exception( String.Format( "Type '{0}' does not have a serialization constructor", t ) );
				}
			}

			return types;
		}

		private static EntityType[] LoadTypes(string path) {
			using (FileStream tdb = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader tdbReader = new BinaryReader(tdb)) {
					return LoadTypes(tdbReader);
				}
			}
		}

		private static MobileEntry[] LoadMobileIndex(BinaryReader idxReader,
													 EntityType[] types) {
			int count = idxReader.ReadInt32();

			object[] ctorArgs = new object[1];

			m_Mobiles = new Hashtable((count * 11) / 10);
			MobileEntry[] entries = new MobileEntry[count];

			for (int i = 0; i < count; ++i) {
				int typeID = idxReader.ReadInt32();
				int serial = idxReader.ReadInt32();
				long pos = idxReader.ReadInt64();
				int length = idxReader.ReadInt32();

				if (serial == Serial.MinusOne)
					continue;

				EntityType type = types[typeID];
				if (type.Constructor == null)
					continue;

				Mobile m = null;

				try {
					ctorArgs[0] = (Serial)serial;
					m = (Mobile)type.Constructor.Invoke(ctorArgs);
				} catch {
				}

				if (m != null) {
					entries[i] = new MobileEntry(m, typeID, type.Name, pos, length);
					AddMobile(m);
				}
			}

			return entries;
		}

		private static MobileEntry[] LoadMobileIndex(string path,
													 EntityType[] ctors) {
			using (FileStream idx = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader idxReader = new BinaryReader(idx)) {
					return LoadMobileIndex(idxReader, ctors);
				}
			}
		}

		private static ItemEntry[] LoadItemIndex(BinaryReader idxReader,
												 EntityType[] types) {
			int count = idxReader.ReadInt32();

			object[] ctorArgs = new object[1];

			m_Items = new Hashtable((count * 11) / 10);
			ItemEntry[] entries = new ItemEntry[count];

			for (int i = 0; i < count; ++i) {
				int typeID = idxReader.ReadInt32();
				int serial = idxReader.ReadInt32();
				long pos = idxReader.ReadInt64();
				int length = idxReader.ReadInt32();

				if (serial == Serial.MinusOne)
					continue;

				EntityType type = types[typeID];
				if (type.Constructor == null)
					continue;

				Item item = null;

				try {
					ctorArgs[0] = (Serial)serial;
					item = (Item)type.Constructor.Invoke(ctorArgs);
				} catch {
				}

				if (item != null) {
					entries[i] = new ItemEntry(item, typeID, type.Name, pos, length);
					AddItem(item);
				}
			}

			return entries;
		}

		private static ItemEntry[] LoadItemIndex(string path,
												 EntityType[] ctors) {
			using (FileStream idx = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader idxReader = new BinaryReader(idx)) {
					return LoadItemIndex(idxReader, ctors);
				}
			}
		}

		private static string m_LoadingType;

		public static string LoadingType
		{
			get{ return m_LoadingType; }
		}

		private static void LoadEntities(string saveDirectory) {
			ItemEntry[] itemEntries = null;
			MobileEntry[] mobileEntries = null;
			GuildEntry[] guildEntries = null;
			RegionEntry[] regionEntries = null;

			string mobileBase = Path.Combine(saveDirectory, "Mobiles");
			string mobIdxPath = Path.Combine(mobileBase, "Mobiles.idx");
			string mobTdbPath = Path.Combine(mobileBase, "Mobiles.tdb");
			string mobBinPath = Path.Combine(mobileBase, "Mobiles.bin");

			if ( File.Exists( mobIdxPath ) && File.Exists( mobTdbPath ) )
			{
				log.Debug("loading mobile index");
				EntityType[] types = LoadTypes(mobTdbPath);
				mobileEntries = LoadMobileIndex(mobIdxPath, types);
			}
			else
			{
				m_Mobiles = new Hashtable();
			}

			string itemBase = Path.Combine(saveDirectory, "Items");
			string itemIdxPath = Path.Combine(itemBase, "Items.idx");
			string itemTdbPath = Path.Combine(itemBase, "Items.tdb");
			string itemBinPath = Path.Combine(itemBase, "Items.bin");

			if ( File.Exists( itemIdxPath ) && File.Exists( itemTdbPath ) )
			{
				log.Debug("loading item index");
				EntityType[] types = LoadTypes(itemTdbPath);
				itemEntries = LoadItemIndex(itemIdxPath, types);
			}
			else
			{
				m_Items = new Hashtable();
			}

			string guildBase = Path.Combine(saveDirectory, "Guilds");
			string guildIdxPath = Path.Combine(guildBase, "Guilds.idx");
			string guildBinPath = Path.Combine(guildBase, "Guilds.bin");

			if ( File.Exists( guildIdxPath ) )
			{
				log.Debug("loading guild index");

				using ( FileStream idx = new FileStream( guildIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader idxReader = new BinaryReader( idx );

					int count = idxReader.ReadInt32();
					guildEntries = new GuildEntry[count];

					CreateGuildEventArgs createEventArgs = new CreateGuildEventArgs( -1 );
					for ( int i = 0; i < count; ++i )
					{
						idxReader.ReadInt32();//no typeid for guilds
						int id = idxReader.ReadInt32();
						long pos = idxReader.ReadInt64();
						int length = idxReader.ReadInt32();

						createEventArgs.Id = id;
						BaseGuild guild = EventSink.InvokeCreateGuild( createEventArgs );//new Guild( id );
						if ( guild != null )
							guildEntries[i] = new GuildEntry( guild, pos, length );
					}

					idxReader.Close();
				}
			}

			string regionBase = Path.Combine(saveDirectory, "Regions");
			string regionIdxPath = Path.Combine(regionBase, "Regions.idx");
			string regionBinPath = Path.Combine(regionBase, "Regions.bin");

			if ( File.Exists( regionIdxPath ) )
			{
				log.Debug("loading region index");

				using ( FileStream idx = new FileStream( regionIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader idxReader = new BinaryReader( idx );

					int count = idxReader.ReadInt32();
					regionEntries = new RegionEntry[count];

					for ( int i = 0; i < count; ++i )
					{
						idxReader.ReadInt32(); /* typeID */
						int serial = idxReader.ReadInt32();
						long pos = idxReader.ReadInt64();
						int length = idxReader.ReadInt32();

						if (serial == Serial.MinusOne)
							continue;

						Region r = Region.FindByUId( serial );

						if ( r != null )
						{
							regionEntries[i] = new RegionEntry( r, pos, length );
							Region.AddRegion( r );
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
				log.Debug("loading mobiles");

				using ( FileStream bin = new FileStream( mobBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < mobileEntries.Length; ++i )
					{
						MobileEntry entry = mobileEntries[i];
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
								log.Error("failed to load mobile", e);
								mobileEntries[i].Clear();

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

				if (!failedMobiles)
					mobileEntries = null;
			}

			if ( !failedMobiles && File.Exists( itemBinPath ) )
			{
				log.Debug("loading items");

				using ( FileStream bin = new FileStream( itemBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < itemEntries.Length; ++i )
					{
						ItemEntry entry = itemEntries[i];
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
								log.Fatal("failed to load item", e);
								itemEntries[i].Clear();

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

				if (!failedItems)
					itemEntries = null;
			}

			if ( !failedMobiles && !failedItems && File.Exists( guildBinPath ) )
			{
				log.Debug("loading guilds");

				using ( FileStream bin = new FileStream( guildBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < guildEntries.Length; ++i )
					{
						GuildEntry entry = guildEntries[i];
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
								log.Fatal("failed to load guild", e);
								guildEntries[i].Clear();

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

				if (!failedGuilds)
					guildEntries = null;
			}

			if ( !failedMobiles && !failedItems && File.Exists( regionBinPath ) )
			{
				log.Debug("loading regions");

				using ( FileStream bin = new FileStream( regionBinPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryFileReader reader = new BinaryFileReader( new BinaryReader( bin ) );

					for ( int i = 0; i < regionEntries.Length; ++i )
					{
						RegionEntry entry = regionEntries[i];
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
								log.Fatal("failed to load region", e);
								regionEntries[i].Clear();

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

				if (!failedRegions)
					regionEntries = null;
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
								for ( int i = 0; i < mobileEntries.Length; ++i)
								{
									if (mobileEntries[i].TypeID == failedTypeID)
										mobileEntries[i].Clear();
								}
							}
							else if ( failedItems )
							{
								for ( int i = 0; i < itemEntries.Length; ++i)
								{
									if (itemEntries[i].TypeID == failedTypeID)
										itemEntries[i].Clear();
								}
							}
						}
					}

					if (mobileEntries != null) {
						if (!Directory.Exists(mobileBase))
							Directory.CreateDirectory(mobileBase);
						SaveIndex( mobileEntries, mobIdxPath );
					}

					if (itemEntries != null) {
						if (!Directory.Exists(itemBase))
							Directory.CreateDirectory(itemBase);
						SaveIndex( itemEntries, itemIdxPath );
					}

					if (guildEntries != null) {
						if (!Directory.Exists(guildBase))
							Directory.CreateDirectory(guildBase);

						SaveIndex( guildEntries, guildIdxPath );
					}

					if (regionEntries != null)
						SaveIndex( regionEntries, regionIdxPath );
				}

				Console.WriteLine( "After pressing return an exception will be thrown and the server will terminate" );
				Console.ReadLine();

				throw new Exception( String.Format( "Load failed (items={0}, mobiles={1}, guilds={2}, regions={3}, type={4}, serial={5})", failedItems, failedMobiles, failedGuilds, failedRegions, failedType, failedSerial ), failed );
			}
		}

		public static void Load()
		{
			if ( m_Loaded )
				return;

			m_Loaded = true;

			log.Info("Loading world");

			DateTime start = DateTime.Now;

			m_Loading = true;
			m_DeleteList = new ArrayList();

			LoadEntities(Core.Config.SaveDirectory);

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

			log.Info(String.Format("World loaded: {1} items, {2} mobiles ({0:F1} seconds)", (DateTime.Now-start).TotalSeconds, m_Items.Count, m_Mobiles.Count));
		}

		private static void SaveIndex( ICollection list, string path )
		{
			using ( FileStream idx = new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.None ) )
			{
				BinaryWriter idxWriter = new BinaryWriter( idx );

				idxWriter.Write( list.Count );

				foreach (IEntityEntry e in list) {
					idxWriter.Write( e.TypeID );
					idxWriter.Write( e.Serial );
					idxWriter.Write( e.Position );
					idxWriter.Write( e.Length );
				}
				
				idxWriter.Close();
			}
		}

		public static void Save()
		{
			Save( true );
		}

		private class SaveItemsStart
		{
			private string itemBase;

			public SaveItemsStart(string _itemBase)
			{
				itemBase = _itemBase;
			}

			public void SaveItems() {
				World.SaveItems(itemBase);
			}
		}

		public static void Save(string saveDirectory, bool message)
		{
			if ( m_Saving || AsyncWriter.ThreadCount > 0 ) 
				return;

			NetState.FlushAll();
			
			m_Saving = true;

			if ( message )
				Broadcast( 0x35, true, "The world is saving, please wait." );

			log.Info( "Saving world" );

			DateTime startTime = DateTime.Now;

			string mobileBase = Path.Combine(saveDirectory, "Mobiles");
			string itemBase = Path.Combine(saveDirectory, "Items");
			string guildBase = Path.Combine(saveDirectory, "Guilds");
			string regionBase = Path.Combine(saveDirectory, "Regions");

			if (Core.Config.Features["multi-threading"])
			{
				Thread saveThread = new Thread(new ThreadStart(new SaveItemsStart(itemBase).SaveItems));

				saveThread.Name = "Item Save Subset";
				saveThread.Start();

				SaveMobiles(mobileBase);
				SaveGuilds(guildBase);
				SaveRegions(regionBase);

				saveThread.Join();
			}
			else
			{
				SaveMobiles(mobileBase);
				SaveItems(itemBase);
				SaveGuilds(guildBase);
				SaveRegions(regionBase);
			}

			log.InfoFormat("Entities saved in {0:F1} seconds.",
						   (DateTime.Now - startTime).TotalSeconds);

			//Accounts.Save();

			try
			{
				EventSink.InvokeWorldSave(new WorldSaveEventArgs(saveDirectory, message));
			}
			catch ( Exception e )
			{
				throw new Exception( "World Save event threw an exception.  Save failed!", e );
			}

			//System.GC.Collect();

			DateTime endTime = DateTime.Now;
			log.Info(String.Format("World saved in {0:F1} seconds.",
								   (endTime - startTime).TotalSeconds));

			if ( message )
				Broadcast( 0x35, true, "World save complete. The entire process took {0:F1} seconds.", (endTime - startTime).TotalSeconds );

			m_Saving = false;
		}

		private static void MoveDirectoryContents(string src, string dst) {
			foreach (string name in Directory.GetFileSystemEntries(src)) {
				string baseName = Path.GetFileName(name);
				if (baseName != "tmp")
					Directory.Move(name, Path.Combine(dst, baseName));
			}
		}

		public static void Save( bool message )
		{
			/* create "./Saves/tmp/old" */

			string saveDirectory = Core.Config.SaveDirectory;
			if (!Directory.Exists(saveDirectory))
				Directory.CreateDirectory(saveDirectory);

			string tmpDirectory = Path.Combine(saveDirectory, "tmp");
			if (Directory.Exists(tmpDirectory))
				Directory.Delete(tmpDirectory, true);
			else if (File.Exists(tmpDirectory))
				File.Delete(tmpDirectory);
			Directory.CreateDirectory(tmpDirectory);

			string oldDirectory = Path.Combine(tmpDirectory, "old");
			Directory.CreateDirectory(oldDirectory);

			/* move current save to "./Saves/tmp/old/" */

			MoveDirectoryContents(saveDirectory, oldDirectory);

			try {
				/* save to "./Saves/" */

				Save(saveDirectory, message);

			} catch {
				/* rollback */

				string newDirectory = Path.Combine(tmpDirectory, "new");
				Directory.CreateDirectory(newDirectory);

				MoveDirectoryContents(saveDirectory, newDirectory);
				MoveDirectoryContents(oldDirectory, saveDirectory);

				throw;
			} finally {
				Directory.Delete(tmpDirectory, true);
			}
		}

		private static void SaveMobiles(string mobileBase)
		{
			ArrayList restock = new ArrayList();

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if (!Directory.Exists(mobileBase))
				Directory.CreateDirectory(mobileBase);

			string mobIdxPath = Path.Combine(mobileBase, "Mobiles.idx");
			string mobTdbPath = Path.Combine(mobileBase, "Mobiles.tdb");
			string mobBinPath = Path.Combine(mobileBase, "Mobiles.bin");

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

		private static void SaveItems(string itemBase)
		{
			string itemIdxPath = Path.Combine( itemBase, "Items.idx" );
			string itemTdbPath = Path.Combine( itemBase, "Items.tdb" );
			string itemBinPath = Path.Combine( itemBase, "Items.bin" );

			ArrayList decaying = new ArrayList();

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if (!Directory.Exists(itemBase))
				Directory.CreateDirectory(itemBase);

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

		private static void SaveGuilds(string guildBase)
		{
			string guildIdxPath = Path.Combine(guildBase, "Guilds.idx");
			string guildBinPath = Path.Combine(guildBase, "Guilds.bin");

			GenericWriter idx;
			GenericWriter bin;

			if (!Directory.Exists(guildBase))
				Directory.CreateDirectory(guildBase);

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

		private static void SaveRegions(string regionBase)
		{
			string regionIdxPath = Path.Combine( regionBase, "Regions.idx" );
			string regionBinPath = Path.Combine( regionBase, "Regions.bin" );

			int count = 0;

			GenericWriter bin;

			if (!Directory.Exists(regionBase))
				Directory.CreateDirectory(regionBase);

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
