using System;
using System.IO;
using Server.Gumps;
using Server.Guilds;
using Server.Network;
using Server.Factions;

namespace Server.Items
{
	public class Guildstone : Item
	{
		private Guild m_Guild;

		public Guild Guild
		{
			get
			{
				return m_Guild;
			}
		}

		public override int LabelNumber{ get{ return 1041429; } } // a guildstone

		public Guildstone( Guild g ) : base( 0xED4 )
		{
			m_Guild = g;

			Movable = false;
		}

		public Guildstone( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( m_Guild );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_Guild = reader.ReadGuild() as Guild;

					goto case 0;
				}
				case 0:
				{
					break;
				}
			}

			if ( m_Guild == null )
				this.Delete();
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Guild != null )
			{
				string name;

				if ( (name = m_Guild.Name) == null || (name = name.Trim()).Length <= 0 )
					name = "(unnamed)";

				list.Add( 1060802, Utility.FixHtml( name ) ); // Guild name: ~1_val~
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			string name;

			if ( m_Guild == null )
				name = "(unfounded)";
			else if ( (name = m_Guild.Name) == null || (name = name.Trim()).Length <= 0 )
				name = "(unnamed)";

			this.LabelTo( from, name );
		}

		public override void OnAfterDelete()
		{
			if ( m_Guild != null && !m_Guild.Disbanded )
				m_Guild.Disband();
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_Guild == null || m_Guild.Disbanded )
			{
				Delete();
			}
			else if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.SendLocalizedMessage( 500446 ); // That is too far away.
			}
			else if ( m_Guild.Accepted.Contains( from ) )
			{
				#region Factions
				PlayerState guildState = PlayerState.Find( m_Guild.Leader );
				PlayerState targetState = PlayerState.Find( from );

				Faction guildFaction = ( guildState == null ? null : guildState.Faction );
				Faction targetFaction = ( targetState == null ? null : targetState.Faction );
				
				if ( guildFaction != targetFaction || (targetState != null && targetState.IsLeaving) )
					return;

				if ( guildState != null && targetState != null )
					targetState.Leaving = guildState.Leaving;
				#endregion

				m_Guild.Accepted.Remove( from );
				m_Guild.AddMember( from );

				GuildGump.EnsureClosed( from );
				from.SendGump( new GuildGump( from, m_Guild ) );
			}
			else if ( from.AccessLevel < AccessLevel.GameMaster && !m_Guild.IsMember( from ) )
			{
				from.Send( new MessageLocalized( Serial, ItemID, MessageType.Regular, 0x3B2, 3, 501158, "", "" ) ); // You are not a member ...
			}
			else
			{
				GuildGump.EnsureClosed( from );
				from.SendGump( new GuildGump( from, m_Guild ) );
			}
		}
	}
}
