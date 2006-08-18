using System;
using Server.Network;
using Server.Prompts;
using Server.Items;

namespace Server.Items
{
	public class NameChangeDeed : Item
	{
		[Constructable]
		public NameChangeDeed() : base( 0x14F0 )
		{
			base.Weight = 1.0;
			base.Name = "a name change deed";
		}

		public NameChangeDeed( Serial serial ) : base( serial )
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

		public override void OnDoubleClick( Mobile from )
		{
			// Do namechange
		}
	}
}


