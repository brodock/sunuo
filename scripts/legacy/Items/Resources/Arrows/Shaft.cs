using System;
using Server.Items;

namespace Server.Items
{
	public class Shaft : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} shaft" : "{0} shafts", Amount );
			}
		}

		[Constructable]
		public Shaft() : this( 1 )
		{
		}

		[Constructable]
		public Shaft( int amount ) : base( 0x1BD4 )
		{
			Stackable = true;
			Weight = 0.1;
			Amount = amount;
		}

		public Shaft( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Shaft( amount ), amount );
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