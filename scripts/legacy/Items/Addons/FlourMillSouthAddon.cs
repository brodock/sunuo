using System;
using Server;

namespace Server.Items
{
	public class FlourMillSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new FlourMillSouthDeed(); } }

		[Constructable]
		public FlourMillSouthAddon()
		{
			AddComponent( new AddonComponent( 0x192E ), 0, 1, 0 );
			AddComponent( new AddonComponent( 0x192C ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0x1930 ), 0, 2, 0 );
		}

		public FlourMillSouthAddon( Serial serial ) : base( serial )
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

	public class FlourMillSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new FlourMillSouthAddon(); } }
		public override int LabelNumber{ get{ return 1044348; } } // flour mill (south)

		[Constructable]
		public FlourMillSouthDeed()
		{
		}

		public FlourMillSouthDeed( Serial serial ) : base( serial )
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