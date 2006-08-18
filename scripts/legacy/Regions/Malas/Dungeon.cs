using System;
using Server;

namespace Server.Regions
{
	public class MalasDungeon : DungeonRegion
	{
		public static void Initialize()
		{
			Region.AddRegion( new MalasDungeon( "Doom" ) ); 
			Region.AddRegion( new MalasDungeon( "Doom Gauntlet" ) ); 
		}

		public MalasDungeon( string name ) : base( name, Map.Malas )
		{
		}
	}
}