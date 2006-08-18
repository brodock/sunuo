using System;
using System.Reflection;
using System.Collections;
using Server;
using Server.Network;

namespace Server.Gumps
{
	public class SetTimeSpanGump : Gump
	{
		private PropertyInfo m_Property;
		private Mobile m_Mobile;
		private object m_Object;
		private Stack m_Stack;
		private int m_Page;
		private ArrayList m_List;

		public const bool OldStyle = PropsConfig.OldStyle;

		public const int GumpOffsetX = PropsConfig.GumpOffsetX;
		public const int GumpOffsetY = PropsConfig.GumpOffsetY;

		public const int TextHue = PropsConfig.TextHue;
		public const int TextOffsetX = PropsConfig.TextOffsetX;

		public const int OffsetGumpID = PropsConfig.OffsetGumpID;
		public const int HeaderGumpID = PropsConfig.HeaderGumpID;
		public const int  EntryGumpID = PropsConfig.EntryGumpID;
		public const int   BackGumpID = PropsConfig.BackGumpID;
		public const int    SetGumpID = PropsConfig.SetGumpID;

		public const int SetWidth = PropsConfig.SetWidth;
		public const int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
		public const int SetButtonID1 = PropsConfig.SetButtonID1;
		public const int SetButtonID2 = PropsConfig.SetButtonID2;

		public const int PrevWidth = PropsConfig.PrevWidth;
		public const int PrevOffsetX = PropsConfig.PrevOffsetX, PrevOffsetY = PropsConfig.PrevOffsetY;
		public const int PrevButtonID1 = PropsConfig.PrevButtonID1;
		public const int PrevButtonID2 = PropsConfig.PrevButtonID2;

		public const int NextWidth = PropsConfig.NextWidth;
		public const int NextOffsetX = PropsConfig.NextOffsetX, NextOffsetY = PropsConfig.NextOffsetY;
		public const int NextButtonID1 = PropsConfig.NextButtonID1;
		public const int NextButtonID2 = PropsConfig.NextButtonID2;

		public const int OffsetSize = PropsConfig.OffsetSize;

		public const int EntryHeight = PropsConfig.EntryHeight;
		public const int BorderSize = PropsConfig.BorderSize;

		private const int EntryWidth = 212;

		private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
		private const int TotalHeight = OffsetSize + (7 * (EntryHeight + OffsetSize));

		private const int BackWidth = BorderSize + TotalWidth + BorderSize;
		private const int BackHeight = BorderSize + TotalHeight + BorderSize;

		public SetTimeSpanGump( PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list ) : base( GumpOffsetX, GumpOffsetY )
		{
			m_Property = prop;
			m_Mobile = mobile;
			m_Object = o;
			m_Stack = stack;
			m_Page = page;
			m_List = list;

			TimeSpan ts = (TimeSpan)prop.GetValue( o, null );

			AddPage( 0 );

			AddBackground( 0, 0, BackWidth, BackHeight, BackGumpID );
			AddImageTiled( BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight, OffsetGumpID );

			AddRect( 0, prop.Name, 0, -1 );
			AddRect( 1, ts.ToString(), 0, -1 );
			AddRect( 2, "Zero", 1, -1 );
			AddRect( 3, "From H:M:S", 2, -1 );
			AddRect( 4, "H:", 3, 0 );
			AddRect( 5, "M:", 4, 1 );
			AddRect( 6, "S:", 5, 2 );
		}

		private void AddRect( int index, string str, int button, int text )
		{
			int x = BorderSize + OffsetSize;
			int y = BorderSize + OffsetSize + (index * (EntryHeight + OffsetSize));

			AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, str );

			if ( text != -1 )
				AddTextEntry( x + 16 + TextOffsetX, y, EntryWidth - TextOffsetX - 16, EntryHeight, TextHue, text, "" );

			x += EntryWidth + OffsetSize;

			if ( SetGumpID != 0 )
				AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

			if ( button != 0 )
				AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, button, GumpButtonType.Reply, 0 );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			TimeSpan toSet;
			bool shouldSet, shouldSend;

			TextRelay h = info.GetTextEntry( 0 );
			TextRelay m = info.GetTextEntry( 1 );
			TextRelay s = info.GetTextEntry( 2 );

			switch ( info.ButtonID )
			{
				case 1: // Zero
				{
					toSet = TimeSpan.Zero;
					shouldSet = true;
					shouldSend = true;

					break;
				}
				case 2: // From H:M:S
				{
					if ( h != null && m != null && s != null )
					{
						try
						{
							toSet = TimeSpan.Parse( h.Text + ":" + m.Text + ":" + s.Text );
							shouldSet = true;
							shouldSend = true;

							break;
						}
						catch
						{
						}
					}

					toSet = TimeSpan.Zero;
					shouldSet = false;
					shouldSend = false;

					break;
				}
				case 3: // From H
				{
					if ( h != null )
					{
						try
						{
							toSet = TimeSpan.FromHours( Utility.ToDouble( h.Text ) );
							shouldSet = true;
							shouldSend = true;

							break;
						}
						catch
						{
						}
					}

					toSet = TimeSpan.Zero;
					shouldSet = false;
					shouldSend = false;

					break;
				}
				case 4: // From M
				{
					if ( m != null )
					{
						try
						{
							toSet = TimeSpan.FromMinutes( Utility.ToDouble( m.Text ) );
							shouldSet = true;
							shouldSend = true;

							break;
						}
						catch
						{
						}
					}

					toSet = TimeSpan.Zero;
					shouldSet = false;
					shouldSend = false;

					break;
				}
				case 5: // From S
				{
					if ( s != null )
					{
						try
						{
							toSet = TimeSpan.FromSeconds( Utility.ToDouble( s.Text ) );
							shouldSet = true;
							shouldSend = true;

							break;
						}
						catch
						{
						}
					}

					toSet = TimeSpan.Zero;
					shouldSet = false;
					shouldSend = false;

					break;
				}
				default:
				{
					toSet = TimeSpan.Zero;
					shouldSet = false;
					shouldSend = true;

					break;
				}
			}

			if ( shouldSet )
			{
				try
				{
					Server.Scripts.Commands.CommandLogging.LogChangeProperty( m_Mobile, m_Object, m_Property.Name, toSet.ToString() );
					m_Property.SetValue( m_Object, toSet, null );
					PropertiesGump.OnValueChanged( m_Object, m_Property, m_Stack );
				}
				catch
				{
					m_Mobile.SendMessage( "An exception was caught. The property may not have changed." );
				}
			}

			if ( shouldSend )
				m_Mobile.SendGump( new PropertiesGump( m_Mobile, m_Object, m_Stack, m_List, m_Page ) );
		}
	}
}