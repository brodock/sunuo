using System;
using Server;
using System.Collections;
using Server.Items;

namespace Server.Guilds
{
	public class Guild : BaseGuild
	{
		public static void Configure()
		{
			EventSink.CreateGuild += new CreateGuildHandler( EventSink_CreateGuild );
			EventSink.GuildGumpRequest += new GuildGumpRequestHandler( EventSink_GuildGumpRequest );
		}

		public static void EventSink_GuildGumpRequest( GuildGumpRequestArgs args )
		{
			if( !Core.SE )
				return;

			args.Mobile.SendMessage( "The new Guild system is not implemented yet" );
		}

		public static BaseGuild EventSink_CreateGuild( CreateGuildEventArgs args )
		{
			return (BaseGuild)(new Guild( args.Id ));
		}

		private Mobile m_Leader;

		private string m_Name;
		private string m_Abbreviation;

		private ArrayList m_Allies;
		private ArrayList m_Enemies;

		private ArrayList m_Members;

		private Item m_Guildstone;
		private Item m_Teleporter;

		private string m_Charter;
		private string m_Website;

		private DateTime m_LastFealty;

		private GuildType m_Type;
		private DateTime m_TypeLastChange;

		private ArrayList m_AllyDeclarations, m_AllyInvitations;

		private ArrayList m_WarDeclarations, m_WarInvitations;
		private ArrayList m_Candidates, m_Accepted;

		public Guild( Mobile leader, string name, string abbreviation )
		{
			m_Leader = leader;

			m_Members = new ArrayList();
			m_Allies = new ArrayList();
			m_Enemies = new ArrayList();
			m_WarDeclarations = new ArrayList();
			m_WarInvitations = new ArrayList();
			m_AllyDeclarations = new ArrayList();
			m_AllyInvitations = new ArrayList();
			m_Candidates = new ArrayList();
			m_Accepted = new ArrayList();

			m_LastFealty = DateTime.Now;

			m_Name = name;
			m_Abbreviation = abbreviation;

			m_TypeLastChange = DateTime.MinValue;

			AddMember( m_Leader );
		}

		public Guild( int id ) : base( id )//serialization ctor
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			if ( this.LastFealty+TimeSpan.FromDays( 1.0 ) < DateTime.Now )
				this.CalculateGuildmaster();
			
			writer.Write( (int) 4 );//version

			writer.WriteGuildList( m_AllyDeclarations, true );
			writer.WriteGuildList( m_AllyInvitations, true );

			writer.Write( m_TypeLastChange );

			writer.Write( (int)m_Type );

			writer.Write( m_LastFealty );

			writer.Write( m_Leader );
			writer.Write( m_Name );
			writer.Write( m_Abbreviation );

			writer.WriteGuildList( m_Allies, true );
			writer.WriteGuildList( m_Enemies, true );
			writer.WriteGuildList( m_WarDeclarations, true );
			writer.WriteGuildList( m_WarInvitations, true );

			writer.WriteMobileList( m_Members, true );
			writer.WriteMobileList( m_Candidates, true );
			writer.WriteMobileList( m_Accepted, true );

			writer.Write( m_Guildstone );
			writer.Write( m_Teleporter );

			writer.Write( m_Charter );
			writer.Write( m_Website );
		}

