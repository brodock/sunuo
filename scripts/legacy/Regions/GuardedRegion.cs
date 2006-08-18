using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class GuardedRegion : Region
	{
		private static object[] m_GuardParams = new object[1];
		private Type m_GuardType;
		private bool m_Disabled;

		public bool Disabled{ get{ return m_Disabled; } set{ m_Disabled = value; } }

		public virtual bool IsDisabled()
		{
			return m_Disabled;
		}

		public static void Initialize()
		{
			Commands.Register( "CheckGuarded", AccessLevel.GameMaster, new CommandEventHandler( CheckGuarded_OnCommand ) );
			Commands.Register( "SetGuarded", AccessLevel.Administrator, new CommandEventHandler( SetGuarded_OnCommand ) );
			Commands.Register( "ToggleGuarded", AccessLevel.Administrator, new CommandEventHandler( ToggleGuarded_OnCommand ) );
		}

		[Usage( "CheckGuarded" )]
		[Description( "Returns a value indicating if the current region is guarded or not." )]
		private static void CheckGuarded_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			GuardedRegion reg = from.Region as GuardedRegion;

			if ( reg == null )
				from.SendMessage( "You are not in a guardable region." );
			else if ( reg.Disabled )
				from.SendMessage( "The guards in this region have been disabled." );
			else
				from.SendMessage( "This region is actively guarded." );
		}

		[Usage( "SetGuarded <true|false>" )]
		[Description( "Enables or disables guards for the current region." )]
		private static void SetGuarded_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;

			if ( e.Length == 1 )
			{
				GuardedRegion reg = from.Region as GuardedRegion;

				if ( reg == null )
				{
					from.SendMessage( "You are not in a guardable region." );
				}
				else
				{
					reg.Disabled = !e.GetBoolean( 0 );

					if ( reg.Disabled )
						from.SendMessage( "The guards in this region have been disabled." );
					else
						from.SendMessage( "The guards in this region have been enabled." );
				}
			}
			else
			{
				from.SendMessage( "Format: SetGuarded <true|false>" );
			}
		}

		[Usage( "ToggleGuarded" )]
		[Description( "Toggles the state of guards for the current region." )]
		private static void ToggleGuarded_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			GuardedRegion reg = from.Region as GuardedRegion;

			if ( reg == null )
			{
				from.SendMessage( "You are not in a guardable region." );
			}
			else
			{
				reg.Disabled = !reg.Disabled;

				if ( reg.Disabled )
					from.SendMessage( "The guards in this region have been disabled." );
				else
					from.SendMessage( "The guards in this region have been enabled." );
			}
		}

		public static GuardedRegion Disable( GuardedRegion reg )
		{
			reg.Disabled = true;
			return reg;
		}

		public virtual bool AllowReds{ get{ return Core.AOS; } }

		public virtual bool CheckVendorAccess( BaseVendor vendor, Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.GameMaster || IsDisabled() )
				return true;

			return ( from.Kills < 5 );
		}

		public GuardedRegion( string prefix, string name, Map map, Type guardType ) : base( prefix, name, map )
		{
			m_GuardType = guardType;
		}

		public override bool OnBeginSpellCast( Mobile m, ISpell s )
		{
			if ( !IsDisabled() && !s.OnCastInTown( this ) )
			{
				m.SendLocalizedMessage( 500946 ); // You cannot cast this in town!
				return false;
			}

			return base.OnBeginSpellCast( m, s );
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}

		public override void MakeGuard( Mobile focus )
		{
			BaseGuard useGuard = null;

			foreach ( Mobile m in focus.GetMobilesInRange( 8 ) )
			{
				if ( m is BaseGuard )
				{
					BaseGuard g = (BaseGuard)m;

					if ( g.Focus == null ) // idling
					{
						useGuard = g;
						break;
					}
				}
			}

			if ( useGuard != null )
			{
				useGuard.Focus = focus;
			}
			else
			{
				m_GuardParams[0] = focus;

				Activator.CreateInstance( m_GuardType, m_GuardParams );
			}
		}

		public override void OnEnter( Mobile m )
		{
			if ( IsDisabled() )
				return;

			//m.SendLocalizedMessage( 500112 ); // You are now under the protection of the town guards.

			if ( !AllowReds && m.Kills >= 5 )
				CheckGuardCandidate( m );
		}

		public override void OnExit( Mobile m )
		{
			if ( IsDisabled() )
				return;

			//m.SendLocalizedMessage( 500113 ); // You have left the protection of the town guards.
		}

		public override void OnSpeech( SpeechEventArgs args )
		{
			if ( IsDisabled() )
				return;

			if ( args.Mobile.Alive && args.HasKeyword( 0x0007 ) ) // *guards*
				CallGuards( args.Mobile.Location );
		}

		public override void OnAggressed( Mobile aggressor, Mobile aggressed, bool criminal )
		{
			base.OnAggressed( aggressor, aggressed, criminal );

			if ( !IsDisabled() && aggressor != aggressed && criminal )
				CheckGuardCandidate( aggressor );
		}

		public override void OnGotBenificialAction( Mobile helper, Mobile helped )
		{
			base.OnGotBenificialAction( helper, helped );

			if ( IsDisabled() )
				return;

			int noto = Notoriety.Compute( helper, helped );

			if ( helper != helped && (noto == Notoriety.Criminal || noto == Notoriety.Murderer) )
				CheckGuardCandidate( helper );
		}

		public override void OnCriminalAction( Mobile m, bool message )
		{
			base.OnCriminalAction( m, message );

			if ( !IsDisabled() )
				CheckGuardCandidate( m );
		}

		private Hashtable m_GuardCandidates = new Hashtable();

		public void CheckGuardCandidate( Mobile m )
		{
			if ( IsDisabled() )
				return;

			if ( IsGuardCandidate( m ) )
			{
				GuardTimer timer = (GuardTimer)m_GuardCandidates[m];

				if ( timer == null )
				{
					timer = new GuardTimer( m, m_GuardCandidates );
					timer.Start();

					m_GuardCandidates[m] = timer;
					m.SendLocalizedMessage( 502275 ); // Guards can now be called on you!

					Map map = m.Map;

					if ( map != null )
					{
						Mobile fakeCall = null;
						double prio = 0.0;

						foreach ( Mobile v in m.GetMobilesInRange( 8 ) )
						{
							if ( !v.Player && v.Body.IsHuman && v != m && !IsGuardCandidate( v ) )
							{
								double dist = m.GetDistanceToSqrt( v );

								if ( fakeCall == null || dist < prio )
								{
									fakeCall = v;
									prio = dist;
								}
							}
						}

						if ( fakeCall != null )
						{
							fakeCall.Say( Utility.RandomList( 1007037, 501603, 1013037, 1013038, 1013039, 1013041, 1013042, 1013043, 1013052 ) );
							MakeGuard( m );
							m_GuardCandidates.Remove( m );
							m.SendLocalizedMessage( 502276 ); // Guards can no longer be called on you.
						}
					}
				}
				else
				{
					timer.Stop();
					timer.Start();
				}
			}
		}

		public void CallGuards( Point3D p )
		{
			if ( IsDisabled() )
				return;

			IPooledEnumerable eable = Map.GetMobilesInRange( p, 14 );

			foreach ( Mobile m in eable )
			{
				if ( IsGuardCandidate( m ) && ((!AllowReds && m.Kills >= 5 && Mobiles.Contains( m )) || m_GuardCandidates.Contains( m )) )
				{
					MakeGuard( m );
					m_GuardCandidates.Remove( m );
					m.SendLocalizedMessage( 502276 ); // Guards can no longer be called on you.
					break;
				}
			}

			eable.Free();
		}

		public bool IsGuardCandidate( Mobile m )
		{
			if ( m is BaseGuard || !m.Alive || m.AccessLevel > AccessLevel.Player || m.Blessed || IsDisabled() )
				return false;

			return (!AllowReds && m.Kills >= 5) || m.Criminal;
		}

		private class GuardTimer : Timer
		{
			private Mobile m_Mobile;
			private Hashtable m_Table;

			public GuardTimer( Mobile m, Hashtable table ) : base( TimeSpan.FromSeconds( 15.0 ) )
			{
				Priority = TimerPriority.TwoFiftyMS;

				m_Mobile = m;
				m_Table = table;
			}

			protected override void OnTick()
			{
				if ( m_Table.Contains( m_Mobile ) )
				{
					m_Table.Remove( m_Mobile );
					m_Mobile.SendLocalizedMessage( 502276 ); // Guards can no longer be called on you.
				}
			}
		}
	}
}