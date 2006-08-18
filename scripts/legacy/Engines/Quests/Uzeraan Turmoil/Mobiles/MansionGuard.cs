using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.Quests;

namespace Server.Engines.Quests.Haven
{
	public class MansionGuard : BaseQuester
	{
		[Constructable]
		public MansionGuard() : base( "the Mansion Guard" )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Hue = Utility.RandomSkinHue();

			Female = false;
			Body = 0x190;
			Name = NameList.RandomName( "male" );
		}

		public override void InitOutfit()
		{
			AddItem( new PlateChest() );
			AddItem( new PlateArms() );
			AddItem( new PlateGloves() );
			AddItem( new PlateLegs() );

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

			Bardiche weapon = new Bardiche();
			weapon.Movable = false;
			AddItem( weapon );
		}

		public override int GetAutoTalkRange( PlayerMobile pm )
		{
			return 3;
		}

		public override bool CanTalkTo( PlayerMobile to )
		{
			return ( to.Quest == null && QuestSystem.CanOfferQuest( to, typeof( UzeraanTurmoilQuest ) ) );
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
			if ( player.Quest == null && QuestSystem.CanOfferQuest( player, typeof( UzeraanTurmoilQuest ) ) )
			{
				Direction = GetDirectionTo( player );

				new UzeraanTurmoilQuest( player ).SendOffer();
			}
		}

		public MansionGuard( Serial serial ) : base( serial )
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
	}
}