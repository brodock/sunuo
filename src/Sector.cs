/***************************************************************************
 *                                 Sector.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Sector.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
 *   $Author: krrios $
 *   $Date: 2005/01/22 04:25:04 $
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using Server.Items;
using Server.Network;

namespace Server
{
	public class Sector
	{
		private int m_X, m_Y;
		private Map m_Owner;
		private ArrayList m_Mobiles;
		private ArrayList m_Items;
		private ArrayList m_Clients;
		private ArrayList m_Multis;
		private ArrayList m_Regions;
		private ArrayList m_Players;
		private bool m_Active;

		private static ArrayList m_DefaultList = new ArrayList();

		public static ArrayList EmptyList
		{
			get{ return m_DefaultList; }
		}

		public Sector( int x, int y, Map owner )
		{
			m_X = x;
			m_Y = y;
			m_Owner = owner;
			m_Active = false;
		}

		public void OnClientChange( NetState oldState, NetState newState )
		{
			if ( m_Clients != null )
				m_Clients.Remove( oldState );

			if ( newState != null )
			{
				if ( m_Clients == null )
					m_Clients = new ArrayList( 4 );

				m_Clients.Add( newState );
			}
		}

		public void OnEnter( Mobile m )
		{
			if ( m_Mobiles == null )
				m_Mobiles = new ArrayList( 4 );

			m_Mobiles.Add( m );

			if ( m.NetState != null )
			{
				if ( m_Clients == null )
					m_Clients = new ArrayList( 4 );

				m_Clients.Add( m.NetState );
			}
	
			if ( m.Player )
			{
				if ( m_Players == null )
					m_Players = new ArrayList( 4 );

				m_Players.Add( m );

				if ( m_Players.Count == 1 )//first player
					Owner.ActivateSectors( m_X, m_Y );
			}
		}

		public void OnEnter( Item item )
		{
			if ( m_Items == null )
				m_Items = new ArrayList();

			m_Items.Add( item );
		}

		public void OnEnter( Region r )
		{
			if ( m_Regions == null || !m_Regions.Contains( r ) )
			{
				if ( m_Regions == null )
					m_Regions = new ArrayList();

				m_Regions.Add( r );
				m_Regions.Sort();

				if ( m_Mobiles != null && m_Mobiles.Count > 0 )
				{
					ArrayList list = new ArrayList( m_Mobiles );

					for ( int i = 0; i < list.Count; ++i )
						((Mobile)list[i]).ForceRegionReEnter( true );
				}
			}
		}

		public void OnLeave( Region r )
		{
			if ( m_Regions != null )
				m_Regions.Remove( r );
		}

		public void OnLeave( Mobile m )
		{
			if ( m_Mobiles != null )
				m_Mobiles.Remove( m );

			if ( m_Clients != null && m.NetState != null )
				m_Clients.Remove( m.NetState );

			if ( m.Player )
			{
				if ( m_Players != null )
					m_Players.Remove( m );

				if ( m_Players == null || m_Players.Count == 0 )
					Owner.DeactivateSectors( m_X, m_Y );
			}
		}

		public void Activate()
		{
			if ( !Active && m_Owner != Map.Internal )//only activate if its the first player in
			{
				for ( int i = 0; m_Items != null && i < m_Items.Count; i++ )
					((Item)m_Items[i]).OnSectorActivate();

				for ( int i = 0; m_Mobiles != null && i < m_Mobiles.Count; i++ )
				{
					Mobile m = (Mobile)m_Mobiles[i];

					if ( !m.Player )
						m.OnSectorActivate();
				}

				m_Active = true;
			}
		}

		public void Deactivate()
		{
			if ( Active && (m_Players == null || m_Players.Count == 0) )//only deactivate if there's really no more players here
			{
				for ( int i = 0; m_Items != null && i < m_Items.Count; i++ )
					((Item)m_Items[i]).OnSectorDeactivate();

				for ( int i = 0; m_Mobiles != null && i < m_Mobiles.Count; i++ )
					((Mobile)m_Mobiles[i]).OnSectorDeactivate();

				m_Active = false;
			}
		}

		public void OnLeave( Item item )
		{
			if ( m_Items != null )
				m_Items.Remove( item );
		}

		public void OnMultiEnter( Item item )
		{
			if ( m_Multis == null )
				m_Multis = new ArrayList( 4 );

			m_Multis.Add( item );
		}

		public void OnMultiLeave( Item item )
		{
			if ( m_Multis != null )
				m_Multis.Remove( item );
		}

		public ArrayList Regions
		{
			get
			{
				if ( m_Regions == null )
					return m_DefaultList;

				return m_Regions;
			}
		}

		public ArrayList Multis
		{
			get
			{
				if ( m_Multis == null )
					return m_DefaultList;

				return m_Multis;
			}
		}

		public ArrayList Mobiles
		{
			get
			{
				if ( m_Mobiles == null )
					return m_DefaultList;

				return m_Mobiles;
			}
		}

		public ArrayList Items
		{
			get
			{
				if ( m_Items == null )
					return m_DefaultList;

				return m_Items;
			}
		}

		public ArrayList Clients
		{
			get
			{
				if ( m_Clients == null )
					return m_DefaultList;

				return m_Clients;
			}
		}

		public ArrayList Players
		{
			get
			{
				if ( m_Players == null )
					return m_DefaultList;

				return m_Players;
			}
		}

		public bool Active 
		{ 
			get
			{ 
				return ( m_Active && m_Owner != Map.Internal ); 
			} 
		}

		public Map Owner
		{
			get
			{
				return m_Owner;
			}
		}

		public int X
		{
			get
			{
				return m_X;
			}
		}

		public int Y
		{
			get
			{
				return m_Y;
			}
		}
	}
}