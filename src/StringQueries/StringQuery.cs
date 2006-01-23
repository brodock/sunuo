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
using System.Text;
using Server.Network;

namespace Server.StringQueries
{
	public class StringQuery
	{
		private static int m_NextSerial = 1;

		private int m_Serial;

		private string m_TopText = "";
		private string m_EntryText = "";

		private bool m_Cancellable = false;
		private byte m_Number = 1;
		private int m_Max = 10;

		public StringQuery()
		{
			do
			{
				m_Serial = m_NextSerial++;
			} while ( m_Serial == 0 );
		}

		public void Invalidate()
		{
			if ( m_Packet != null )
			{
				m_Packet = null;
			}
		}

		public int Serial
		{
			get
			{
				return m_Serial;
			}
			set
			{
				if ( m_Serial != value )
				{
					m_Serial = value;
					Invalidate();
				}
			}
		}

		public string TopText
		{
			get
			{
				return m_TopText;
			}
			set
			{
				if ( m_TopText != value )
				{
					m_TopText = value;
					Invalidate();
				}
			}
		}

		public string EntryText
		{
			get
			{
				return m_EntryText;
			}
			set
			{
				if ( m_EntryText != value )
				{
					m_EntryText = value;
					Invalidate();
				}
			}
		}

		public bool Cancellable
		{
			get
			{
				return m_Cancellable;
			}
			set
			{
				if ( m_Cancellable != value )
				{
					m_Cancellable = value;
					Invalidate();
				}
			}
		}

		public byte Number
		{
			get
			{
				return m_Number;
			}
			set
			{
				if ( m_Number != value )
				{
					m_Number = value;
					Invalidate();
				}
			}
		}

		public int Max
		{
			get
			{
				return m_Max;
			}
			set
			{
				if ( m_Max != value )
				{
					m_Max = value;
					Invalidate();
				}
			}
		}

		public void SendTo( NetState state )
		{
			state.AddStringQuery( this );

			if ( m_Packet == null ) {
				m_Packet = new DisplayStringQuery( m_Serial, m_TopText, m_Cancellable,
												   m_Number, m_Max, m_EntryText );
			}

			state.Send( m_Packet );
		}

		private DisplayStringQuery m_Packet;

		public virtual void OnResponse( NetState sender, bool okay, string text )
		{
		}
	}
}
