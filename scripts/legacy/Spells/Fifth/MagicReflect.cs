using System;
using System.Collections;
using Server;
using Server.Targeting;
using Server.Network;

namespace Server.Spells.Fifth
{
	public class MagicReflectSpell : Spell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Magic Reflection", "In Jux Sanct",
				SpellCircle.Fifth,
				242,
				9012,
				Reagent.Garlic,
				Reagent.MandrakeRoot,
				Reagent.SpidersSilk
			);

		public MagicReflectSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			if ( Core.AOS )
				return true;

			if ( Caster.MagicDamageAbsorb > 0 )
			{
				Caster.SendLocalizedMessage( 1005559 ); // This spell is already in effect.
				return false;
			}
			else if ( !Caster.CanBeginAction( typeof( DefensiveSpell ) ) )
			{
				Caster.SendLocalizedMessage( 1005385 ); // The spell will not adhere to you at this time.
				return false;
			}

			return true;
		}

		private static Hashtable m_Table = new Hashtable();

		public override void OnCast()
		{
			if ( Core.AOS )
			{
				/* The magic reflection spell decreases the caster's physical resistance, while increasing the caster's elemental resistances.
				 * Physical decrease = 25 - (Inscription/20).
				 * Elemental resistance = +10 (-20 physical, +10 elemental at GM Inscription)
				 * The magic reflection spell has an indefinite duration, becoming active when cast, and deactivated when re-cast.
				 * Reactive Armor, Protection, and Magic Reflection will stay on—even after logging out, even after dying—until you “turn them off” by casting them again. 
				 */

				if ( CheckSequence() )
				{
					Mobile targ = Caster;

					ResistanceMod[] mods = (ResistanceMod[])m_Table[targ];

					if ( mods == null )
					{
						targ.PlaySound( 0x1E9 );
						targ.FixedParticles( 0x375A, 10, 15, 5037, EffectLayer.Waist );

						mods = new ResistanceMod[5]
							{
								new ResistanceMod( ResistanceType.Physical, -25 + (int)(targ.Skills[SkillName.Inscribe].Value / 20) ),
								new ResistanceMod( ResistanceType.Fire, 10 ),
								new ResistanceMod( ResistanceType.Cold, 10 ),
								new ResistanceMod( ResistanceType.Poison, 10 ),
								new ResistanceMod( ResistanceType.Energy, 10 )
							};

						m_Table[targ] = mods;

						for ( int i = 0; i < mods.Length; ++i )
							targ.AddResistanceMod( mods[i] );
					}
					else
					{
						targ.PlaySound( 0x1ED );
						targ.FixedParticles( 0x375A, 10, 15, 5037, EffectLayer.Waist );

						m_Table.Remove( targ );

						for ( int i = 0; i < mods.Length; ++i )
							targ.RemoveResistanceMod( mods[i] );
					}
				}

				FinishSequence();
			}
			else
			{
				if ( Caster.MagicDamageAbsorb > 0 )
				{
					Caster.SendLocalizedMessage( 1005559 ); // This spell is already in effect.
				}
				else if ( !Caster.CanBeginAction( typeof( DefensiveSpell ) ) )
				{
					Caster.SendLocalizedMessage( 1005385 ); // The spell will not adhere to you at this time.
				}
				else if ( CheckSequence() )
				{
					if ( Caster.BeginAction( typeof( DefensiveSpell ) ) )
					{
						int value = (int)(Caster.Skills[SkillName.Magery].Value + Caster.Skills[SkillName.Inscribe].Value);
						value = (int)(8 + (value/200)*7.0);//absorb from 8 to 15 "circles"

						Caster.MagicDamageAbsorb = value;

						Caster.FixedParticles( 0x375A, 10, 15, 5037, EffectLayer.Waist );
						Caster.PlaySound( 0x1E9 );
					}
					else
					{
						Caster.SendLocalizedMessage( 1005385 ); // The spell will not adhere to you at this time.
					}
				}

				FinishSequence();
			}
		}
	}
}
