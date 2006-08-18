using System;
using System.Reflection;
using System.Collections;
using Server;
using Server.Targeting;
using Server.Network;

namespace Server.Gumps
{
	public class SetPoint3DGump : Gump
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

		private const int CoordWidth = 70;
		private const int EntryWidth = CoordWidth + OffsetSize + CoordWidth + OffsetSize + CoordWidth;

		private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
		private const int TotalHeight = OffsetSize + (4 * (EntryHeight + OffsetSize));

		private const int BackWidth = BorderSize + TotalWidth + BorderSize;
		private const int BackHeight = BorderSize + TotalHeight + BorderSize;

		public SetPoint3DGump( PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list ) : base( GumpOffsetX, GumpOffsetY )
		{
			m_Property = prop;
			m_Mobile = mobile;
			m_Object = o;
			m_Stack = stack;
			m_Page = page;
			m_List = list;

			Point3D p = (Point3D)prop.GetValue( o, null );

			AddPage( 0 );

			AddBackground( 0, 0, BackWidth, BackHeight, BackGumpID );
			AddImageTiled( BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight, OffsetGumpID );

			int x = BorderSize + OffsetSize;
			int y = BorderSize + OffsetSize;

			AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, prop.Name );
			x += EntryWidth + OffsetSize;

			if ( SetGumpID != 0 )
				AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

			x = BorderSize + OffsetSize;
			y += EntryHeight + OffsetSize;

			AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Use your location" );
			x += EntryWidth + OffsetSize;

			if ( SetGumpID != 0 )
				AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );
			AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1, GumpButtonType.Reply, 0 );

			x = BorderSize + OffsetSize;
			y += EntryHeight + OffsetSize;

			AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Target a location" );
			x += EntryWidth + OffsetSize;

			if ( SetGumpID != 0 )
				AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );
			AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2, GumpButtonType.Reply, 0 );

			x = BorderSize + OffsetSize;
			y += EntryHeight + OffsetSize;

			AddImageTiled( x, y, CoordWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "X:" );
			AddTextEntry( x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 0, p.X.ToString() );
			x += CoordWidth + OffsetSize;

			AddImageTiled( x, y, CoordWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "Y:" );
			AddTextEntry( x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 1, p.Y.ToString() );
			x += CoordWidth + OffsetSize;

			AddImageTiled( x, y, CoordWidth, EntryHeight, EntryGumpID );
			AddLabelCropped( x + TextOffsetX, y, CoordWidth - TextOffsetX, EntryHeight, TextHue, "Z:" );
			AddTextEntry( x + 16, y, CoordWidth - 16, EntryHeight, TextHue, 2, p.Z.ToString() );
			x += CoordWidth + OffsetSize;

			if ( SetGumpID != 0 )
				AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );
			AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3, GumpButtonType.Reply, 0 );
		}

		private class InternalTarget : Target
		{
			private PropertyInfo m_Property;
			private Mobile m_Mobile;
			private object m_Object;
			private Stack m_Stack;
			private int m_Page;
			private ArrayList m_List;

			public InternalTarget( PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list ) : base( -1, true, TargetFlags.None )
			{
				m_Property = prop;
				m_Mobile = mobile;
				m_Object = o;
				m_Stack = stack;
				m_Page = page;
				m_List = list;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				IPoint3D p = targeted as IPoint3D;

				if ( p != null )
				{
					try
					{
						Server.Scripts.Commands.CommandLogging.LogChangeProperty( m_Mobile, m_Object, m_Property.Name, new Point3D( p ).ToString() );
						m_Property.SetValue( m_Object, new Point3D( p ), null );
						PropertiesGump.OnValueChanged( m_Object, m_Property, m_Stack );
					}
					catch
					{
						m_Mobile.SendMessage( "An exception was caught. The property may not have changed." );
					}
				}
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Mobile.SendGump( new PropertiesGump( m_Mobile, m_Object, m_Stack, m_List, m_Page ) );
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			Point3D toSet;
			bool shouldSet, shouldSend;

			switch ( info.ButtonID )
			{
				case 1: // Current location
				{
					toSet = m_Mobile.Location;
					shouldSet = true;
					shouldSend = true;

					break;
				}
				case 2: // Pick location
				{
					m_Mobile.Target = new InternalTarget( m_Property, m_Mobile, m_Object, m_Stack, m_Page, m_List );

					toSet = Point3D.Zero;
					shouldSet = false;
					shouldSend = false;

					break;
				}
				case 3: // Use values
				{
					TextRelay x = info.GetTextEntry( 0 );
					TextRelay y = info.GetTextEntry( 1 );
					TextRelay z = info.GetTextEntry( 2 );

					toSet = new Point3D( x == null ? 0 : Utility.ToInt32( x.Text ), y == null ? 0 : Utility.ToInt32( y.Text ), z == null ? 0 : Utility.ToInt32( z.Text ) );
					shouldSet = true;
					shouldSend = true;

					break;
				}
				default:
				{
					toSet = Point3D.Zero;
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