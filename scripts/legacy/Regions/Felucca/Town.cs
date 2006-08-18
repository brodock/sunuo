using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class FeluccaTown : GuardedRegion
	{
		public static new void Initialize()
		{
			Region.AddRegion( new FeluccaTown( "Cove" ) );
			Region.AddRegion( new FeluccaTown( "Britain" ) );
			Region.AddRegion( new FeluccaTown( "Jhelom" ) );
			Region.AddRegion( new FeluccaTown( "Minoc" ) );
			Region.AddRegion( new FeluccaTown( "Ocllo" ) );
			Region.AddRegion( new FeluccaTown( "Trinsic" ) );
			Region.AddRegion( new FeluccaTown( "Vesper" ) );
			Region.AddRegion( new FeluccaTown( "Yew" ) );
			Region.AddRegion( new FeluccaTown( "Wind" ) );
			Region.AddRegion( new FeluccaTown( "Serpent's Hold" ) );
			Region.AddRegion( new FeluccaTown( "Skara Brae" ) );
			Region.AddRegion( new FeluccaTown( "Nujel'm" ) );
			Region.AddRegion( new FeluccaTown( "Moonglow" ) );
			Region.AddRegion( new FeluccaTown( "Magincia" ) );
			Region.AddRegion( new FeluccaTown( "Delucia" ) );
			Region.AddRegion( new FeluccaTown( "Papua" ) );
			Region.AddRegion( GuardedRegion.Disable( new FeluccaTown( "Buccaneer's Den" ) ) );

			Region.AddRegion( new GuardedRegion( "", "Moongates", Map.Felucca, typeof( WarriorGuard ) ) );
		}

		public FeluccaTown( string name ) : this( name, typeof( WarriorGuard ) )
		{
		}

		public FeluccaTown( string name, Type guardType ) : base( "the town of", name, Map.Felucca, guardType )
		{
		}
	}
}