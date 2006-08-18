using System;
using Server;

namespace Server.Regions
{
	public class FeluccaDungeon : DungeonRegion
	{
		public static void Initialize()
		{
			Region.AddRegion( new FeluccaDungeon( "Covetous" ) );
			Region.AddRegion( new FeluccaDungeon( "Deceit" ) );
			Region.AddRegion( new FeluccaDungeon( "Despise" ) );
			Region.AddRegion( new FeluccaDungeon( "Destard" ) );
			Region.AddRegion( new FeluccaDungeon( "Hythloth" ) );
			Region.AddRegion( new FeluccaDungeon( "Shame" ) );
			Region.AddRegion( new FeluccaDungeon( "Wrong" ) );
			Region.AddRegion( new FeluccaDungeon( "Terathan Keep" ) );
			Region.AddRegion( new FeluccaDungeon( "Fire" ) );
			Region.AddRegion( new FeluccaDungeon( "Ice" ) );
			Region.AddRegion( new FeluccaDungeon( "Orc Cave" ) );
			Region.AddRegion( new FeluccaDungeon( "Khaldun" ) );
			Region.AddRegion( new FeluccaDungeon( "Misc Dungeons" ) );
		}

		public FeluccaDungeon( string name ) : base( name, Map.Felucca )
		{
		}

		public override bool CanUseStuckMenu( Mobile m )
		{
			return false;
		}
	}
}