using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
	public abstract class BaseChampion : BaseCreature
	{
		public BaseChampion( AIType aiType ) : this( aiType, FightMode.Closest )
		{
		}

		public BaseChampion( AIType aiType, FightMode mode ) : base( aiType, mode, 18, 1, 0.1, 0.2 )
		{
		}

		public BaseChampion( Serial serial ) : base( serial )
		{
		}

		public abstract ChampionSkullType SkullType{ get; }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public void GivePowerScrolls()
		{
			if ( Map != Map.Felucca )
				return;

			ArrayList toGive = new ArrayList();
			ArrayList rights = BaseCreature.GetLootingRights( this.DamageEntries, this.HitsMax );

			for ( int i = rights.Count - 1; i >= 0; --i )
			{
				DamageStore ds = (DamageStore)rights[i];

				if ( ds.m_HasRight )
					toGive.Add( ds.m_Mobile );
			}

			if ( toGive.Count == 0 )
				return;

			// Randomize
			for ( int i = 0; i < toGive.Count; ++i )
			{
				int rand = Utility.Random( toGive.Count );
				object hold = toGive[i];
				toGive[i] = toGive[rand];
				toGive[rand] = hold;
			}

			for ( int i = 0; i < 6; ++i )
			{
				int level;
				double random = Utility.RandomDouble();

				if ( 0.1 >= random )
					level = 20;
				else if ( 0.4 >= random )
					level = 15;
				else
					level = 10;

				Mobile m = (Mobile)toGive[i % toGive.Count];

				PowerScroll ps = PowerScroll.CreateRandomNoCraft( level, level );

				m.SendLocalizedMessage( 1049524 ); // You have received a scroll of power!
				m.AddToBackpack( ps );

				if ( m is PlayerMobile )
				{
					PlayerMobile pm = (PlayerMobile)m;

					for ( int j = 0; j < pm.JusticeProtectors.Count; ++j )
					{
						Mobile prot = (Mobile)pm.JusticeProtectors[j];

						if ( prot.Map != m.Map || prot.Kills >= 5 || prot.Criminal || !JusticeVirtue.CheckMapRegion( m, prot ) )
							continue;

						int chance = 0;

						switch ( VirtueHelper.GetLevel( prot, VirtueName.Justice ) )
						{
							case VirtueLevel.Seeker: chance = 60; break;
							case VirtueLevel.Follower: chance = 80; break;
							case VirtueLevel.Knight: chance = 100; break;
						}

						if ( chance > Utility.Random( 100 ) )
						{
							prot.SendLocalizedMessage( 1049368 ); // You have been rewarded for your dedication to Justice!
							prot.AddToBackpack( new PowerScroll( ps.Skill, ps.Value ) );
						}
					}
				}
			}
		}

		public override bool OnBeforeDeath()
		{
			if ( !NoKillAwards )
			{
				GivePowerScrolls();

				Map map = this.Map;

				if ( map != null )
				{
					for ( int x = -12; x <= 12; ++x )
					{
						for ( int y = -12; y <= 12; ++y )
						{
							double dist = Math.Sqrt(x*x+y*y);

							if ( dist <= 12 )
								new GoodiesTimer( map, X + x, Y + y ).Start();
						}
					}
				}
			}

			return base.OnBeforeDeath();
		}

		public override void OnDeath( Container c )
		{
			if ( Map == Map.Felucca )
				c.DropItem( new ChampionSkull( SkullType ) );

			base.OnDeath( c );
		}

		private class GoodiesTimer : Timer
		{
			private Map m_Map;
			private int m_X, m_Y;

			public GoodiesTimer( Map map, int x, int y ) : base( TimeSpan.FromSeconds( Utility.RandomDouble() * 10.0 ) )
			{
				m_Map = map;
				m_X = x;
				m_Y = y;
			}

			protected override void OnTick()
			{
				int z = m_Map.GetAverageZ( m_X, m_Y );
				bool canFit = m_Map.CanFit( m_X, m_Y, z, 6, false, false );

				for ( int i = -3; !canFit && i <= 3; ++i )
				{
					canFit = m_Map.CanFit( m_X, m_Y, z + i, 6, false, false );

					if ( canFit )
						z += i;
				}

				if ( !canFit )
					return;

				Gold g = new Gold( 500, 1000 );
				
				g.MoveToWorld( new Point3D( m_X, m_Y, z ), m_Map );

				if ( 0.5 >= Utility.RandomDouble() )
				{
					switch ( Utility.Random( 3 ) )
					{
						case 0: // Fire column
						{
							Effects.SendLocationParticles( EffectItem.Create( g.Location, g.Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 5052 );
							Effects.PlaySound( g, g.Map, 0x208 );

							break;
						}
						case 1: // Explosion
						{
							Effects.SendLocationParticles( EffectItem.Create( g.Location, g.Map, EffectItem.DefaultDuration ), 0x36BD, 20, 10, 5044 );
							Effects.PlaySound( g, g.Map, 0x307 );

							break;
						}
						case 2: // Ball of fire
						{
							Effects.SendLocationParticles( EffectItem.Create( g.Location, g.Map, EffectItem.DefaultDuration ), 0x36FE, 10, 10, 5052 );

							break;
						}
					}
				}
			}
		}
	}
}