using System;
using System.Collections;
using Server;
using Server.Regions;

namespace Server.Factions
{
	public class StrongholdRegion : Region
	{
		private Faction m_Faction;

		public Faction Faction
		{
			get{ return m_Faction; }
			set{ m_Faction = value; }
		}

		public StrongholdRegion( Faction faction ) : base( "Faction Stronghold of the", faction.Definition.FriendlyName, Faction.Facet )
		{
			m_Faction = faction;

			Priority = TownPriority + 5;
			LoadFromXml = false;

			Coords = new ArrayList();
			Coords.AddRange( faction.Definition.Stronghold.Area );
		}

		public override bool OnMoveInto( Mobile m, Direction d, Point3D newLocation, Point3D oldLocation )
		{
			if ( m.AccessLevel >= AccessLevel.Counselor || Contains( oldLocation ) )
				return true;

			return ( Faction.Find( m, true, true ) != null );
		}

		public override void OnEnter( Mobile m )
		{
		}

		public override void OnExit( Mobile m )
		{
		}
	}
}