using System;
using System.Collections;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.CannedEvil
{
	public class ChampionSpawn : Item
	{
		private bool m_Active;
		private bool m_RandomizeType;
		private ChampionSpawnType m_Type;
		private ArrayList m_Creatures;
		private ArrayList m_RedSkulls;
		private ArrayList m_WhiteSkulls;
		private ChampionPlatform m_Platform;
		private ChampionAltar m_Altar;
		private int m_Kills;
		private Mobile m_Champion;
		private int m_SpawnRange;

		private TimeSpan m_ExpireDelay;
		private DateTime m_ExpireTime;

		private TimeSpan m_RestartDelay;
		private DateTime m_RestartTime;

		private Timer m_Timer, m_RestartTimer;

		[Constructable]
		public ChampionSpawn() : base( 0xBD2 )
		{
			Movable = false;
			Visible = false;

			m_Creatures = new ArrayList();
			m_RedSkulls = new ArrayList();
			m_WhiteSkulls = new ArrayList();

			m_Platform = new ChampionPlatform( this );
			m_Altar = new ChampionAltar( this );

			m_ExpireDelay = TimeSpan.FromMinutes( 10.0 );
			m_RestartDelay = TimeSpan.FromMinutes( 5.0 );

			m_SpawnRange = 24;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool RandomizeType
		{
			get{ return m_RandomizeType; }
			set{ m_RandomizeType = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Kills
		{
			get
			{
				return m_Kills;
			}
			set
			{
				m_Kills = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int SpawnRange
		{
			get
			{
				return m_SpawnRange;
			}
			set
			{
				m_SpawnRange = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan RestartDelay
		{
			get
			{
				return m_RestartDelay;
			}
			set
			{
				m_RestartDelay = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime RestartTime
		{
			get
			{
				return m_RestartTime;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan ExpireDelay
		{
			get
			{
				return m_ExpireDelay;
			}
			set
			{
				m_ExpireDelay = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime ExpireTime
		{
			get
			{
				return m_ExpireTime;
			}
			set
			{
				m_ExpireTime = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public ChampionSpawnType Type
		{
			get
			{
				return m_Type;
			}
			set
			{
				m_Type = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Active
		{
			get
			{
				return m_Active;
			}
			set
			{
				if ( value )
					Start();
				else
					Stop();

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Champion
		{
			get
			{
				return m_Champion;
			}
			set
			{
				m_Champion = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Level
		{
			get
			{
				return m_RedSkulls.Count;
			}
			set
			{
				for ( int i = m_RedSkulls.Count - 1; i >= value; --i )
				{
					((Item)m_RedSkulls[i]).Delete();
					m_RedSkulls.RemoveAt( i );
				}

				for ( int i = m_RedSkulls.Count; i < value; ++i )
				{
					Item skull = new Item( 0x1854 );

					skull.Hue = 0x26;
					skull.Movable = false;
					skull.Light = LightType.Circle150;

					skull.MoveToWorld( GetRedSkullLocation( i ), Map );

					m_RedSkulls.Add( skull );
				}

				InvalidateProperties();
			}
		}

		public int MaxKills
		{
			get
			{
				return 250 - (Level * 12);
			}
		}

		public void SetWhiteSkullCount( int val )
		{
			for ( int i = m_WhiteSkulls.Count - 1; i >= val; --i )
			{
				((Item)m_WhiteSkulls[i]).Delete();
				m_WhiteSkulls.RemoveAt( i );
			}

			for ( int i = m_WhiteSkulls.Count; i < val; ++i )
			{
				Item skull = new Item( 0x1854 );

				skull.Movable = false;
				skull.Light = LightType.Circle150;

				skull.MoveToWorld( GetWhiteSkullLocation( i ), Map );

				m_WhiteSkulls.Add( skull );

				Effects.PlaySound( skull.Location, skull.Map, 0x29 );
				Effects.SendLocationEffect( new Point3D( skull.X + 1, skull.Y + 1, skull.Z ), skull.Map, 0x3728, 10 );
			}
		}

		public void Start()
		{
			if ( m_Active || Deleted )
				return;

			m_Active = true;

			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = new SliceTimer( this );
			m_Timer.Start();

			if ( m_RestartTimer != null )
				m_RestartTimer.Stop();

			m_RestartTimer = null;

			if ( m_Altar != null )
				m_Altar.Hue = 0;
		}

		public void Stop()
		{
			if ( !m_Active || Deleted )
				return;

			m_Active = false;

			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = null;

			if ( m_RestartTimer != null )
				m_RestartTimer.Stop();

			m_RestartTimer = null;

			if ( m_Altar != null )
				m_Altar.Hue = 0;
		}

		public void BeginRestart( TimeSpan ts )
		{
			if ( m_RestartTimer != null )
				m_RestartTimer.Stop();

			m_RestartTime = DateTime.Now + ts;

			m_RestartTimer = new RestartTimer( this, ts );
			m_RestartTimer.Start();
		}

		public void EndRestart()
		{
			if ( RandomizeType )
			{
				switch ( Utility.Random( 5 ) )
				{
					case 0: Type = ChampionSpawnType.VerminHorde; break;
					case 1: Type = ChampionSpawnType.UnholyTerror; break;
					case 2: Type = ChampionSpawnType.ColdBlood; break;
					case 3: Type = ChampionSpawnType.Abyss; break;
					case 4: Type = ChampionSpawnType.Arachnid; break;
				}
			}

			Start();
		}

		public void OnSlice()
		{
			if ( !m_Active || Deleted )
				return;

			if ( m_Champion != null )
			{
				if ( m_Champion.Deleted )
				{
					if ( m_Altar != null )
					{
						m_Altar.Hue = 0;
						new StarRoomGate( true, m_Altar.Location, m_Altar.Map );
					}

					m_Champion = null;
					Stop();

					BeginRestart( m_RestartDelay );
				}
			}
			else
			{
				for ( int i = 0; i < m_Creatures.Count; ++i )
				{
					Mobile m = (Mobile)m_Creatures[i];

					if ( m.Deleted )
					{
						m_Creatures.RemoveAt( i );
						--i;
						++m_Kills;
						InvalidateProperties();
					}
				}

				double n = m_Kills / (double)MaxKills;
				int p = (int)(n * 100);

				if ( p >= 90 )
					AdvanceLevel();
				else if ( p > 0 )
					SetWhiteSkullCount( p / 20 );

				if ( DateTime.Now >= m_ExpireTime )
					Expire();

				Respawn();
			}
		}

		public void AdvanceLevel()
		{
			m_ExpireTime = DateTime.Now + m_ExpireDelay;

			if ( Level < 16 )
			{
				m_Kills = 0;
				++Level;
				InvalidateProperties();
				SetWhiteSkullCount( 0 );

				if ( m_Altar != null )
				{
					Effects.PlaySound( m_Altar.Location, m_Altar.Map, 0x29 );
					Effects.SendLocationEffect( new Point3D( m_Altar.X + 1, m_Altar.Y + 1, m_Altar.Z ), m_Altar.Map, 0x3728, 10 );
				}
			}
			else
			{
				SpawnChampion();
			}
		}

		public void SpawnChampion()
		{
			if ( m_Altar != null )
				m_Altar.Hue = 0x26;

			m_Kills = 0;
			Level = 0;
			InvalidateProperties();
			SetWhiteSkullCount( 0 );

			switch ( m_Type )
			{
				default:
				case ChampionSpawnType.UnholyTerror: m_Champion = new Neira(); break;
				case ChampionSpawnType.VerminHorde: m_Champion = new Barracoon(); break;
				case ChampionSpawnType.ForestLord: m_Champion = new LordOaks(); break;
				case ChampionSpawnType.ColdBlood: m_Champion = new Rikktor(); break;
				case ChampionSpawnType.Arachnid: m_Champion = new Mephitis(); break;
				case ChampionSpawnType.Abyss: m_Champion = new Semidar(); break;
			}

			m_Champion.MoveToWorld( new Point3D( X, Y, Z - 15 ), Map );
		}

		public void Respawn()
		{
			if ( !m_Active || Deleted || m_Champion != null )
				return;

			while ( m_Creatures.Count < (50 - (GetSubLevel() * 10)) )
			{
				Mobile m = Spawn();

				if ( m == null )
					return;

				m_Creatures.Add( m );
				m.MoveToWorld( GetSpawnLocation(), Map );

				if ( m is BaseCreature )
				{
					((BaseCreature)m).Tamable = false;
					((BaseCreature)m).Home = Location;
					((BaseCreature)m).RangeHome = m_SpawnRange;
				}
			}
		}

		public Point3D GetSpawnLocation()
		{
			Map map = Map;

			if ( map == null )
				return Location;

			// Try 20 times to find a spawnable location.
			for ( int i = 0; i < 20; i++ )
			{
				int x = Location.X + (Utility.Random( (m_SpawnRange * 2) + 1 ) - m_SpawnRange);
				int y = Location.Y + (Utility.Random( (m_SpawnRange * 2) + 1 ) - m_SpawnRange);
				int z = Map.GetAverageZ( x, y );

				if ( Map.CanSpawnMobile( new Point2D( x, y ), z ) )
					return new Point3D( x, y, z );
			}

			return Location;
		}

		private const int Level1 = 5;  // First spawn level from 0-5 red skulls
		private const int Level2 = 9;  // Second spawn level from 6-9 red skulls
		private const int Level3 = 13; // Third spawn level from 10-13 red skulls

		private static Type[][][] m_Types = new Type[6][][]
			{
				new Type[][]
				{																											// Abyss
					new Type[]{ typeof( Mongbat ), typeof( Imp ) },															// Level 1
					new Type[]{ typeof( Gargoyle ), typeof( Harpy ) },														// Level 2
					new Type[]{ typeof( FireGargoyle ), typeof( StoneGargoyle ) },											// Level 3
					new Type[]{ typeof( Daemon ), typeof( Succubus ) }														// Level 4
				},
				new Type[][]
				{																											// Arachnid
					new Type[]{ typeof( Scorpion ), typeof( GiantSpider ) },												// Level 1
					new Type[]{ typeof( TerathanDrone ), typeof( TerathanWarrior ) },										// Level 2
					new Type[]{ typeof( DreadSpider ), typeof( TerathanMatriarch ) },										// Level 3
					new Type[]{ typeof( PoisonElemental ), typeof( TerathanAvenger ) }										// Level 4
				},
				new Type[][]
				{																											// Cold Blood
					new Type[]{ typeof( Lizardman ), typeof( Snake ) },														// Level 1
					new Type[]{ typeof( LavaLizard ), typeof( OphidianWarrior ) },											// Level 2
					new Type[]{ typeof( Drake ), typeof( OphidianArchmage ) },												// Level 3
					new Type[]{ typeof( Dragon ), typeof( OphidianKnight ) }												// Level 4
				},
				new Type[][]
				{																											// Forest Lord
					new Type[]{ typeof( Pixie ), typeof( ShadowWisp ) },													// Level 1
					new Type[]{ typeof( Kirin ), typeof( Wisp ) },															// Level 2
					new Type[]{ typeof( Centaur ), typeof( Unicorn ) },														// Level 3
					new Type[]{ typeof( EtherealWarrior ), typeof( SerpentineDragon ) }										// Level 4
				},
				new Type[][]
				{																											// Vermin Horde
					new Type[]{ typeof( GiantRat ), typeof( Slime ) },														// Level 1
					new Type[]{ typeof( DireWolf ), typeof( Ratman ) },														// Level 2
					new Type[]{ typeof( HellHound ), typeof( RatmanMage ) },												// Level 3
					new Type[]{ typeof( RatmanArcher ), typeof( SilverSerpent ) }											// Level 4
				},
				new Type[][]
				{																											// Unholy Terror
					new Type[]{ typeof( Bogle ), typeof( Ghoul ), typeof( Shade ), typeof( Spectre ), typeof( Wraith ) },	// Level 1
					new Type[]{ typeof( BoneMagi ), typeof( Mummy ), typeof( SkeletalMage ) },								// Level 2
					new Type[]{ typeof( BoneKnight ), typeof( Lich ), typeof( SkeletalKnight ) },							// Level 3
					new Type[]{ typeof( LichLord ), typeof( RottingCorpse ) }												// Level 4
				}
			};

		public int GetSubLevel()
		{
			int level = this.Level;

			if ( level <= Level1 )
				return 0;
			else if ( level <= Level2 )
				return 1;
			else if ( level <= Level3 )
				return 2;

			return 3;
		}

		public Mobile Spawn()
		{
			int v = (int)m_Type;

			if ( v >= 0 && v < m_Types.Length )
			{
				Type[][] types = m_Types[v];

				v = GetSubLevel();

				if ( v >= 0 && v < types.Length )
					return Spawn( types[v] );
			}

			return null;
		}

		public Mobile Spawn( params Type[] types )
		{
			try
			{
				return Activator.CreateInstance( types[Utility.Random( types.Length )] ) as Mobile;
			}
			catch
			{
				return null;
			}
		}

		public void Expire()
		{
			m_Kills = 0;

			if ( m_WhiteSkulls.Count == 0 )
			{
				// They didn't even get 20%, go back a level

				if ( Level > 0 )
					--Level;

				InvalidateProperties();
			}
			else
			{
				SetWhiteSkullCount( 0 );
			}

			m_ExpireTime = DateTime.Now + m_ExpireDelay;
		}

		public Point3D GetRedSkullLocation( int index )
		{
			int x, y;

			if ( index < 5 )
			{
				x = index - 2;
				y = -2;
			}
			else if ( index < 9 )
			{
				x = 2;
				y = index - 6;
			}
			else if ( index < 13 )
			{
				x = 10 - index;
				y = 2;
			}
			else
			{
				x = -2;
				y = 14 - index;
			}

			return new Point3D( X + x, Y + y, Z - 15 );
		}

		public Point3D GetWhiteSkullLocation( int index )
		{
			int x, y;

			switch ( index )
			{
				default:
				case 0: x = -1; y = -1; break;
				case 1: x =  1; y = -1; break;
				case 2: x =  1; y =  1; break;
				case 3: x = -1; y =  1; break;
			}

			return new Point3D( X + x, Y + y, Z - 15 );
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			list.Add( "champion spawn" );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Active )
			{
				list.Add( 1060742 ); // active
				list.Add( 1060658, "Type\t{0}", m_Type ); // ~1_val~: ~2_val~
				list.Add( 1060659, "Level\t{0}", Level ); // ~1_val~: ~2_val~
				list.Add( 1060660, "Kills\t{0} of {1} ({2:F1}%)", m_Kills, MaxKills, 100.0 * ((double)m_Kills / MaxKills) ); // ~1_val~: ~2_val~
				list.Add( 1060661, "Spawn Range\t{0}", m_SpawnRange ); // ~1_val~: ~2_val~
			}
			else
			{
				list.Add( 1060743 ); // inactive
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( m_Active )
				LabelTo( from, "{0} (Active; Level: {1}; Kills: {2}/{3})", m_Type, Level, m_Kills, MaxKills );
			else
				LabelTo( from, "{0} (Inactive)", m_Type );
		}

		public override void OnDoubleClick( Mobile from )
		{
			from.SendGump( new PropertiesGump( from, this ) );
		}

		public override void OnLocationChange( Point3D oldLoc )
		{
			if ( Deleted )
				return;

			if ( m_Platform != null )
				m_Platform.Location = new Point3D( X, Y, Z - 20 );

			if ( m_Altar != null )
				m_Altar.Location = new Point3D( X, Y, Z - 15 );

			if ( m_RedSkulls != null )
			{
				for ( int i = 0; i < m_RedSkulls.Count; ++i )
					((Item)m_RedSkulls[i]).Location = GetRedSkullLocation( i );
			}

			if ( m_WhiteSkulls != null )
			{
				for ( int i = 0; i < m_WhiteSkulls.Count; ++i )
					((Item)m_WhiteSkulls[i]).Location = GetWhiteSkullLocation( i );
			}
		}

		public override void OnMapChange()
		{
			if ( Deleted )
				return;

			if ( m_Platform != null )
				m_Platform.Map = Map;

			if ( m_Altar != null )
				m_Altar.Map = Map;

			if ( m_RedSkulls != null )
			{
				for ( int i = 0; i < m_RedSkulls.Count; ++i )
					((Item)m_RedSkulls[i]).Map = Map;
			}

			if ( m_WhiteSkulls != null )
			{
				for ( int i = 0; i < m_WhiteSkulls.Count; ++i )
					((Item)m_WhiteSkulls[i]).Map = Map;
			}
		}

		public override void OnDelete()
		{
			base.OnDelete ();
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Platform != null )
				m_Platform.Delete();

			if ( m_Altar != null )
				m_Altar.Delete();

			if ( m_RedSkulls != null )
			{
				for ( int i = 0; i < m_RedSkulls.Count; ++i )
					((Item)m_RedSkulls[i]).Delete();

				m_RedSkulls.Clear();
			}

			if ( m_WhiteSkulls != null )
			{
				for ( int i = 0; i < m_WhiteSkulls.Count; ++i )
					((Item)m_WhiteSkulls[i]).Delete();

				m_WhiteSkulls.Clear();
			}

			if ( m_Creatures != null )
			{
				for ( int i = 0; i < m_Creatures.Count; ++i )
				{
					Mobile mob = (Mobile)m_Creatures[i];

					if ( !mob.Player )
						mob.Delete();
				}

				m_Creatures.Clear();
			}

			if ( m_Champion != null && !m_Champion.Player )
				m_Champion.Delete();

			Stop();
		}

		public ChampionSpawn( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 ); // version

			writer.Write( m_RandomizeType );

			writer.Write( m_SpawnRange );
			writer.Write( m_Kills );

			writer.Write( (bool) m_Active );
			writer.Write( (int) m_Type );
			writer.WriteMobileList( m_Creatures, true );
			writer.WriteItemList( m_RedSkulls, true );
			writer.WriteItemList( m_WhiteSkulls, true );
			writer.Write( m_Platform );
			writer.Write( m_Altar );
			writer.Write( m_ExpireDelay );
			writer.WriteDeltaTime( m_ExpireTime );
			writer.Write( m_Champion );
			writer.Write( m_RestartDelay );

			writer.Write( m_RestartTimer != null );

			if ( m_RestartTimer != null )
				writer.WriteDeltaTime( m_RestartTime );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
				{
					m_RandomizeType = reader.ReadBool();

					goto case 1;
				}
				case 1:
				{
					m_SpawnRange = reader.ReadInt();
					m_Kills = reader.ReadInt();

					goto case 0;
				}
				case 0:
				{
					if ( version < 1 )
						m_SpawnRange = 24;

					bool active = reader.ReadBool();
					m_Type = (ChampionSpawnType)reader.ReadInt();
					m_Creatures = reader.ReadMobileList();
					m_RedSkulls = reader.ReadItemList();
					m_WhiteSkulls = reader.ReadItemList();
					m_Platform = reader.ReadItem() as ChampionPlatform;
					m_Altar = reader.ReadItem() as ChampionAltar;
					m_ExpireDelay = reader.ReadTimeSpan();
					m_ExpireTime = reader.ReadDeltaTime();
					m_Champion = reader.ReadMobile();
					m_RestartDelay = reader.ReadTimeSpan();

					if ( reader.ReadBool() )
					{
						m_RestartTime = reader.ReadDeltaTime();
						BeginRestart( m_RestartTime - DateTime.Now );
					}

					if ( m_Platform == null || m_Altar == null )
						Delete();
					else if ( active )
						Start();

					break;
				}
			}
		}
	}
}