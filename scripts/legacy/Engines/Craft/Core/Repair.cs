using System;
using Server;
using Server.Mobiles;
using Server.Targeting;
using Server.Items;

namespace Server.Engines.Craft
{
	public class Repair
	{
		public Repair()
		{
		}

		public static void Do( Mobile from, CraftSystem craftSystem, BaseTool tool )
		{
			from.Target = new InternalTarget( craftSystem, tool );
			from.SendLocalizedMessage( 1044276 ); // Target an item to repair.
		}

		private class InternalTarget : Target
		{
			private CraftSystem m_CraftSystem;
			private BaseTool m_Tool;

			public InternalTarget( CraftSystem craftSystem, BaseTool tool ) :  base ( 2, false, TargetFlags.None )
			{
				m_CraftSystem = craftSystem;
				m_Tool = tool;
			}

			private static void EndGolemRepair( object state )
			{
				((Mobile)state).EndAction( typeof( Golem ) );
			}

			private bool IsSpecialWeapon( BaseWeapon weapon )
			{
				// Weapons repairable but not craftable

				if ( m_CraftSystem is DefTinkering )
				{
					return ( weapon is Cleaver )
						|| ( weapon is Hatchet )
						|| ( weapon is Pickaxe )
						|| ( weapon is ButcherKnife )
						|| ( weapon is SkinningKnife );
				}
				else if ( m_CraftSystem is DefCarpentry )
				{
					return ( weapon is Club )
						|| ( weapon is BlackStaff )
						|| ( weapon is MagicWand );
				}
				else if ( m_CraftSystem is DefBlacksmithy )
				{
					return ( weapon is Pitchfork );
				}

				return false;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				int number;

				if ( m_CraftSystem is DefTinkering && targeted is Golem )
				{
					Golem g = (Golem)targeted;
					int damage = g.HitsMax - g.Hits;

					if ( g.IsDeadBondedPet )
					{
						number = 500426; // You can't repair that.
					}
					else if ( damage <= 0 )
					{
						number = 500423; // That is already in full repair.
					}
					else
					{
						double skillValue = from.Skills[SkillName.Tinkering].Value;

						if ( skillValue < 60.0 )
						{
							number = 1044153; // You don't have the required skills to attempt this item.
						}
						else if ( !from.CanBeginAction( typeof( Golem ) ) )
						{
							number = 501789; // You must wait before trying again.
						}
						else
						{
							if ( damage > (int)(skillValue * 0.3) )
								damage = (int)(skillValue * 0.3);

							damage += 30;

							if ( !from.CheckSkill( SkillName.Tinkering, 0.0, 100.0 ) )
								damage /= 2;

							Container pack = from.Backpack;

							if ( pack != null )
							{
								int v = pack.ConsumeUpTo( typeof( IronIngot ), (damage+4)/5 );

								if ( v > 0 )
								{
									g.Hits += v*5;

									number = 1044279; // You repair the item.

									from.BeginAction( typeof( Golem ) );
									Timer.DelayCall( TimeSpan.FromSeconds( 12.0 ), new TimerStateCallback( EndGolemRepair ), from );
								}
								else
								{
									number = 1044037; // You do not have sufficient metal to make that.
								}
							}
							else
							{
								number = 1044037; // You do not have sufficient metal to make that.
							}
						}
					}
				}
				else if ( targeted is BaseWeapon )
				{
					BaseWeapon weapon = (BaseWeapon)targeted;
					SkillName skill = m_CraftSystem.MainSkill;
					int toWeaken = 0;

					if ( Core.AOS )
					{
						toWeaken = 1;
					}
					else if ( skill != SkillName.Tailoring )
					{
						double skillLevel = from.Skills[skill].Base;

						if ( skillLevel >= 90.0 )
							toWeaken = 1;
						else if ( skillLevel >= 70.0 )
							toWeaken = 2;
						else
							toWeaken = 3;
					}

					if ( m_CraftSystem.CraftItems.SearchForSubclass( weapon.GetType() ) == null && !IsSpecialWeapon( weapon ) )
					{
						number = 1044277; // That item cannot be repaired.
					}
					else if ( !weapon.IsChildOf( from.Backpack ) )
					{
						number = 1044275; // The item must be in your backpack to repair it.
					}
					else if ( weapon.MaxHits <= 0 || weapon.Hits == weapon.MaxHits )
					{
						number = 1044281; // That item is in full repair
					}
					else if ( weapon.MaxHits <= toWeaken )
					{
						number = 500424; // You destroyed the item.
						m_CraftSystem.PlayCraftEffect( from );
						weapon.Delete();
					}
					else if ( from.CheckSkill( skill, -285.0, 100.0 ) )
					{
						number = 1044279; // You repair the item.
						m_CraftSystem.PlayCraftEffect( from );
						weapon.MaxHits -= toWeaken;
						weapon.Hits = weapon.MaxHits;
					}
					else
					{
						number = 1044280; // You fail to repair the item.
						m_CraftSystem.PlayCraftEffect( from );
						weapon.MaxHits -= toWeaken;

						if ( weapon.Hits - toWeaken < 0 )
							weapon.Hits = 0;
						else
							weapon.Hits -= toWeaken;
					}

					if ( weapon.MaxHits <= toWeaken )
						from.SendLocalizedMessage( 1044278 ); // That item has been repaired many times, and will break if repairs are attempted again.
				}
				else if ( targeted is BaseArmor )
				{
					BaseArmor armor = (BaseArmor)targeted;
					SkillName skill = m_CraftSystem.MainSkill;
					int toWeaken = 0;

					if ( Core.AOS )
					{
						toWeaken = 1;
					}
					else if ( skill != SkillName.Tailoring )
					{
						double skillLevel = from.Skills[skill].Base;

						if ( skillLevel >= 90.0 )
							toWeaken = 1;
						else if ( skillLevel >= 70.0 )
							toWeaken = 2;
						else
							toWeaken = 3;
					}

					if ( m_CraftSystem.CraftItems.SearchForSubclass( armor.GetType() ) == null )
					{
						number = 1044277; // That item cannot be repaired.
					}
					else if ( !armor.IsChildOf( from.Backpack ) )
					{
						number = 1044275; // The item must be in your backpack to repair it.
					}
					else if ( armor.MaxHitPoints <= 0 || armor.HitPoints == armor.MaxHitPoints )
					{
						number = 1044281; // That item is in full repair
					}
					else if ( armor.MaxHitPoints <= toWeaken )
					{
						number = 500424; // You destroyed the item.
						m_CraftSystem.PlayCraftEffect( from );
						armor.Delete();
					}
					else if ( from.CheckSkill( skill, -285.0, 100.0 ) )
					{
						number = 1044279; // You repair the item.
						m_CraftSystem.PlayCraftEffect( from );
						armor.MaxHitPoints -= toWeaken;
						armor.HitPoints = armor.MaxHitPoints;
					}
					else
					{
						number = 1044280; // You fail to repair the item.
						m_CraftSystem.PlayCraftEffect( from );
						armor.MaxHitPoints -= toWeaken;

						if ( armor.HitPoints - toWeaken < 0 )
							armor.HitPoints = 0;
						else
							armor.HitPoints -= toWeaken;
					}

					if ( armor.MaxHitPoints <= toWeaken )
						from.SendLocalizedMessage( 1044278 ); // That item has been repaired many times, and will break if repairs are attempted again.
				}
				else if ( targeted is Item )
				{
					number = 1044277; // That item cannot be repaired.
				}
				else
				{
					number = 500426; // You can't repair that.
				}

				CraftContext context = m_CraftSystem.GetContext( from );

				from.SendGump( new CraftGump( from, m_CraftSystem, m_Tool, number ) );
			}
		}
	}
}