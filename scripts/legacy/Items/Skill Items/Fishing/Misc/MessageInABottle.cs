using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class MessageInABottle : Item
	{
		public override int LabelNumber{ get{ return 1041080; } } // a message in a bottle

		private Map m_TargetMap;

		[CommandProperty( AccessLevel.GameMaster )]
		public Map TargetMap
		{
			get{ return m_TargetMap; }
			set{ m_TargetMap = value; }
		}

		[Constructable]
		public MessageInABottle() : this( Map.Trammel )
		{
		}

		[Constructable]
		public MessageInABottle( Map map ) : base( 0x099F )
		{
			Weight = 1.0;
			m_TargetMap = map;
		}

		public MessageInABottle( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( m_TargetMap );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_TargetMap = reader.ReadMap();
					break;
				}
				case 0:
				{
					m_TargetMap = Map.Trammel;
					break;
				}
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsChildOf( from.Backpack ) )
			{
				Consume();
				from.AddToBackpack( new SOS( m_TargetMap ) );
				from.LocalOverheadMessage( Network.MessageType.Regular, 0x3B2, 501891 ); // You extract the message from the bottle.
			}
			else
			{
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			}
		}
	}
}