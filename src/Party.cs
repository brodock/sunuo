/***************************************************************************
 *                                  Party.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Party.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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

namespace Server
{
	public abstract class PartyCommands
	{
		private static PartyCommands m_Handler;

		public static PartyCommands Handler{ get{ return m_Handler; } set{ m_Handler = value; } }

		public abstract void OnAdd( Mobile from );
		public abstract void OnRemove( Mobile from, Mobile target );
		public abstract void OnPrivateMessage( Mobile from, Mobile target, string text );
		public abstract void OnPublicMessage( Mobile from, string text );
		public abstract void OnSetCanLoot( Mobile from, bool canLoot );
		public abstract void OnAccept( Mobile from, Mobile leader );
		public abstract void OnDecline( Mobile from, Mobile leader );
	}
}