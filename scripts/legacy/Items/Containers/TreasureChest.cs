using Server;
using Server.Items;
using Server.Multis;
using Server.Network;
using System;

namespace Server.Items
{
	[FlipableAttribute( 0xe43, 0xe42 )] 
	public class WoodenTreasureChest : BaseTreasureChest 
	{ 
		public override int DefaultGumpID{ get{ return 0x49; } }
		public override int DefaultDropSound{ get{ return 0x42; } }

		public override Rectangle2D Bounds
		{
			get{ return new Rectangle2D( 20, 105, 150, 180 ); }
		}

		[Constructable] 
		public WoodenTreasureChest() : base( 0xE43 ) 
		{ 
		} 

		public WoodenTreasureChest( Serial serial ) : base( serial ) 
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

	[FlipableAttribute( 0xe41, 0xe40 )] 
	public class MetalGoldenTreasureChest : BaseTreasureChest 
	{ 
		public override int DefaultGumpID{ get{ return 0x42; } }
		public override int DefaultDropSound{ get{ return 0x42; } }

		public override Rectangle2D Bounds
		{
			get{ return new Rectangle2D( 20, 105, 150, 180 ); }
		}

		[Constructable] 
		public MetalGoldenTreasureChest() : base( 0xE41 ) 
		{ 
		} 

		public MetalGoldenTreasureChest( Serial serial ) : base( serial ) 
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

	[FlipableAttribute( 0x9ab, 0xe7c )] 
	public class MetalTreasureChest : BaseTreasureChest 
	{ 
		public override int DefaultGumpID{ get{ return 0x4A; } }
		public override int DefaultDropSound{ get{ return 0x42; } }

		public override Rectangle2D Bounds
		{
			get{ return new Rectangle2D( 20, 105, 150, 180 ); }
		}

		[Constructable] 
		public MetalTreasureChest() : base( 0x9AB ) 
		{ 
		} 

		public MetalTreasureChest( Serial serial ) : base( serial ) 
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