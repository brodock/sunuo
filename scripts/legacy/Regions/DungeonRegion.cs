using System;
using Server;
using Server.Mobiles;
using Server.Gumps;

namespace Server.Regions
{
	public class DungeonRegion : Region
	{
		public DungeonRegion( string name, Map map ) : base( "the dungeon", name, map )
		{
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}

		public override void OnEnter( Mobile m )
		{
			if ( m is PlayerMobile && ((PlayerMobile)m).Young )
				m.SendGump( new YoungDungeonWarning() );

			// Suppress default behavior
		}

		public override void OnExit( Mobile m )
		{
			// Suppress default behavior
		}

		public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
		{
			global = LightCycle.DungeonLevel;
		}
	}
}