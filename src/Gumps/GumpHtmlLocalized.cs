/***************************************************************************
 *                            GumpHtmlLocalized.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: GumpHtmlLocalized.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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
using Server.Network;

namespace Server.Gumps
{
	public class GumpHtmlLocalized : GumpEntry
	{
		private int m_X, m_Y;
		private int m_Width, m_Height;
		private int m_Number;
		private bool m_Background, m_Scrollbar;
		private int m_Color;
		private bool m_UseColor;

		public int X
		{
			get
			{
				return m_X;
			}
			set
			{
				Delta( ref m_X, value );
			}
		}

		public int Y
		{
			get
			{
				return m_Y;
			}
			set
			{
				Delta( ref m_Y, value );
			}
		}

		public int Width
		{
			get
			{
				return m_Width;
			}
			set
			{
				Delta( ref m_Width, value );
			}
		}

		public int Height
		{
			get
			{
				return m_Height;
			}
			set
			{
				Delta( ref m_Height, value );
			}
		}

		public int Number
		{
			get
			{
				return m_Number;
			}
			set
			{
				Delta( ref m_Number, value );
			}
		}

		public int Color
		{
			get
			{
				return m_Color;
			}
			set
			{
				Delta( ref m_Color, value );
			}
		}

		public bool UseColor
		{
			get
			{
				return m_UseColor;
			}
			set
			{
				Delta( ref m_UseColor, value );
			}
		}

		public bool Background
		{
			get
			{
				return m_Background;
			}
			set
			{
				Delta( ref m_Background, value );
			}
		}

		public bool Scrollbar
		{
			get
			{
				return m_Scrollbar;
			}
			set
			{
				Delta( ref m_Scrollbar, value );
			}
		}

		public GumpHtmlLocalized( int x, int y, int width, int height, int number, bool background, bool scrollbar )
		{
			m_X = x;
			m_Y = y;
			m_Width = width;
			m_Height = height;
			m_Number = number;
			m_Background = background;
			m_Scrollbar = scrollbar;
		}

		public GumpHtmlLocalized( int x, int y, int width, int height, int number, int color, bool background, bool scrollbar )
		{
			m_X = x;
			m_Y = y;
			m_Width = width;
			m_Height = height;
			m_Number = number;
			m_Background = background;
			m_Scrollbar = scrollbar;

			m_Color = color;
			m_UseColor = true;
		}

		public override string Compile()
		{
			if ( m_UseColor )
				return String.Format( "{{ xmfhtmlgumpcolor {0} {1} {2} {3} {4} {5} {6} {7} }}", m_X, m_Y, m_Width, m_Height, m_Number, m_Background ? 1 : 0, m_Scrollbar ? 1 : 0, m_Color );
			else
				return String.Format( "{{ xmfhtmlgump {0} {1} {2} {3} {4} {5} {6} }}", m_X, m_Y, m_Width, m_Height, m_Number, m_Background ? 1 : 0, m_Scrollbar ? 1 : 0 );
		}

		private static byte[] m_LayoutNameColor = Gump.StringToBuffer( "xmfhtmlgumpcolor" );
		private static byte[] m_LayoutName = Gump.StringToBuffer( "xmfhtmlgump" );

		public override void AppendTo( DisplayGumpFast disp )
		{
			disp.AppendLayout( m_UseColor ? m_LayoutNameColor : m_LayoutName );
			disp.AppendLayout( m_X );
			disp.AppendLayout( m_Y );
			disp.AppendLayout( m_Width );
			disp.AppendLayout( m_Height );
			disp.AppendLayout( m_Number );
			disp.AppendLayout( m_Background );
			disp.AppendLayout( m_Scrollbar );

			if ( m_UseColor )
				disp.AppendLayout( m_Color );
		}
	}
}