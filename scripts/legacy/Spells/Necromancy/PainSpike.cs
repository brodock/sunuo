using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class PainSpikeSpell : NecromancerSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Pain Spike", "In Sar",
				SpellCircle.Second,
				203,
				9031,
				Reagent.GraveDust,
				Reagent.PigIron
			);

		public override double RequiredSkill{ get{ return 20.0; } }
		public override int RequiredMana{ get{ return 5; } }

		public PainSpikeSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public override bool DelayedDamage{ get{ return false; } }

		public void Target( Mobile m )
		{
			if ( CheckHSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				SpellHelper.CheckReflect( (int)this.Circle, Caster, ref m );

				/* Temporarily causes intense physical pain to the target, dealing direct damage.
				 * After 10 seconds the spell wears off, and if the target is still alive, some of the Hit Points lost through Pain Spike are restored.
				 * Damage done is ((Spirit Speak skill level - target's Resist Magic skill level) / 100) + 30.
				 * 
				 * NOTE : Above algorithm must either be typo or using fixed point : really is / 10 :
				 * 
				 * 100ss-0mr = 40
				 * 100ss-50mr = 35
				 * 100ss-75mr = 32
				 * 
				 * NOTE : If target already has a pain spike in effect, damage dealt /= 10
				 */

				m.FixedParticles( 0x37C4, 1, 8, 9916, 39, 3, EffectLayer.Head );
				m.FixedParticles( 0x37C4, 1, 8, 9502, 39, 4, EffectLayer.Head );
				m.PlaySound( 0x210 );

				double damage = ((GetDamageSkill( Caster ) - GetResistSkill( m )) / 10) + (m.Player ? 18 : 30);

				if ( damage < 1 )
					damage = 1;

				if ( m_Table.Contains( m ) )
					damage /= 10;
				else
					new InternalTimer( m, damage ).Start();

				Misc.WeightOverloading.DFA = Misc.DFAlgorithm.PainSpike;
				m.Damage( (int) damage, Caster );
				Misc.WeightOverloading.DFA = Misc.DFAlgorithm.Standard;

				//SpellHelper.Damage( this, m, damage, 100, 0, 0, 0, 0, Misc.DFAlgorithm.PainSpike );
			}

			FinishSequence();
		}

		private static Hashtable m_Table = new Hashtable();

		private class InternalTimer : Timer
		{
			private Mobile m_Mobile;
			private int m_ToRestore;

			public InternalTimer( Mobile m, double toRestore ) : base( TimeSpan.FromSeconds( 10.0 ) )
			{
				Priority = TimerPriority.OneSecond;

				m_Mobile = m;
				m_ToRestore = (int)toRestore;

				m_Table[m] = this;
			}

			protected override void OnTick()
			{
				m_Table.Remove( m_Mobile );

				if ( m_Mobile.Alive && !m_Mobile.IsDeadBondedPet )
					m_Mobile.Hits += m_ToRestore;
			}
		}

		private class InternalTarget : Target
		{
			private PainSpikeSpell m_Owner;

			public InternalTarget( PainSpikeSpell owner ) : base( 12, false, TargetFlags.Harmful )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
					m_Owner.Target( (Mobile) o );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}