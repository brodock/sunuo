using System;
using System.IO;
using System.Collections;
using Server;
using Server.Regions;

namespace Server
{ 
	public class TreasureRegion : Region
	{
		private const int Range = 5; // No house may be placed within 5 tiles of the treasure

		public TreasureRegion( int x, int y, Map map ): base( "", "DynRegion", map )
		{
			Priority = Region.TownPriority;
			LoadFromXml = false;

			Coords = new ArrayList();
			Coords.Add( new Rectangle2D( x - Range, y - Range, 1 + (Range * 2), 1 + (Range * 2) ) );

			GoLocation = new Point3D( x, y, map.GetAverageZ( x, y ) );
		}

		public static void Initialize()
		{
			string filePath = Path.Combine( Core.BaseDirectory, "Data/treasure.cfg" );
			int i = 0, x = 0, y = 0;

			if ( File.Exists( filePath ) )
			{
				using ( StreamReader ip = new StreamReader( filePath ) )
				{
					string line;

					while ( (line = ip.ReadLine()) != null )
					{
						i++;

						try
						{
							string[] split = line.Split( ' ' );

							x = Convert.ToInt32( split[0] );
							y = Convert.ToInt32( split[1] );

							try
							{
								Region.AddRegion( new TreasureRegion( x, y, Map.Felucca ) );
								Region.AddRegion( new TreasureRegion( x, y, Map.Trammel ) );
							}
							catch ( Exception e )
							{
								Console.WriteLine( "{0} {1} {2} {3}", i, x, y, e );
							}
						}
						catch
						{
						}
					}
				}
			}
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}

		public override void OnEnter( Mobile m )
		{
			if ( m.AccessLevel > AccessLevel.Player )
				m.SendMessage( "You have entered a protected treasure map area." );
		}

		public override void OnExit( Mobile m )
		{
			if ( m.AccessLevel > AccessLevel.Player )
				m.SendMessage( "You have left a protected treasure map area." );
		}
	}
}