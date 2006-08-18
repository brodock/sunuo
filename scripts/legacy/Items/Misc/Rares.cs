using System;

namespace Server.Items
{
	[Flipable( 0x14F8, 0x14FA )]
	public class Rope : Item
	{
		[Constructable]
		public Rope() : this( 1 )
		{
		}

		[Constructable]
		public Rope( int amount ) : base( 0x14F8 )
		{
			Stackable = true;
			Weight = 1.0;
			Amount = amount;
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Rope( amount ), amount );
		}

		public Rope( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class IronWire : Item
	{
		[Constructable]
		public IronWire() : this( 1 )
		{
		}

		[Constructable]
		public IronWire( int amount ) : base( 0x1876 )
		{
			Stackable = true;
			Weight = 2.0;
			Amount = amount;
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new IronWire( amount ), amount );
		}

		public IronWire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class SilverWire : Item
	{
		[Constructable]
		public SilverWire() : this( 1 )
		{
		}

		[Constructable]
		public SilverWire( int amount ) : base( 0x1877 )
		{
			Stackable = true;
			Weight = 2.0;
			Amount = amount;
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new SilverWire( amount ), amount );
		}

		public SilverWire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class GoldWire : Item
	{
		[Constructable]
		public GoldWire() : this( 1 )
		{
		}

		[Constructable]
		public GoldWire( int amount ) : base( 0x1878 )
		{
			Stackable = true;
			Weight = 2.0;
			Amount = amount;
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new GoldWire( amount ), amount );
		}

		public GoldWire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class CopperWire : Item
	{
		[Constructable]
		public CopperWire() : this( 1 )
		{
		}

		[Constructable]
		public CopperWire( int amount ) : base( 0x1879 )
		{
			Stackable = true;
			Weight = 2.0;
			Amount = amount;
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new CopperWire( amount ), amount );
		}

		public CopperWire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class Whip : Item
	{
		[Constructable]
		public Whip() : base( 0x166E )
		{
			Weight = 1.0;
		}

		public Whip( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class PaintsAndBrush : Item
	{
		[Constructable]
		public PaintsAndBrush() : base( 0xFC1 )
		{
			Weight = 1.0;
		}

		public PaintsAndBrush( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}