		public override void Deserialize( GenericReader reader )
		{
			int version = reader.ReadInt();

			switch ( version )
			{
				case 4:
				{
					m_AllyDeclarations = reader.ReadGuildList();
					m_AllyInvitations = reader.ReadGuildList();

					goto case 3;
				}
				case 3:
				{
					m_TypeLastChange = reader.ReadDateTime();

					goto case 2;
				}
				case 2:
				{
					m_Type = (GuildType)reader.ReadInt();

					goto case 1;
				}
				case 1:
				{
					m_LastFealty = reader.ReadDateTime();

					goto case 0;
				}
				case 0:
				{
					m_Leader = reader.ReadMobile();
					m_Name = reader.ReadString();
					m_Abbreviation = reader.ReadString();

					m_Allies = reader.ReadGuildList();
					m_Enemies = reader.ReadGuildList();
					m_WarDeclarations = reader.ReadGuildList();
					m_WarInvitations = reader.ReadGuildList();

					m_Members = reader.ReadMobileList();
					m_Candidates = reader.ReadMobileList();
					m_Accepted = reader.ReadMobileList(); 

					m_Guildstone = reader.ReadItem();
					m_Teleporter = reader.ReadItem();

					m_Charter = reader.ReadString();
					m_Website = reader.ReadString();

					break;
				}
			}

			if ( m_AllyDeclarations == null )
				m_AllyDeclarations = new ArrayList();

			if ( m_AllyInvitations == null )
				m_AllyInvitations = new ArrayList();

			if ( m_Guildstone == null || m_Members.Count == 0 )
				Disband(); 
		}

		public void AddMember( Mobile m )
		{
			if ( !m_Members.Contains( m ) )
			{
				if ( m.Guild != null && m.Guild != this )
					((Guild)m.Guild).RemoveMember( m );

				m_Members.Add( m );
				m.Guild = this;
				m.GuildFealty = m_Leader;
			}
		}

		public void RemoveMember( Mobile m )
		{
			if ( m_Members.Contains( m ) )
			{
				m_Members.Remove( m );
				m.Guild = null;

				m.SendLocalizedMessage( 1018028 ); // You have been dismissed from your guild.

				if ( m == m_Leader )
				{
					CalculateGuildmaster();

					if ( m_Leader == null )
						Disband();
				}

				if ( m_Members.Count == 0 )
					Disband();
			}
		}

		public void AddAlly( Guild g )
		{
			if ( !m_Allies.Contains( g ) )
			{
				m_Allies.Add( g );

				g.AddAlly( this );
			}
		}

		public void RemoveAlly( Guild g )
		{
			if ( m_Allies.Contains( g ) )
			{
				m_Allies.Remove( g );

				g.RemoveAlly( this );
			}
		}

		public void AddEnemy( Guild g )
		{
			if ( !m_Enemies.Contains( g ) )
			{
				m_Enemies.Add( g );

				g.AddEnemy( this );
			}
		}

		public void RemoveEnemy( Guild g )
		{
			if ( m_Enemies != null && m_Enemies.Contains( g ) )
			{
				m_Enemies.Remove( g );

				g.RemoveEnemy( this );
			}
		}

		public void GuildMessage( int num, string format, params object[] args )
		{
			GuildMessage( num, String.Format( format, args ) );
		}

		public void GuildMessage( int num, string append )
		{
			for ( int i = 0; i < m_Members.Count; ++i )
				((Mobile)m_Members[i]).SendLocalizedMessage( num, true, append );
		}

		public void Disband()
		{
			m_Leader = null;

			BaseGuild.List.Remove( this.Id );

			foreach ( Mobile m in m_Members )
			{
				m.SendLocalizedMessage( 502131 ); // Your guild has disbanded.
				m.Guild = null;
			}

			m_Members.Clear();

			for ( int i = m_Allies.Count - 1; i >= 0; --i )
				if ( i < m_Allies.Count )
					RemoveAlly( (Guild) m_Allies[i] );

			for ( int i = m_Enemies.Count - 1; i >= 0; --i )
				if ( i < m_Enemies.Count )
					RemoveEnemy( (Guild) m_Enemies[i] );

			if ( m_Guildstone != null )
			{
				m_Guildstone.Delete();
				m_Guildstone = null;
			}
		}

