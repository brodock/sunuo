using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
	public class BookOfNinjitsu : Spellbook
	{
		public override SpellbookType SpellbookType{ get{ return SpellbookType.Ninja; } }
		public override int BookOffset{ get{ return 500; } }
		public override int BookCount{ get{ return 8; } }

		public override Item Dupe( int amount )
		{
			Spellbook book = new BookOfNinjitsu();

			book.Content = this.Content;

			return base.Dupe( book, amount );
		}

		[Constructable]
		public BookOfNinjitsu() : this( (ulong)0xFF )
		{
		}

		[Constructable]
		public BookOfNinjitsu( ulong content ) : base( content, 0x23A0 )
		{
			Layer = Layer.Invalid;
		}

		public BookOfNinjitsu( Serial serial ) : base( serial )
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
			Layer = Layer.Invalid;
		}
	}
}