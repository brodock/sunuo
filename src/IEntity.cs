/***************************************************************************
 *                                IEntity.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: IEntity.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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
	public interface IEntity : IPoint3D
	{
		Serial Serial{ get; }
		Point3D Location{ get; }
		Map Map{ get; }
	}

	public class Entity : IEntity
	{
		private Serial m_Serial;
		private Point3D m_Location;
		private Map m_Map;

		public Entity( Serial serial, Point3D loc, Map map )
		{
			m_Serial = serial;
			m_Location = loc;
			m_Map = map;
		}

		public Serial Serial
		{
			get
			{
				return m_Serial;
			}
		}

		public Point3D Location
		{
			get
			{
				return m_Location;
			}
		}

		public int X
		{
			get
			{
				return m_Location.X;
			}
		}

		public int Y
		{
			get
			{
				return m_Location.Y;
			}
		}

		public int Z
		{
			get
			{
				return m_Location.Z;
			}
		}

		public Map Map
		{
			get
			{
				return m_Map;
			}
		}
	}
}