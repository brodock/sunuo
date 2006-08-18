using System;
using Server.Network;

namespace Server.Items
{
	public class FruitBasket : Food
	{
		[Constructable]
		public FruitBasket() : base( 1, 0x993 )
		{
			Weight = 2.0;
			FillFactor = 5;
			Stackable = false;
		}

		public FruitBasket( Serial serial ) : base( serial )
		{
		}

		public override bool Eat( Mobile from )
		{
			if ( !base.Eat( from ) )
				return false;

			from.AddToBackpack( new Basket() );
			return true;
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

	[FlipableAttribute( 0x171f, 0x1720 )]
	public class Banana : Food
	{
		[Constructable]
		public Banana() : this( 1 )
		{
		}

		[Constructable]
		public Banana( int amount ) : base( amount, 0x171f )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Banana( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Banana(), amount );
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

	[Flipable( 0x1721, 0x1722 )]
	public class Bananas : Food
	{
		[Constructable]
		public Bananas() : this( 1 )
		{
		}

		[Constructable]
		public Bananas( int amount ) : base( amount, 0x1721 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Bananas( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Bananas(), amount );
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

	public class SplitCoconut : Food
	{
		[Constructable]
		public SplitCoconut() : this( 1 )
		{
		}

		[Constructable]
		public SplitCoconut( int amount ) : base( amount, 0x1725 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public SplitCoconut( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new SplitCoconut(), amount );
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

	public class Lemon : Food
	{
		[Constructable]
		public Lemon() : this( 1 )
		{
		}

		[Constructable]
		public Lemon( int amount ) : base( amount, 0x1728 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Lemon( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Lemon(), amount );
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

	public class Lime : Food
	{
		[Constructable]
		public Lime() : this( 1 )
		{
		}

		[Constructable]
		public Lime( int amount ) : base( amount, 0x172a )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Lime( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Lime(), amount );
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

	public class Coconut : Food
	{
		[Constructable]
		public Coconut() : this( 1 )
		{
		}

		[Constructable]
		public Coconut( int amount ) : base( amount, 0x1726 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Coconut( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Coconut(), amount );
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

	public class Dates : Food
	{
		[Constructable]
		public Dates() : this( 1 )
		{
		}

		[Constructable]
		public Dates( int amount ) : base( amount, 0x1727 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Dates( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Dates(), amount );
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

	public class Grapes : Food
	{
		[Constructable]
		public Grapes() : this( 1 )
		{
		}

		[Constructable]
		public Grapes( int amount ) : base( amount, 0x9D1 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Grapes( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Grapes(), amount );
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

	public class Peach : Food
	{
		[Constructable]
		public Peach() : this( 1 )
		{
		}

		[Constructable]
		public Peach( int amount ) : base( amount, 0x9D2 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Peach( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Peach(), amount );
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

	public class Pear : Food
	{
		[Constructable]
		public Pear() : this( 1 )
		{
		}

		[Constructable]
		public Pear( int amount ) : base( amount, 0x994 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Pear( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Pear(), amount );
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

	public class Apple : Food
	{
		[Constructable]
		public Apple() : this( 1 )
		{
		}

		[Constructable]
		public Apple( int amount ) : base( amount, 0x9D0 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Apple( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Apple(), amount );
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

	[FlipableAttribute( 0xc5c, 0xc5d )]
	public class Watermelon : Food
	{
		[Constructable]
		public Watermelon() : this( 1 )
		{
		}

		[Constructable]
		public Watermelon( int amount ) : base( amount, 0xc5c )
		{
			this.Weight = 2.0;
			this.FillFactor = 2;
		}

		public Watermelon( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Watermelon(), amount );
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

	[FlipableAttribute( 0xc72, 0xc73 )]
	public class Squash : Food
	{
		[Constructable]
		public Squash() : this( 1 )
		{
		}

		[Constructable]
		public Squash( int amount ) : base( amount, 0xc72 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Squash( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Squash(), amount );
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

	[FlipableAttribute( 0xc79, 0xc7a )]
	public class Cantaloupe : Food
	{
		[Constructable]
		public Cantaloupe() : this( 1 )
		{
		}

		[Constructable]
		public Cantaloupe( int amount ) : base( amount, 0xc79 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Cantaloupe( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Cantaloupe(), amount );
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