		public void CalculateGuildmaster()
		{
			Hashtable votes = new Hashtable();

			for ( int i = 0; m_Members != null && i < m_Members.Count; ++i )
			{
				Mobile memb = (Mobile)m_Members[i];

				if ( memb == null || memb.Deleted || memb.Guild != this )
					continue;

				Mobile m = ((Mobile)m_Members[i]).GuildFealty;

				if ( m == null || m.Deleted || m.Guild != this )
				{
					if ( m_Leader != null && !m_Leader.Deleted && m_Leader.Guild == this )
						m = m_Leader;
					else 
						m = memb;
				}

				if ( m == null )
					continue;

				if ( votes[m] == null )
					votes[m] = (int)1;
				else
					votes[m] = (int)(votes[m]) + 1;
			}

			Mobile winner = null;
			int highVotes = 0;

			foreach ( DictionaryEntry de in votes )
			{
				Mobile m = (Mobile)de.Key;
				int val = (int)de.Value;

				if ( winner == null || val > highVotes )
				{
					winner = m;
					highVotes = val;
				}
			}

			if ( m_Leader != winner && winner != null )
				GuildMessage( 1018015, winner.Name ); // Guild Message: Guildmaster changed to:

			m_Leader = winner;
			m_LastFealty = DateTime.Now;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Item Guildstone
		{
			get
			{
				return m_Guildstone;
			}
			set
			{
				m_Guildstone = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Item Teleporter
		{
			get
			{
				return m_Teleporter;
			}
			set
			{
				m_Teleporter = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public override string Name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;

				if ( m_Guildstone != null )
					m_Guildstone.InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string Website
		{
			get
			{
				return m_Website;
			}
			set
			{
				m_Website = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public override string Abbreviation
		{
			get
			{
				return m_Abbreviation;
			}
			set
			{
				m_Abbreviation = value;

				InvalidateMemberProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string Charter
		{
			get
			{
				return m_Charter;
			}
			set
			{
				m_Charter = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public override GuildType Type
		{
			get
			{
				return m_Type;
			}
			set
			{
				if ( m_Type != value )
				{
					m_Type = value;
					m_TypeLastChange = DateTime.Now;

					InvalidateMemberProperties();
				}
			}
		}

		public void InvalidateMemberProperties()
		{
			if ( m_Members != null )
			{
				for (int i=0;i<m_Members.Count;i++)
					((Mobile)m_Members[i]).InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Leader
		{
			get
			{
				if ( m_Leader == null || m_Leader.Deleted || m_Leader.Guild != this )
					CalculateGuildmaster();

				return m_Leader;
			}
			set
			{
				m_Leader = value;
			}
		}

		public override bool Disbanded
		{
			get
			{
				return ( m_Leader == null || m_Leader.Deleted );
			}
		}

		public ArrayList Allies
		{
			get
			{
				return m_Allies;
			}
		}

		public ArrayList Enemies
		{
			get
			{
				return m_Enemies;
			}
		}

		public ArrayList AllyDeclarations
		{
			get
			{
				return m_AllyDeclarations;
			}
		}

		public ArrayList AllyInvitations
		{
			get
			{
				return m_AllyInvitations;
			}
		}

		public ArrayList WarDeclarations
		{
			get
			{
				return m_WarDeclarations;
			}
		}

		public ArrayList WarInvitations
		{
			get
			{
				return m_WarInvitations;
			}
		}

		public ArrayList Candidates
		{
			get
			{
				return m_Candidates;
			}
		}

		public ArrayList Accepted
		{
			get
			{
				return m_Accepted;
			}
		}

		public ArrayList Members
		{
			get
			{
				return m_Members;
			}
		}

		public bool IsMember( Mobile m )
		{
			return m_Members.Contains( m );
		}

		public bool IsAlly( Guild g )
		{
			return m_Allies.Contains( g );
		}

		public bool IsEnemy( Guild g )
		{
			if ( m_Type != GuildType.Regular && g.m_Type != GuildType.Regular && m_Type != g.m_Type )
				return true;

			return m_Enemies.Contains( g );
		}

		public bool IsWar( Guild g )
		{
			return m_Enemies.Contains( g );
		}

		public override void OnDelete( Mobile mob )
		{
			RemoveMember( mob );
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime LastFealty
		{
			get
			{
				return m_LastFealty;
			}
			set
			{
				m_LastFealty = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime TypeLastChange
		{
			get
			{
				return m_TypeLastChange;
			}
		}
	}
}
