/***************************************************************************
 *                               BufferPool.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: BufferPool.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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

namespace Server.Network
{
	public class BufferPool
	{
		private int m_BufferSize;
		private Queue m_FreeBuffers;

		public BufferPool( int initialCapacity, int bufferSize )
		{
			m_BufferSize = bufferSize;
			m_FreeBuffers = new Queue( initialCapacity );

			for ( int i = 0; i < initialCapacity; ++i )
				m_FreeBuffers.Enqueue( new byte[bufferSize] );
		}

		public byte[] AquireBuffer()
		{
			if ( m_FreeBuffers.Count > 0 )
			{
				lock ( m_FreeBuffers )
				{
					if ( m_FreeBuffers.Count > 0 )
						return (byte[]) m_FreeBuffers.Dequeue();
				}
			}

			return new byte[m_BufferSize];
		}

		public void ReleaseBuffer( byte[] buffer )
		{
			lock ( m_FreeBuffers )
				m_FreeBuffers.Enqueue( buffer );
		}
	}
}