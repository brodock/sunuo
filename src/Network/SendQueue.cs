/***************************************************************************
 *                               SendQueue.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: SendQueue.cs,v 1.4 2005/01/22 04:25:04 krrios Exp $
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
using System.Collections;

namespace Server.Network
{
	public class SendQueue
	{
		private class Entry
		{
			public byte[] m_Buffer;
			public int m_Length;
			public bool m_Managed;

			private Entry( byte[] buffer, int length, bool managed )
			{
				m_Buffer = buffer;
				m_Length = length;
				m_Managed = managed;
			}

			private static Stack m_Pool = new Stack();

			public static Entry Pool( byte[] buffer, int length, bool managed )
			{
				lock ( m_Pool )
				{
					if ( m_Pool.Count == 0 )
						return new Entry( buffer, length, managed );

					Entry e = (Entry)m_Pool.Pop();

					e.m_Buffer = buffer;
					e.m_Length = length;
					e.m_Managed = managed;

					return e;
				}
			}

			public static void Release( Entry e )
			{
				lock ( m_Pool )
				{
					m_Pool.Push( e );

					if ( e.m_Managed )
						ReleaseBuffer( e.m_Buffer );
				}
			}
		}

		private static int m_CoalesceBufferSize = 512;
		private static bool m_CoalescePerSlice = true;
		private static BufferPool m_UnusedBuffers = new BufferPool( 4096, m_CoalesceBufferSize );

		public static bool CoalescePerSlice
		{
			get{ return m_CoalescePerSlice; }
			set{ m_CoalescePerSlice = value; }
		}

		public static int CoalesceBufferSize
		{
			get{ return m_CoalesceBufferSize; }
			set
			{
				if ( m_CoalesceBufferSize == value )
					return;

				m_UnusedBuffers = new BufferPool( 4096, m_CoalesceBufferSize );
				m_CoalesceBufferSize = value;
			}
		}
		public static byte[] GetUnusedBuffer()
		{
			return m_UnusedBuffers.AquireBuffer();
		}

		public static void ReleaseBuffer( byte[] buffer )
		{
			if ( buffer == null )
				Console.WriteLine( "Warning: Attempting to release null packet buffer" );
			else if ( buffer.Length == m_CoalesceBufferSize )
				m_UnusedBuffers.ReleaseBuffer( buffer );
		}

		private Queue m_Queue;
		private Entry m_Buffered;

		public bool IsFlushReady{ get{ return ( m_Queue.Count == 0 && m_Buffered != null ); } }
		public bool IsEmpty{ get{ return ( m_Queue.Count == 0 && m_Buffered == null ); } }

		public void Clear()
		{
			if ( m_Buffered != null )
			{
				Entry.Release( m_Buffered );
				m_Buffered = null;
			}

			while ( m_Queue.Count > 0 )
				Entry.Release( (Entry) m_Queue.Dequeue() );
		}

		public byte[] CheckFlushReady( ref int length )
		{
			Entry buffered = m_Buffered;

			if ( m_Queue.Count == 0 && buffered != null )
			{
				m_Buffered = null;

				m_Queue.Enqueue( buffered );
				length = buffered.m_Length;
				return buffered.m_Buffer;
			}

			return null;
		}

		public SendQueue()
		{
			m_Queue = new Queue();
		}

		public byte[] Peek( ref int length )
		{
			if ( m_Queue.Count > 0 )
			{
				Entry entry = (Entry)m_Queue.Peek();

				length = entry.m_Length;
				return entry.m_Buffer;
			}

			return null;
		}

		public byte[] Dequeue( ref int length )
		{
			Entry.Release( (Entry)m_Queue.Dequeue() );

			if ( m_Queue.Count == 0 && m_Buffered != null && !m_CoalescePerSlice )
			{
				m_Queue.Enqueue( m_Buffered );
				m_Buffered = null;
			}

			if ( m_Queue.Count > 0 )
			{
				Entry entry = (Entry)m_Queue.Peek();

				length = entry.m_Length;
				return entry.m_Buffer;
			}

			return null;
		}

		public bool Enqueue( byte[] buffer, int length )
		{
			if ( buffer == null )
			{
				Console.WriteLine( "Warning: Attempting to send null packet buffer" );
				return false;
			}

			if ( m_Queue.Count == 0 && !m_CoalescePerSlice )
			{
				m_Queue.Enqueue( Entry.Pool( buffer, length, false ) );
				return true;
			}
			else
			{
				// buffer it

				if ( m_Buffered == null )
				{
					if ( length >= m_CoalesceBufferSize )
					{
						m_Queue.Enqueue( Entry.Pool( buffer, length, false ) );

						return ( m_Queue.Count == 1 );
					}
					else
					{
						m_Buffered = Entry.Pool( GetUnusedBuffer(), 0, true );

						Buffer.BlockCopy( buffer, 0, m_Buffered.m_Buffer, 0, length );
						m_Buffered.m_Length += length;
					}
				}
				else if ( length > 0 ) // sanity
				{
					int availableSpace = m_Buffered.m_Buffer.Length - m_Buffered.m_Length;

					if ( availableSpace < length )
					{
						if ( (length - availableSpace) > m_CoalesceBufferSize )
						{
							m_Queue.Enqueue( m_Buffered );

							m_Buffered = null;

							m_Queue.Enqueue( Entry.Pool( buffer, length, false ) );

							return ( m_Queue.Count == 2 );
						}
						else
						{
							if ( availableSpace > 0 )
								Buffer.BlockCopy( buffer, 0, m_Buffered.m_Buffer, m_Buffered.m_Length, availableSpace );
							else
								availableSpace = 0;

							m_Buffered.m_Length += availableSpace;

							m_Queue.Enqueue( m_Buffered );

							length -= availableSpace;

							m_Buffered = Entry.Pool( GetUnusedBuffer(), 0, true );

							Buffer.BlockCopy( buffer, availableSpace, m_Buffered.m_Buffer, 0, length );
							m_Buffered.m_Length += length;

							return ( m_Queue.Count == 1 );
						}
					}
					else
					{
						Buffer.BlockCopy( buffer, 0, m_Buffered.m_Buffer, m_Buffered.m_Length, length );
						m_Buffered.m_Length += length;
					}
				}

				/*if ( m_Buffered != null && (m_Buffered.m_Length + length) > m_Buffered.m_Buffer.Length )
				{
					m_Queue.Enqueue( m_Buffered );
					m_Buffered = null;
				}

				if ( length >= m_CoalesceBufferSize )
				{
					m_Queue.Enqueue( Entry.Pool( buffer, length, false ) );
				}
				else
				{
					if ( m_Buffered == null )
						m_Buffered = Entry.Pool( GetUnusedBuffer(), 0, true );

					Buffer.BlockCopy( buffer, 0, m_Buffered.m_Buffer, m_Buffered.m_Length, length );
					m_Buffered.m_Length += length;
				}*/
			}

			return false;
		}
	}
}