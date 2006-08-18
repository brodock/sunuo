using System;
using Server;

namespace Server.Regions
{
	public class TrammelDungeon : DungeonRegion
	{
		public static void Initialize()
		{
			Region.AddRegion( new TrammelDungeon( "Covetous" ) );
			Region.AddRegion( new TrammelDungeon( "Deceit" ) );
			Region.AddRegion( new TrammelDungeon( "Despise" ) );
			Region.AddRegion( new TrammelDungeon( "Destard" ) );
			Region.AddRegion( new TrammelDungeon( "Hythloth" ) );
			Region.AddRegion( new TrammelDungeon( "Shame" ) );
			Region.AddRegion( new TrammelDungeon( "Wrong" ) );
			Region.AddRegion( new TrammelDungeon( "Terathan Keep" ) );
			Region.AddRegion( new TrammelDungeon( "Fire" ) );
			Region.AddRegion( new TrammelDungeon( "Ice" ) );
			Region.AddRegion( new TrammelDungeon( "Orc Cave" ) );
			Region.AddRegion( new TrammelDungeon( "Misc Dungeons" ) );
		}

		public TrammelDungeon( string name ) : base( name, Map.Trammel )
		{
		}
	}
}