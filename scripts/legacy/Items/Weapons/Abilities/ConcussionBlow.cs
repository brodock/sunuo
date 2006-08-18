using System;

namespace Server.Items
{
	/// <summary>
	/// This devastating strike is most effective against those who are in good health and whose reserves of mana are low, or vice versa.
	/// </summary>
	public class ConcussionBlow : WeaponAbility
	{
		public ConcussionBlow()
		{
		}

		public override int BaseMana{ get{ return 25; } }

		public override void OnHit( Mobile attacker, Mobile defender, int damage )
		{
			if ( !Validate( attacker ) || !CheckMana( attacker, true ) )
				return;

			ClearCurrentAbility( attacker );

			attacker.SendLocalizedMessage( 1060165 ); // You have delivered a concussion!
			defender.SendLocalizedMessage( 1060166 ); // You feel disoriented!

			defender.PlaySound( 0x213 );
			defender.FixedParticles( 0x377A, 1, 32, 9949, 1153, 0, EffectLayer.Head );

			Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( defender.X, defender.Y, defender.Z + 10 ), defender.Map ), new Entity( Serial.Zero, new Point3D( defender.X, defender.Y, defender.Z + 20 ), defender.Map ), 0x36FE, 1, 0, false, false, 1133, 3, 9501, 1, 0, EffectLayer.Waist, 0x100 );

			AOS.Damage( defender, attacker, Utility.RandomMinMax( 10, 40 ), 100, 0, 0, 0, 0 );
		}
	}
}