using System;
using Server;

namespace Server.Items
{
	public class Amber : Item
	{
		[Constructable]
		public Amber() : this( 1 )
		{
		}

		[Constructable]
		public Amber( int amount ) : base( 0xF25 )
		{
			Stackable = true;
			Weight = 0.1;
			Amount = amount;
		}

		public Amber( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Amber( amount ), amount );
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