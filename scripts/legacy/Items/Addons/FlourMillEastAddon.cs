using System;
using Server;

namespace Server.Items
{
	public class FlourMillEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new FlourMillEastDeed(); } }

		[Constructable]
		public FlourMillEastAddon()
		{
			AddComponent( new AddonComponent( 0x1922 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x1920 ), -1, 0, 0 );
			AddComponent( new AddonComponent( 0x1924 ), 1, 0, 0 );
		}

		public FlourMillEastAddon( Serial serial ) : base( serial )
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

	public class FlourMillEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new FlourMillEastAddon(); } }
		public override int LabelNumber{ get{ return 1044347; } } // flour mill (east)

		[Constructable]
		public FlourMillEastDeed()
		{
		}

		public FlourMillEastDeed( Serial serial ) : base( serial )
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