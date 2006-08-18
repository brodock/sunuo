using System;
using System.Reflection;
using System.Collections;
using Server;
using Server.Network;
using Server.HuePickers;

namespace Server.Gumps
{
	public class SetGump : Gump
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
		private const int TotalHeight = OffsetSize + (2 * (EntryHeight + OffsetSize));

		private const int BackWidth = BorderSize + TotalWidth + BorderSize;
		private const int BackHeight = BorderSize + TotalHeight + BorderSize;

		public SetGump( PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list ) : base( GumpOffsetX, GumpOffsetY )
		{
			m_Property = prop;
			m_Mobile = mobile;
			m_Object = o;
			m_Stack = stack;
			m_Page = page;
			m_List = list;

			bool canNull = !prop.PropertyType.IsValueType;
			bool canDye = prop.IsDefined( typeof( HueAttribute ), false );
			bool isBody = prop.IsDefined( typeof( BodyAttribute ), false );

			object val = prop.GetValue( m_Object, null );
			string initialText;

			if ( val == null )
				initialText = "";
			else
				initialText = val.ToString();

			AddPage( 0 );

			AddBackground( 0, 0, BackWidth, BackHeight + (canNull ? (EntryHeight + OffsetSize) : 0) + (canDye ? (EntryHeight + OffsetSize) : 0) + (isBody ? (EntryHeight + OffsetSize) : 0), BackGumpID );
			AddImageTiled( BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight + (canNull ? (EntryHeight + OffsetSize) : 0) + (canDye ? (EntryHeight + OffsetSize) : 0) + (isBody ? (EntryHeight + OffsetSize) : 0), OffsetGumpID );

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
			AddTextEntry( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, 0, initialText );
			x += EntryWidth + OffsetSize;

			if ( SetGumpID != 0 )
				AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

			AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1, GumpButtonType.Reply, 0 );

			if ( canNull )
			{
				x = BorderSize + OffsetSize;
				y += EntryHeight + OffsetSize;

				AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
				AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Null" );
				x += EntryWidth + OffsetSize;

				if ( SetGumpID != 0 )
					AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

				AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2, GumpButtonType.Reply, 0 );
			}

			if ( canDye )
			{
				x = BorderSize + OffsetSize;
				y += EntryHeight + OffsetSize;

				AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
				AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Hue Picker" );
				x += EntryWidth + OffsetSize;

				if ( SetGumpID != 0 )
					AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

				AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3, GumpButtonType.Reply, 0 );
			}

			if ( isBody )
			{
				x = BorderSize + OffsetSize;
				y += EntryHeight + OffsetSize;

				AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
				AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Body Picker" );
				x += EntryWidth + OffsetSize;

				if ( SetGumpID != 0 )
					AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

				AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 4, GumpButtonType.Reply, 0 );
			}
		}

		private class InternalPicker : HuePicker
		{
			private PropertyInfo m_Property;
			private Mobile m_Mobile;
			private object m_Object;
			private Stack m_Stack;
			private int m_Page;
			private ArrayList m_List;

			public InternalPicker( PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list ) : base( ((IHued)o).HuedItemID )
			{
				m_Property = prop;
				m_Mobile = mobile;
				m_Object = o;
				m_Stack = stack;
				m_Page = page;
				m_List = list;
			}

			public override void OnResponse( int hue )
			{
				try
				{
					Server.Scripts.Commands.CommandLogging.LogChangeProperty( m_Mobile, m_Object, m_Property.Name, hue.ToString() );
					m_Property.SetValue( m_Object, hue, null );
					PropertiesGump.OnValueChanged( m_Object, m_Property, m_Stack );
				}
				catch
				{
					m_Mobile.SendMessage( "An exception was caught. The property may not have changed." );
				}

				m_Mobile.SendGump( new PropertiesGump( m_Mobile, m_Object, m_Stack, m_List, m_Page ) );
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			object toSet;
			bool shouldSet, shouldSend = true;

			switch ( info.ButtonID )
			{
				case 1:
				{
					TextRelay text = info.GetTextEntry( 0 );

					if ( text != null )
					{
						try
						{
							toSet = PropertiesGump.GetObjectFromString( m_Property.PropertyType, text.Text );
							shouldSet = true;
						}
						catch
						{
							toSet = null;
							shouldSet = false;
							m_Mobile.SendMessage( "Bad format" );
						}
					}
					else
					{
						toSet = null;
						shouldSet = false;
					}

					break;
				}
				case 2: // Null
				{
					toSet = null;
					shouldSet = true;

					break;
				}
				case 3: // Hue Picker
				{
					toSet = null;
					shouldSet = false;
					shouldSend = false;

					m_Mobile.SendHuePicker( new InternalPicker( m_Property, m_Mobile, m_Object, m_Stack, m_Page, m_List ) );

					break;
				}
				case 4: // Body Picker
				{
					toSet = null;
					shouldSet = false;
					shouldSend = false;

					m_Mobile.SendGump( new SetBodyGump( m_Property, m_Mobile, m_Object, m_Stack, m_Page, m_List ) );

					break;
				}
				default:
				{
					toSet = null;
					shouldSet = false;

					break;
				}
			}

			if ( shouldSet )
			{
				try
				{
					Server.Scripts.Commands.CommandLogging.LogChangeProperty( m_Mobile, m_Object, m_Property.Name, toSet==null?"(null)":toSet.ToString() );
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