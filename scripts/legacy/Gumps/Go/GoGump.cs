using System;
using System.Collections;
using Server;
using Server.Network;
using Server.Gumps;

namespace Server.Gumps
{
	public class GoGump : Gump
	{
		public static readonly LocationTree Felucca = new LocationTree( "felucca.xml", Map.Felucca );
		public static readonly LocationTree Trammel = new LocationTree( "trammel.xml", Map.Trammel );
		public static readonly LocationTree Ilshenar = new LocationTree( "ilshenar.xml", Map.Ilshenar );
		public static readonly LocationTree Malas = new LocationTree( "malas.xml", Map.Malas );
		public static readonly LocationTree Tokuno = new LocationTree( "tokuno.xml", Map.Tokuno );

		public static bool OldStyle = PropsConfig.OldStyle;

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

		private static bool PrevLabel = false, NextLabel = false;

		private const int PrevLabelOffsetX = PrevWidth + 1;
		private const int PrevLabelOffsetY = 0;

		private const int NextLabelOffsetX = -29;
		private const int NextLabelOffsetY = 0;

		private const int EntryWidth = 180;
		private const int EntryCount = 15;

		private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
		private const int TotalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (EntryCount + 1));

		private const int BackWidth = BorderSize + TotalWidth + BorderSize;
		private const int BackHeight = BorderSize + TotalHeight + BorderSize;

		public static void DisplayTo( Mobile from )
		{
			LocationTree tree;

			if ( from.Map == Map.Ilshenar )
				tree = Ilshenar;
			else if ( from.Map == Map.Felucca )
				tree = Felucca;
			else if ( from.Map == Map.Trammel )
				tree = Trammel;
			else if ( from.Map == Map.Malas )
				tree = Malas;
			else
				tree = Tokuno;

			ParentNode branch = (ParentNode)tree.LastBranch[from];

			if ( branch == null )
				branch = tree.Root;

			if ( branch != null )
				from.SendGump( new GoGump( 0, from, tree, branch ) );
		}

		private LocationTree m_Tree;
		private ParentNode m_Node;
		private int m_Page;

		private GoGump( int page, Mobile from, LocationTree tree, ParentNode node ) : base( 50, 50 )
		{
			from.CloseGump( typeof( GoGump ) );

			tree.LastBranch[from] = node;

			m_Page = page;
			m_Tree = tree;
			m_Node = node;

			int x = BorderSize + OffsetSize;
			int y = BorderSize + OffsetSize;

			int count = node.Children.Length - (page * EntryCount);

			if ( count < 0 )
				count = 0;
			else if ( count > EntryCount )
				count = EntryCount;

			int totalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (count + 1));

			AddPage( 0 );

			AddBackground( 0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID );
			AddImageTiled( BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight, OffsetGumpID );

			if ( OldStyle )
				AddImageTiled( x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID );
			else
				AddImageTiled( x, y, PrevWidth, EntryHeight, HeaderGumpID );

			if ( node.Parent != null )
			{
				AddButton( x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1, GumpButtonType.Reply, 0 );

				if ( PrevLabel )
					AddLabel( x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous" );
			}

			x += PrevWidth + OffsetSize;

			int emptyWidth = TotalWidth - (PrevWidth * 2) - NextWidth - (OffsetSize * 5) - (OldStyle ? SetWidth + OffsetSize : 0);

			if ( !OldStyle )
				AddImageTiled( x - (OldStyle ? OffsetSize : 0), y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, EntryGumpID );

			AddHtml( x + TextOffsetX, y, emptyWidth - TextOffsetX, EntryHeight, String.Format( "<center>{0}</center>", node.Name ), false, false );

			x += emptyWidth + OffsetSize;

			if ( OldStyle )
				AddImageTiled( x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID );
			else
				AddImageTiled( x, y, PrevWidth, EntryHeight, HeaderGumpID );

			if ( page > 0 )
			{
				AddButton( x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 2, GumpButtonType.Reply, 0 );

				if ( PrevLabel )
					AddLabel( x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous" );
			}

			x += PrevWidth + OffsetSize;

			if ( !OldStyle )
				AddImageTiled( x, y, NextWidth, EntryHeight, HeaderGumpID );

			if ( (page + 1) * EntryCount < node.Children.Length )
			{
				AddButton( x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 3, GumpButtonType.Reply, 1 );

				if ( NextLabel )
					AddLabel( x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next" );
			}

			for ( int i = 0, index = page * EntryCount; i < EntryCount && index < node.Children.Length; ++i, ++index )
			{
				x = BorderSize + OffsetSize;
				y += EntryHeight + OffsetSize;

				object child = node.Children[index];
				string name = "";

				if ( child is ParentNode )
					name = ((ParentNode)child).Name;
				else if ( child is ChildNode )
					name = ((ChildNode)child).Name;

				AddImageTiled( x, y, EntryWidth, EntryHeight, EntryGumpID );
				AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, name );

				x += EntryWidth + OffsetSize;

				if ( SetGumpID != 0 )
					AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

				AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, index + 4, GumpButtonType.Reply, 0 );
			}
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile from = state.Mobile;

			switch ( info.ButtonID )
			{
				case 1:
				{
					if ( m_Node.Parent != null )
						from.SendGump( new GoGump( 0, from, m_Tree, m_Node.Parent ) );

					break;
				}
				case 2:
				{
					if ( m_Page > 0 )
						from.SendGump( new GoGump( m_Page - 1, from, m_Tree, m_Node ) );

					break;
				}
				case 3:
				{
					if ( (m_Page + 1) * EntryCount < m_Node.Children.Length )
						from.SendGump( new GoGump( m_Page + 1, from, m_Tree, m_Node ) );

					break;
				}
				default:
				{
					int index = info.ButtonID - 4;

					if ( index >= 0 && index < m_Node.Children.Length )
					{
						object o = m_Node.Children[index];

						if ( o is ParentNode )
						{
							from.SendGump( new GoGump( 0, from, m_Tree, (ParentNode)o ) );
						}
						else
						{
							ChildNode n = (ChildNode)o;

							from.MoveToWorld( n.Location, m_Tree.Map );
						}
					}

					break;
				}
			}
		}
	}
}