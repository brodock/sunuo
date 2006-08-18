using System;
using Server;

namespace Server.Regions
{
	public class IlshenarDungeon : DungeonRegion
	{
		public static void Initialize()
		{
			Region.AddRegion( new IlshenarDungeon( "Rock Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Spider Cave" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Spectre Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Blood Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Wisp Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Ankh Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Exodus Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Sorcerer's Dungeon" ) ); 
			Region.AddRegion( new IlshenarDungeon( "Ancient Lair" ) ); 
		}

		public IlshenarDungeon( string name ) : base( name, Map.Ilshenar )
		{
		}
	}
}