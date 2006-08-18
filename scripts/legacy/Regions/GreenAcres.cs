using System;
using Server;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Seventh;
using Server.Spells.Fourth;
using Server.Spells.Sixth;

namespace Server.Regions
{
	public class GreenAcres : Region
	{
		public static void Initialize()
		{
			Region.AddRegion( new GreenAcres( Map.Felucca ) );
			Region.AddRegion( new GreenAcres( Map.Trammel ) );
		}

		public GreenAcres( Map map ) : base( "", "Green Acres", map )
		{
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			if ( from.AccessLevel == AccessLevel.Player )
				return false;
			else
				return true;
		}

		public override void OnEnter( Mobile m )
		{
		}

		public override void OnExit( Mobile m )
		{
		}

		public override bool OnBeginSpellCast( Mobile m, ISpell s )
		{
			if ( ( s is GateTravelSpell || s is RecallSpell || s is MarkSpell ) && m.AccessLevel == AccessLevel.Player )
			{
				m.SendMessage( "You cannot cast that spell here." );
				return false;
			}
			else
			{
				return base.OnBeginSpellCast( m, s );
			}
		}
	}
}
