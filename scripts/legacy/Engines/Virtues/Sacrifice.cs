using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using Server.Network;

namespace Server
{
	public class SacrificeVirtue
	{
		private static TimeSpan GainDelay = TimeSpan.FromDays( 1.0 );
		private static TimeSpan LossDelay = TimeSpan.FromDays( 7.0 );

		public static void Initialize()
		{
			VirtueGump.Register( 110, new OnVirtueUsed( OnVirtueUsed ) );
		}

		public static void OnVirtueUsed( Mobile from )
		{
			if ( from.Alive )
				from.Target = new InternalTarget();
			else
				Resurrect( from );
		}

		public static void CheckAtrophy( Mobile from )
		{
			PlayerMobile pm = from as PlayerMobile;

			if ( pm == null )
				return;

			try
			{
				if ( (pm.LastSacrificeLoss + LossDelay) < DateTime.Now )
				{
					if ( VirtueHelper.Atrophy( from, VirtueName.Sacrifice ) )
						from.SendLocalizedMessage( 1052041 ); // You have lost some Sacrifice.

					VirtueLevel level = VirtueHelper.GetLevel( from, VirtueName.Sacrifice );

					int resurrects = 0;

					if ( level >= VirtueLevel.Knight )
						resurrects = 3;
					else if ( level >= VirtueLevel.Follower )
						resurrects = 2;
					else if ( level >= VirtueLevel.Seeker )
						resurrects = 1;

					pm.AvailableResurrects = resurrects;
					pm.LastSacrificeLoss = DateTime.Now;
				}
			}
			catch
			{
			}
		}

		public static void Resurrect( Mobile from )
		{
			if ( from.Alive )
				return;

			PlayerMobile pm = from as PlayerMobile;

			if ( pm == null )
				return;

			if ( from.Criminal )
			{
				from.SendLocalizedMessage( 1052007 ); // You cannot use this ability while flagged as a criminal.
			}
			else if ( !VirtueHelper.IsSeeker( from, VirtueName.Sacrifice ) )
			{
				from.SendLocalizedMessage( 1052004 ); // You cannot use this ability.
			}
			else if ( pm.AvailableResurrects <= 0 )
			{
				from.SendLocalizedMessage( 1052005 ); // You do not have any resurrections left.
			}
			else
			{
				--pm.AvailableResurrects;

				from.SendGump( new ResurrectGump( from ) );

				Container pack = from.Backpack;
				Container corpse = from.Corpse;

				if ( pack != null && corpse != null )
				{
					ArrayList items = new ArrayList( corpse.Items );

					for ( int i = 0; i < items.Count; ++i )
					{
						Item item = (Item)items[i];

						if ( item.Layer != Layer.Hair && item.Layer != Layer.FacialHair && item.Movable )
							pack.DropItem( item );
					}
				}
			}
		}

		public static void Sacrifice( Mobile from, object targeted )
		{
			if ( !from.CheckAlive() )
				return;

			PlayerMobile pm = from as PlayerMobile;

			if ( pm == null )
				return;

			Mobile targ = targeted as Mobile;

			if ( targ == null )
				return;

			if ( !ValidateCreature( targ ) )
			{
				from.SendLocalizedMessage( 1052014 ); // You cannot sacrifice your fame for that creature.
			}
			else if ( ((targ.Hits * 100) / Math.Max( targ.HitsMax, 1 )) < 85 )
			{
				from.SendLocalizedMessage( 1052013 ); // You cannot sacrifice for this monster because it is too damaged.
			}
			else if ( from.Hidden )
			{
				from.SendLocalizedMessage( 1052015 ); // You cannot do that while hidden.
			}
			else if ( VirtueHelper.IsHighestPath( from, VirtueName.Sacrifice ) )
			{
				from.SendLocalizedMessage( 1052068 ); // You have already attained the highest path in this virtue.
			}
			else if ( from.Fame < 2500 )
			{
				from.SendLocalizedMessage( 1052017 ); // You do not have enough fame to sacrifice.
			}
			else if ( DateTime.Now < (pm.LastSacrificeGain + GainDelay) )
			{
				from.SendLocalizedMessage( 1052016 ); // You must wait approximately one day before sacrificing again.
			}
			else
			{
				int toGain = from.Fame / 2500;

				from.Fame = 0;

				// I have seen the error of my ways!
				targ.PublicOverheadMessage( MessageType.Regular, 0x3B2, 1052009 );

				from.SendLocalizedMessage( 1052010 ); // You have set the creature free.

				Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerCallback( targ.Delete ) );

				pm.LastSacrificeGain = DateTime.Now;

				bool gainedPath = false;

				if ( VirtueHelper.Award( from, VirtueName.Sacrifice, toGain, ref gainedPath ) )
				{
					if ( gainedPath )
					{
						from.SendLocalizedMessage( 1052008 ); // You have gained a path in Sacrifice!

						if ( pm.AvailableResurrects < 3 )
							++pm.AvailableResurrects;
					}
					else
					{
						from.SendLocalizedMessage( 1054160 ); // You have gained in sacrifice.
					}
				}

				from.SendLocalizedMessage( 1052016 ); // You must wait approximately one day before sacrificing again.
			}
		}

		public static bool ValidateCreature( Mobile m )
		{
			if ( m is BaseCreature && (((BaseCreature)m).Controled || ((BaseCreature)m).Summoned) )
				return false;

			// TODO: Enslaved gargoyle, gargoyle enforcer

			return ( m is Lich || m is Succubus || m is Daemon || m is EvilMage );
		}

		private class InternalTarget : Target
		{
			public InternalTarget() : base( 8, false, TargetFlags.None )
			{
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				Sacrifice( from, targeted );
			}
		}
	}
}