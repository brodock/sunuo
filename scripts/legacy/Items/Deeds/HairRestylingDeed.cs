using System;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Items;
using Server.Targeting;
using Server.Gumps;

namespace Server.Items
{
	public class HairRestylingDeed : Item
	{
		public override int LabelNumber{ get{ return 1041061; } } // a coupon for a free hair restyling

		[Constructable]
		public HairRestylingDeed() : base( 0x14F0 )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;
		}

		public HairRestylingDeed( Serial serial ) : base( serial )
		{
		}

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

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) )
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			else
				from.SendGump( new InternalGump( from, this ) );
		}

		private class InternalGump : Gump
		{
			private Mobile m_From;
			private HairRestylingDeed m_Deed;

			public InternalGump( Mobile from, HairRestylingDeed deed ) : base( 50, 50 )
			{
				m_From = from;
				m_Deed = deed;

				from.CloseGump( typeof( InternalGump ) );

				AddBackground( 100, 10, 400, 385, 0xA28 );

				AddHtmlLocalized( 100, 25, 400, 35, 1013008, false, false );
				AddButton( 175, 340, 0xFA5, 0xFA7, 0x0, GumpButtonType.Reply, 0 ); // CANCEL

				AddHtmlLocalized( 210, 342, 90, 35, 1011012, false, false );// <CENTER>HAIRSTYLE SELECTION MENU</center>

				AddBackground( 220, 60, 50, 50, 0xA3C );
				AddBackground( 220, 115, 50, 50, 0xA3C );
				AddBackground( 220, 170, 50, 50, 0xA3C );
				AddBackground( 220, 225, 50, 50, 0xA3C );
				AddBackground( 425, 60, 50, 50, 0xA3C );
				AddBackground( 425, 115, 50, 50, 0xA3C );
				AddBackground( 425, 170, 50, 50, 0xA3C );
				AddBackground( 425, 225, 50, 50, 0xA3C );
				AddBackground( 425, 280, 50, 50, 0xA3C );

				AddHtmlLocalized( 150, 75, 80, 35, 1011052, false, false ); // Short
				AddHtmlLocalized( 150, 130, 80, 35, 1011053, false, false ); // Long
				AddHtmlLocalized( 150, 185, 80, 35, 1011054, false, false ); // Ponytail
				AddHtmlLocalized( 150, 240, 80, 35, 1011055, false, false ); // Mohawk
				AddHtmlLocalized( 355, 75, 80, 35, 1011047, false, false ); // Pageboy
				AddHtmlLocalized( 355, 130, 80, 35, 1011048, false, false ); // Receding
				AddHtmlLocalized( 355, 185, 80, 35, 1011049, false, false ); // 2-tails
				AddHtmlLocalized( 355, 240, 80, 35, 1011050, false, false ); // Topknot
				AddHtmlLocalized( 355, 295, 80, 35, 1011064, false, false ); // Bald

				AddImage( 153, 20, 0xC60C );
				AddImage( 153, 65, 0xED24 );
				AddImage( 153, 120, 0xED1E );
				AddImage( 153, 185, 0xC60F );
				AddImage( 358, 18, 0xED26 );
				AddImage( 358, 75, 0xEDE5 );
				AddImage( 358, 120, 0xED23 );
				AddImage( 362, 190, 0xED29 );

				AddButton( 118, 73, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0 );
				AddButton( 118, 128, 0xFA5, 0xFA7, 3, GumpButtonType.Reply, 0 );
				AddButton( 118, 183, 0xFA5, 0xFA7, 4, GumpButtonType.Reply, 0 );
				AddButton( 118, 238, 0xFA5, 0xFA7, 5, GumpButtonType.Reply, 0 );
				AddButton( 323, 73, 0xFA5, 0xFA7, 6, GumpButtonType.Reply, 0 );
				AddButton( 323, 128, 0xFA5, 0xFA7, 7, GumpButtonType.Reply, 0 );
				AddButton( 323, 183, 0xFA5, 0xFA7, 8, GumpButtonType.Reply, 0 );
				AddButton( 323, 238, 0xFA5, 0xFA7, 9, GumpButtonType.Reply, 0 );
				AddButton( 323, 292, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0 );
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				if ( m_Deed.Deleted )
					return;

				Item newHair = null;

				switch ( info.ButtonID )
				{
					case 0: return;
					case 2: newHair = new ShortHair(); break;
					case 3: newHair = new LongHair(); break;
					case 4: newHair = new PonyTail(); break;
					case 5: newHair = new Mohawk(); break;
					case 6: newHair = new PageboyHair(); break;
					case 7: newHair = new ReceedingHair(); break;
					case 8: newHair = new TwoPigTails(); break;
					case 9: newHair = new KrisnaHair(); break;
				}

				if ( m_From is PlayerMobile )
				{
					PlayerMobile pm = (PlayerMobile)m_From;

					pm.SetHairMods( -1, -1 ); // clear any hairmods (disguise kit, incognito)
				}

				Item oldHair = m_From.FindItemOnLayer( Layer.Hair );

				if ( oldHair != null )
					oldHair.Delete();

				if ( newHair != null )
				{
					if ( oldHair != null )
						newHair.Hue = oldHair.Hue;

					m_From.AddItem( newHair );
				}

				m_Deed.Delete();
			}
		}
	}
}