using System;
using Server;

namespace Server.Items
{
	public class Diamond : Item
	{
		[Constructable]
		public Diamond() : this( 1 )
		{
		}

		[Constructable]
		public Diamond( int amount ) : base( 0xF26 )
		{
			Stackable = true;
			Weight = 0.1;
			Amount = amount;
		}

		public Diamond( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Diamond( amount ), amount );
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