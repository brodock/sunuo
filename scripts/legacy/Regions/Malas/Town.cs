using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class MalasCity : GuardedRegion
	{
		public static new void Initialize()
		{
			Region.AddRegion( new MalasCity( "Luna" ) ); 
			Region.AddRegion( new MalasCity( "Umbra" ) ); 
		}

		public MalasCity( string name ) : this( name, typeof( ArcherGuard ) )
		{
		}

		public MalasCity( string name, Type guardType ) : base( "the town of", name, Map.Malas, guardType )
		{
		}
	}
}