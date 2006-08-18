using System;
using Server.Items;
using Server.Network;

namespace Server.Items
{
	[Furniture]
	[Flipable( 0x232A, 0x232B )]
	public class GiftBox : BaseContainer
	{
		public override int DefaultGumpID{ get{ return 0x102; } }
		public override int DefaultDropSound{ get{ return 0x42; } }

		public override Rectangle2D Bounds
		{
			get{ return new Rectangle2D( 35, 10, 155, 85 ); }
		}

		[Constructable]
		public GiftBox() : this( Utility.RandomDyedHue() )
		{
		}

		[Constructable]
		public GiftBox( int hue ) : base( Utility.Random( 0x232A, 2 ) )
		{
			Weight = 2.0;
			Hue = hue;
		}

		public GiftBox( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}