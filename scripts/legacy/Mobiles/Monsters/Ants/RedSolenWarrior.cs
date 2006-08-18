using System;
using System.Collections;
using Server.Items;
using Server.Targeting;

namespace Server.Mobiles
{
	[CorpseName( "a solen warrior corpse" )]
	public class RedSolenWarrior : BaseCreature
	{
		[Constructable]
		public RedSolenWarrior() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a red solen warrior";
			Body = 782;
			BaseSoundID = 959;

			SetStr( 196, 220 );
			SetDex( 101, 125 );
			SetInt( 36, 60 );

			SetHits( 96, 107 );

			SetDamage( 5, 15 );

			SetDamageType( ResistanceType.Physical, 80 );
			SetDamageType( ResistanceType.Poison, 20 );

			SetResistance( ResistanceType.Physical, 20, 35 );
			SetResistance( ResistanceType.Fire, 20, 35 );
			SetResistance( ResistanceType.Cold, 10, 25 );
			SetResistance( ResistanceType.Poison, 20, 35 );
			SetResistance( ResistanceType.Energy, 10, 25 );

			SetSkill( SkillName.MagicResist, 60.0 );
			SetSkill( SkillName.Tactics, 80.0 );
			SetSkill( SkillName.Wrestling, 80.0 );

			Fame = 3000;
			Karma = -3000;

			VirtualArmor = 35;

			int gems = Utility.RandomMinMax( 1, 4 );

			for ( int i = 0; i < gems; ++i )
				PackGem();

			PackGold( 250, 300 );
			SolenHelper.PackPicnicBasket( this );
			PackItem( new ZoogiFungus( Utility.RandomMinMax( 3, 13 ) ) );

			if ( Utility.RandomDouble() < 0.05 )
				PackItem( new BraceletOfBinding() );
		}

		public override int GetAngerSound()
		{
			return 0xB5;
		}

		public override int GetIdleSound()
		{
			return 0xB5;
		}

		public override int GetAttackSound()
		{
			return 0x289;
		}

		public override int GetHurtSound()
		{
			return 0xBC;
		}

		public override int GetDeathSound()
		{
			return 0xE4;
		}

		public override bool IsEnemy( Mobile m )
		{
			if ( SolenHelper.CheckRedFriendship( m ) )
				return false;
			else
				return base.IsEnemy( m );
		}

		public override void OnDamage( int amount, Mobile from, bool willKill )
		{
			SolenHelper.OnRedDamage( from );

			base.OnDamage( amount, from, willKill );
		}

		public RedSolenWarrior( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}