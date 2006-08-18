using System;
using System.IO;
using Server;

namespace Server.Misc
{
	public class Strandedness
	{
		private static Point2D[] m_Felucca = new Point2D[]
			{
				new Point2D( 2528, 3568 ), new Point2D( 2376, 3400 ), new Point2D( 2528, 3896 ),
				new Point2D( 2168, 3904 ), new Point2D( 1136, 3416 ), new Point2D( 1432, 3648 ),
				new Point2D( 1416, 4000 ), new Point2D( 4512, 3936 ), new Point2D( 4440, 3120 ),
				new Point2D( 4192, 3672 ), new Point2D( 4720, 3472 ), new Point2D( 3744, 2768 ),
				new Point2D( 3480, 2432 ), new Point2D( 3560, 2136 ), new Point2D( 3792, 2112 ),
				new Point2D( 2800, 2296 ), new Point2D( 2736, 2016 ), new Point2D( 4576, 1456 ),
				new Point2D( 4680, 1152 ), new Point2D( 4304, 1104 ), new Point2D( 4496, 984 ),
				new Point2D( 4248, 696 ), new Point2D( 4040, 616 ), new Point2D( 3896, 248 ),
				new Point2D( 4176, 384 ), new Point2D( 3672, 1104 ), new Point2D( 3520, 1152 ),
				new Point2D( 3720, 1360 ), new Point2D( 2184, 2152 ), new Point2D( 1952, 2088 ),
				new Point2D( 2056, 1936 ), new Point2D( 1720, 1992 ), new Point2D( 472, 2064 ),
				new Point2D( 656, 2096 ), new Point2D( 3008, 3592 ), new Point2D( 2784, 3472 ),
				new Point2D( 5456, 2400 ), new Point2D( 5976, 2424 ), new Point2D( 5328, 3112 ),
				new Point2D( 5792, 3152 ), new Point2D( 2120, 3616 ), new Point2D( 2136, 3128 ),
				new Point2D( 1632, 3528 ), new Point2D( 1328, 3160 ), new Point2D( 1072, 3136 ),
				new Point2D( 1128, 2976 ), new Point2D( 960, 2576 ), new Point2D( 752, 1832 ),
				new Point2D( 184, 1488 ), new Point2D( 592, 1440 ), new Point2D( 368, 1216 ),
				new Point2D( 232, 752 ), new Point2D( 696, 744 ), new Point2D( 304, 1000 ),
				new Point2D( 840, 376 ), new Point2D( 1192, 624 ), new Point2D( 1200, 192 ),
				new Point2D( 1512, 240 ), new Point2D( 1336, 456 ), new Point2D( 1536, 648 ),
				new Point2D( 1104, 952 ), new Point2D( 1864, 264 ), new Point2D( 2136, 200 ),
				new Point2D( 2160, 528 ), new Point2D( 1904, 512 ), new Point2D( 2240, 784 ),
				new Point2D( 2536, 776 ), new Point2D( 2488, 216 ), new Point2D( 2336, 72 ),
				new Point2D( 2648, 288 ), new Point2D( 2680, 576 ), new Point2D( 2896, 88 ),
				new Point2D( 2840, 344 ), new Point2D( 3136, 72 ), new Point2D( 2968, 520 ),
				new Point2D( 3192, 328 ), new Point2D( 3448, 208 ), new Point2D( 3432, 608 ),
				new Point2D( 3184, 752 ), new Point2D( 2800, 704 ), new Point2D( 2768, 1016 ),
				new Point2D( 2448, 1232 ), new Point2D( 2272, 920 ), new Point2D( 2072, 1080 ),
				new Point2D( 2048, 1264 ), new Point2D( 1808, 1528 ), new Point2D( 1496, 1880 ),
				new Point2D( 1656, 2168 ), new Point2D( 2096, 2320 ), new Point2D( 1816, 2528 ),
				new Point2D( 1840, 2640 ), new Point2D( 1928, 2952 ), new Point2D( 2120, 2712 )
			};

		private static Point2D[] m_Trammel = m_Felucca;

		private static Point2D[] m_Ilshenar = new Point2D[]
			{
				new Point2D( 1252, 1180 ), new Point2D( 1562, 1090 ), new Point2D( 1444, 1016 ),
				new Point2D( 1324, 968 ), new Point2D( 1418, 806 ), new Point2D( 1722, 874 ),
				new Point2D( 1456, 684 ), new Point2D( 1036, 866 ), new Point2D( 612, 476 ),
				new Point2D( 1476, 372 ), new Point2D( 762, 472 ), new Point2D( 812, 1162 ),
				new Point2D( 1422, 1144 ), new Point2D( 1254, 1066 ), new Point2D( 1598, 870 ),
				new Point2D( 1358, 866 ), new Point2D( 510, 302 ), new Point2D( 510, 392 )
			};

		public static void Initialize()
		{
			EventSink.Login += new LoginEventHandler( EventSink_Login );
		}

		public static void EventSink_Login( LoginEventArgs e )
		{
			Mobile from = e.Mobile;
			Map map = from.Map;

			if ( map == null )
				return;

			if ( map.CanFit( from.X, from.Y, from.Z, 16, false, false ) )
				return;

			bool isStranded = false;

			Tile landTile = map.Tiles.GetLandTile( from.X, from.Y );

			if ( (landTile.ID >= 168 && landTile.ID <= 171) || (landTile.ID >= 310 && landTile.ID <= 311) )
			{
				isStranded = true;
			}
			else
			{
				Tile[] tiles = map.Tiles.GetStaticTiles( from.X, from.Y );

				for ( int i = 0; !isStranded && i < tiles.Length; ++i )
				{
					Tile tile = tiles[i];

					if ( tile.ID >= 0x5797 && tile.ID <= 0x57B2 )
						isStranded = true;
				}
			}

			if ( !isStranded )
				return;

			Point2D[] list;

			if ( map == Map.Felucca )
				list = m_Felucca;
			else if ( map == Map.Trammel )
				list = m_Trammel;
			else if ( map == Map.Ilshenar )
				list = m_Ilshenar;
			else
				return;

			Point2D p = Point2D.Zero;
			double pdist = double.MaxValue;

			for ( int i = 0; i < list.Length; ++i )
			{
				double dist = from.GetDistanceToSqrt( list[i] );

				if ( dist < pdist )
				{
					p = list[i];
					pdist = dist;
				}
			}

			int x = p.X, y = p.Y;
			int z;
			bool canFit = false;

			z = map.GetAverageZ( x, y );
			canFit = map.CanSpawnMobile( x, y, z );

			for ( int i = 1; !canFit && i <= 40; i += 2 )
			{
				for ( int xo = -1; !canFit && xo <= 1; ++xo )
				{
					for ( int yo = -1; !canFit && yo <= 1; ++yo )
					{
						if ( xo == 0 && yo == 0 )
							continue;

						x = p.X + (xo * i);
						y = p.Y + (yo * i);
						z = map.GetAverageZ( x, y );
						canFit = map.CanSpawnMobile( x, y, z );
					}
				}
			}

			if ( canFit )
				from.Location = new Point3D( x, y, z );
		}
	}
}