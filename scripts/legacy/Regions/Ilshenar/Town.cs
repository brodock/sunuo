using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class IlshenarCity : GuardedRegion
	{
		public static new void Initialize()
		{
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Mistas" ) ) ); 
			Region.AddRegion( new IlshenarCity( "Gargoyle City" ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Montor" ) ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Alexandretta's Bowl" ) ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Lenmir Anfinmotas" ) ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Reg Volon" ) ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Bet-Lem Reg" ) ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Lake Shire" ) ) ); 
			Region.AddRegion( GuardedRegion.Disable( new IlshenarCity( "Ancient Citadel" ) ) );
		}

		public IlshenarCity( string name ) : this( name, typeof( ArcherGuard ) )
		{
		}

		public IlshenarCity( string name, Type guardType ) : base( "the town of", name, Map.Ilshenar, guardType )
		{
		}
	}
}