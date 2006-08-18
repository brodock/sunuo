using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class TrammelTown : GuardedRegion
	{
		public static new void Initialize()
		{
			Region.AddRegion( new TrammelTown( "Cove" ) );
			Region.AddRegion( new TrammelTown( "Britain" ) );
			Region.AddRegion( new TrammelTown( "Jhelom" ) );
			Region.AddRegion( new TrammelTown( "Minoc" ) );
			Region.AddRegion( new TrammelTown( "Haven" ) );
			Region.AddRegion( new TrammelTown( "Trinsic" ) );
			Region.AddRegion( new TrammelTown( "Vesper" ) );
			Region.AddRegion( new TrammelTown( "Yew" ) );
			Region.AddRegion( new TrammelTown( "Wind" ) );
			Region.AddRegion( new TrammelTown( "Serpent's Hold" ) );
			Region.AddRegion( new TrammelTown( "Skara Brae" ) );
			Region.AddRegion( new TrammelTown( "Nujel'm" ) );
			Region.AddRegion( new TrammelTown( "Moonglow" ) );
			Region.AddRegion( new TrammelTown( "Magincia" ) );
			Region.AddRegion( new TrammelTown( "Delucia" ) );
			Region.AddRegion( new TrammelTown( "Papua" ) );
			Region.AddRegion( GuardedRegion.Disable( new TrammelTown( "Buccaneer's Den" ) ) );

			Region.AddRegion( new GuardedRegion( "", "Moongates", Map.Trammel, typeof( WarriorGuard ) ) );
		}

		public TrammelTown( string name ) : this( name, typeof( WarriorGuard ) )
		{
		}

		public TrammelTown( string name, Type guardType ) : base( "the town of", name, Map.Trammel, guardType )
		{
		}
	}
}