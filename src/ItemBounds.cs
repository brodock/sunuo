/***************************************************************************
 *                               ItemBounds.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: ItemBounds.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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
using System.IO;

namespace Server
{
	public class ItemBounds
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static Rectangle2D[] m_Bounds;

		public static Rectangle2D[] Table
		{
			get
			{
				return m_Bounds;
			}
		}

		static ItemBounds()
		{
			string path = Path.Combine(Path.Combine(Core.Config.ConfigDirectory, "Binary"), "Bounds.bin");

			if ( File.Exists( path ) )
			{
				using ( FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader bin = new BinaryReader( fs );

					m_Bounds = new Rectangle2D[0x4000];

					for ( int i = 0; i < 0x4000; ++i )
					{
						int xMin = bin.ReadInt16();
						int yMin = bin.ReadInt16();
						int xMax = bin.ReadInt16();
						int yMax = bin.ReadInt16();

						m_Bounds[i].Set( xMin, yMin, (xMax - xMin) + 1, (yMax - yMin) + 1 );
					}

					bin.Close();
				}
			}
			else
			{
				log.Warn(String.Format("{0} does not exist", path));

				m_Bounds = new Rectangle2D[0x4000];
			}
		}
	}
}
