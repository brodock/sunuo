using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class NoHousingRegion : Region
	{
		public static void Initialize()
		{
			/* The first parameter is a boolean value:
			 *  - False: this uses 'stupid OSI' house placement checking: part of the house may be placed here provided that the center is not in the region
			 *  -  True: this uses 'smart RunUO' house placement checking: no part of the house may be in the region
			 */

			Region.AddRegion( new NoHousingRegion( false, "", "Britain Graveyard", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Wrong Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Covetous Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Despise Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Despise Passage", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Jhelom Islands", Map.Felucca ) );

			Region.AddRegion( new NoHousingRegion( false, "", "Britain Graveyard", Map.Trammel ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Wrong Entrance", Map.Trammel ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Covetous Entrance", Map.Trammel ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Despise Entrance", Map.Trammel ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Despise Passage", Map.Trammel ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Jhelom Islands", Map.Trammel ) );
			Region.AddRegion( new NoHousingRegion(  true, "", "Haven Island", Map.Trammel ) );

			Region.AddRegion( new NoHousingRegion( false, "", "Crystal Cave Entrance", Map.Malas ) );
			Region.AddRegion( new NoHousingRegion(  true, "", "Protected Island", Map.Malas ) );
		}

		private bool m_SmartChecking;

		public bool SmartChecking{ get{ return m_SmartChecking; } }

		public NoHousingRegion( bool smartChecking, string prefix, string name, Map map ) : base( prefix, name, map )
		{
			m_SmartChecking = smartChecking;
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return m_SmartChecking;
		}

		public override void OnEnter( Mobile m )
		{
		}

		public override void OnExit( Mobile m )
		{
		}
	}
}