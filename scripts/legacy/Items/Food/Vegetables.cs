using System;
using Server.Network;

namespace Server.Items
{
	[FlipableAttribute( 0xc77, 0xc78 )]
	public class Carrot : Food
	{
		[Constructable]
		public Carrot() : this( 1 )
		{
		}

		[Constructable]
		public Carrot( int amount ) : base( amount, 0xc78 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Carrot( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Carrot(), amount );
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

	[FlipableAttribute( 0xc7b, 0xc7c )]
	public class Cabbage : Food
	{
		[Constructable]
		public Cabbage() : this( 1 )
		{
		}

		[Constructable]
		public Cabbage( int amount ) : base( amount, 0xc7b )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Cabbage( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Cabbage(), amount );
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

	[FlipableAttribute( 0xc6d, 0xc6e )]
	public class Onion : Food
	{
		[Constructable]
		public Onion() : this( 1 )
		{
		}

		[Constructable]
		public Onion( int amount ) : base( amount, 0xc6d )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Onion( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Onion(), amount );
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

	[FlipableAttribute( 0xc70, 0xc71 )]
	public class Lettuce : Food
	{
		[Constructable]
		public Lettuce() : this( 1 )
		{
		}

		[Constructable]
		public Lettuce( int amount ) : base( amount, 0xc70 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Lettuce( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Lettuce(), amount );
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

	[FlipableAttribute( 0xc6a, 0xc6b )]
	public class Pumpkin : Food
	{
		[Constructable]
		public Pumpkin() : this( 1 )
		{
		}

		[Constructable]
		public Pumpkin( int amount ) : base( amount, 0xc6a )
		{
			this.Weight = 5.0;
			this.FillFactor = 4;
		}

		public Pumpkin( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Pumpkin(), amount );
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