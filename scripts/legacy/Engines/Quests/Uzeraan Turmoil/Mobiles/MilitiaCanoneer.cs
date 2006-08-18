using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Quests;

namespace Server.Engines.Quests.Haven
{
	public class MilitiaCanoneer : BaseQuester
	{
		private bool m_Active;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Active
		{
			get { return m_Active; }
			set { m_Active = value; }
		}

		[Constructable]
		public MilitiaCanoneer() : base( "the Militia Canoneer" )
		{
			m_Active = true;
		}

		public override void InitBody()
		{
			InitStats( 100, 125, 25 );

			Hue = Utility.RandomSkinHue();

			Female = false;
			Body = 0x190;
			Name = NameList.RandomName( "male" );
		}

		public override void InitOutfit()
		{
			Item hair = new Item( Utility.RandomList( 0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2047, 0x2049, 0x204A ) );
			hair.Hue = Utility.RandomHairHue();
			hair.Layer = Layer.Hair;
			hair.Movable = false;
			AddItem( hair );

			Item beard = new Item( Utility.RandomList( 0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D ) );
			beard.Hue = hair.Hue;
			beard.Layer = Layer.FacialHair;
			beard.Movable = false;
			AddItem( beard );

			AddItem( new PlateChest() );
			AddItem( new PlateArms() );
			AddItem( new PlateGloves() );
			AddItem( new PlateLegs() );

			Torch torch = new Torch();
			torch.Movable = false;
			AddItem( torch );
			torch.Ignite();
		}

		public override bool CanTalkTo( PlayerMobile to )
		{
			return false;
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
		}

		public override bool IsEnemy( Mobile m )
		{
			if ( m.Player || m is BaseVendor )
				return false;

			if ( m is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)m;

				Mobile master = bc.GetMaster();
				if( master != null )
					return IsEnemy( master );
			}

			return m.Karma < 0;
		}

		public bool WillFire( Cannon cannon, Mobile target )
		{
			if ( m_Active && IsEnemy( target ) )
			{
				Direction = GetDirectionTo( target );
				Say( Utility.RandomList( 500651, 1049098, 1049320, 1043149 ) );
				return true;
			}

			return false;
		}

		public MilitiaCanoneer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (bool) m_Active );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Active = reader.ReadBool();
		}
	}
}