using System;
using System.Collections;
using Server;
using Server.Targeting;
using Server.Network;
using Server.Spells;
using Server.Scripts.Commands;

namespace Server.Items
{
	public enum SpellbookType
	{
		Invalid = -1,
		Regular,
		Necromancer,
		Paladin,
		Ninja,
		Samurai
	}

	public class Spellbook : Item
	{
		public static void Initialize()
		{
			EventSink.OpenSpellbookRequest += new OpenSpellbookRequestEventHandler( EventSink_OpenSpellbookRequest );
			EventSink.CastSpellRequest += new CastSpellRequestEventHandler( EventSink_CastSpellRequest );
			Commands.Register( "AllSpells", AccessLevel.GameMaster, new CommandEventHandler( AllSpells_OnCommand ) );
		}

		[Usage( "AllSpells" )]
		[Description( "Completely fills a targeted spellbook with scrolls." )]
		private static void AllSpells_OnCommand( CommandEventArgs e )
		{
			e.Mobile.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( AllSpells_OnTarget ) );
			e.Mobile.SendMessage( "Target the spellbook to fill." );
		}

		private static void AllSpells_OnTarget( Mobile from, object obj )
		{
			if ( obj is Spellbook )
			{
				Spellbook book = (Spellbook)obj;

				if ( book.BookCount == 64 )
					book.Content = ulong.MaxValue;
				else
					book.Content = (1ul << book.BookCount) - 1;

				from.SendMessage( "The spellbook has been filled." );

				CommandLogging.WriteLine( from, "{0} {1} filling spellbook {2}", from.AccessLevel, CommandLogging.Format( from ), CommandLogging.Format( book ) );
			}
			else
			{
				from.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( AllSpells_OnTarget ) );
				from.SendMessage( "That is not a spellbook. Try again." );
			}
		}

		private static void EventSink_OpenSpellbookRequest( OpenSpellbookRequestEventArgs e )
		{
			Mobile from = e.Mobile;

			if ( !Multis.DesignContext.Check( from ) )
				return; // They are customizing

			SpellbookType type;

			switch ( e.Type )
			{
				default:
				case 1: type = SpellbookType.Regular; break;
				case 2: type = SpellbookType.Necromancer; break;
				case 3: type = SpellbookType.Paladin; break;
				case 4: type = SpellbookType.Ninja; break;
				case 5: type = SpellbookType.Samurai; break;
			}

			Spellbook book = Spellbook.Find( from, -1, type );

			if ( book != null )
				book.DisplayTo( from );
		}

		private static void EventSink_CastSpellRequest( CastSpellRequestEventArgs e )
		{
			Mobile from = e.Mobile;

			if ( !Multis.DesignContext.Check( from ) )
				return; // They are customizing

			Spellbook book = e.Spellbook as Spellbook;
			int spellID = e.SpellID;

			if ( book == null || !book.HasSpell( spellID ) )
				book = Find( from, spellID );

			if ( book != null && book.HasSpell( spellID ) )
			{
				Spell spell = SpellRegistry.NewSpell( spellID, from, null );

				if ( spell != null )
					spell.Cast();
				else
					from.SendLocalizedMessage( 502345 ); // This spell has been temporarily disabled.
			}
			else
			{
				from.SendLocalizedMessage( 500015 ); // You do not have that spell!
			}
		}

		private static Hashtable m_Table = new Hashtable();

		public static SpellbookType GetTypeForSpell( int spellID )
		{
			if ( spellID >= 0 && spellID < 64 )
				return SpellbookType.Regular;
			else if ( spellID >= 100 && spellID < 116 )
				return SpellbookType.Necromancer;
			else if ( spellID >= 200 && spellID < 210 )
				return SpellbookType.Paladin;
			else if( spellID >= 400 && spellID < 406 )
				return SpellbookType.Samurai;
			else if( spellID >= 500 && spellID < 508 )
				return SpellbookType.Ninja;

			return SpellbookType.Invalid;
		}

		public static Spellbook FindRegular( Mobile from )
		{
			return Find( from, -1, SpellbookType.Regular );
		}

		public static Spellbook FindNecromancer( Mobile from )
		{
			return Find( from, -1, SpellbookType.Necromancer );
		}

		public static Spellbook FindPaladin( Mobile from )
		{
			return Find( from, -1, SpellbookType.Paladin );
		}

		public static Spellbook FindSamurai( Mobile from )
		{
			return Find( from, -1, SpellbookType.Samurai );
		}

		public static Spellbook FindNinja( Mobile from )
		{
			return Find( from, -1, SpellbookType.Ninja );
		}

		public static Spellbook Find( Mobile from, int spellID )
		{
			return Find( from, spellID, GetTypeForSpell( spellID ) );
		}

		public static Spellbook Find( Mobile from, int spellID, SpellbookType type )
		{
			if ( from == null )
				return null;

			ArrayList list = (ArrayList)m_Table[from];

			if ( from.Deleted )
			{
				m_Table.Remove( from );
				return null;
			}

			bool searchAgain = false;

			if ( list == null )
				m_Table[from] = list = FindAllSpellbooks( from );
			else
				searchAgain = true;

			Spellbook book = FindSpellbookInList( list, from, spellID, type );

			if ( book == null && searchAgain )
			{
				m_Table[from] = list = FindAllSpellbooks( from );

				book = FindSpellbookInList( list, from, spellID, type );
			}

			return book;
		}

		public static Spellbook FindSpellbookInList( ArrayList list, Mobile from, int spellID, SpellbookType type )
		{
			Container pack = from.Backpack;

			for ( int i = list.Count - 1; i >= 0; --i )
			{
				if ( i >= list.Count )
					continue;

				Spellbook book = (Spellbook)list[i];

				if ( !book.Deleted && (book.Parent == from || (pack != null && book.Parent == pack)) && ValidateSpellbook( book, spellID, type ) )
					return book;

				list.Remove( i );
			}

			return null;
		}

		public static ArrayList FindAllSpellbooks( Mobile from )
		{
			ArrayList list = new ArrayList();

			Item item = from.FindItemOnLayer( Layer.OneHanded );

			if ( item is Spellbook )
				list.Add( item );

			Container pack = from.Backpack;

			if ( pack == null )
				return list;

			for ( int i = 0; i < pack.Items.Count; ++i )
			{
				item = (Item)pack.Items[i];

				if ( item is Spellbook )
					list.Add( item );
			}

			return list;
		}

		public static bool ValidateSpellbook( Spellbook book, int spellID, SpellbookType type )
		{
			return ( book.SpellbookType == type && ( spellID == -1 || book.HasSpell( spellID ) ) );
		}

		public virtual SpellbookType SpellbookType{ get{ return SpellbookType.Regular; } }
		public virtual int BookOffset{ get{ return 0; } }
		public virtual int BookCount{ get{ return 64; } }

		private ulong m_Content;
		private int m_Count;

		public override bool AllowEquipedCast( Mobile from )
		{
			return true;
		}

		public override Item Dupe( int amount )
		{
			Spellbook book = new Spellbook();

			book.Content = this.Content;
			book.LootType = this.LootType;

			return base.Dupe( book, amount );
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( dropped is SpellScroll && dropped.Amount == 1 )
			{
				SpellScroll scroll = (SpellScroll)dropped;

				SpellbookType type = GetTypeForSpell( scroll.SpellID );

				if ( type != this.SpellbookType )
				{
					return false;
				}
				else if ( HasSpell( scroll.SpellID ) )
				{
					from.SendLocalizedMessage( 500179 ); // That spell is already present in that spellbook.
					return false;
				}
				else
				{
					int val = scroll.SpellID - BookOffset;

					if ( val >= 0 && val < BookCount )
					{
						m_Content |= (ulong)1 << val;
						++m_Count;

						InvalidateProperties();

						scroll.Delete();

						from.Send( new PlaySound( 0x249, GetWorldLocation() ) );
						return true;
					}

					return false;
				}
			}
			else
			{
				return false;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public ulong Content
		{
			get
			{
				return m_Content;
			}
			set
			{
				if ( m_Content != value )
				{
					m_Content = value;

					m_Count = 0;

					while ( value > 0 )
					{
						m_Count += (int)(value & 0x1);
						value >>= 1;
					}

					InvalidateProperties();
				}
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int SpellCount
		{
			get
			{
				return m_Count;
			}
		}

		[Constructable]
		public Spellbook() : this( (ulong)0 )
		{
		}

		[Constructable]
		public Spellbook( ulong content ) : this( content, 0xEFA )
		{
		}

		public Spellbook( ulong content, int itemID ) : base( itemID )
		{
			Weight = 3.0;
			Layer = Layer.OneHanded;
			LootType = LootType.Blessed;

			Content = content;
		}

		public bool HasSpell( int spellID )
		{
			spellID -= BookOffset;

			return ( spellID >= 0 && spellID < BookCount && (m_Content & ((ulong)1 << spellID)) != 0 );
		}

		public Spellbook( Serial serial ) : base( serial )
		{
		}

		private static readonly ClientVersion Version_400a = new ClientVersion( "4.0.0a" );

		public void DisplayTo( Mobile to )
		{
			// The client must know about the spellbook or it will crash!

			if ( Parent == null )
			{
				to.Send( this.WorldPacket );
			}
			else if ( Parent is Item )
			{
				// What will happen if the client doesn't know about our parent?
				to.Send( new ContainerContentUpdate( this ) );
			}
			else if ( Parent is Mobile )
			{
				// What will happen if the client doesn't know about our parent?
				to.Send( new EquipUpdate( this ) );
			}

			to.Send( new DisplaySpellbook( this ) );

			if ( Core.AOS && to.NetState != null && to.NetState.Version != null && to.NetState.Version >= Version_400a )
				to.Send( new NewSpellbookContent( this, ItemID, BookOffset + 1, m_Content ) );
			else
				to.Send( new SpellbookContent( m_Count, BookOffset + 1, m_Content, this ) );
		}

		public override bool DisplayLootType{ get{ return Core.AOS; } }

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1042886, m_Count.ToString() ); // ~1_NUMBERS_OF_SPELLS~ Spells
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			this.LabelTo( from, 1042886, m_Count.ToString() );
		}

		public override void OnDoubleClick( Mobile from )
		{
			Container pack = from.Backpack;

			if ( Parent == from || ( pack != null && Parent == pack ) )
				DisplayTo( from );
			else
				from.SendLocalizedMessage( 500207 ); // The spellbook must be in your backpack (and not in a container within) to open.
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Content );
			writer.Write( m_Count );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			LootType = LootType.Blessed;

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Content = reader.ReadULong();
					m_Count = reader.ReadInt();

					break;
				}
			}
		}
	}
}