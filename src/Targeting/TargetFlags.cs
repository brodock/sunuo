/***************************************************************************
 *                              TargetFlags.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: TargetFlags.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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

namespace Server.Targeting
{
	public enum TargetFlags : byte
	{
		None = 0x00,
		Harmful = 0x01,
		Beneficial = 0x02,
	}
}