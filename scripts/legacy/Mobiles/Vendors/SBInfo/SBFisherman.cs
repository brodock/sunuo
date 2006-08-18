using System; 
using System.Collections; 
using Server.Items; 

namespace Server.Mobiles 
{ 
	public class SBFisherman : SBInfo 
	{ 
		private ArrayList m_BuyInfo = new InternalBuyInfo(); 
		private IShopSellInfo m_SellInfo = new InternalSellInfo(); 

		public SBFisherman() 
		{ 
		} 

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } } 
		public override ArrayList BuyInfo { get { return m_BuyInfo; } } 

		public class InternalBuyInfo : ArrayList 
		{ 
			public InternalBuyInfo() 
			{ 
				Add( new GenericBuyInfo( typeof( RawFishSteak ), 3, 20, 0x97A, 0 ) );
				Add( new GenericBuyInfo( typeof( Fish ), 3, 80, 0x9CC, 0 ) );
				Add( new GenericBuyInfo( typeof( FishingPole ), 15, 20, 0xDC0, 0 ) );
			} 
		} 

		public class InternalSellInfo : GenericSellInfo 
		{ 
			public InternalSellInfo() 
			{ 
				Add( typeof( RawFishSteak ), 1 );
				Add( typeof( Fish ), 1 );
				Add( typeof( FishingPole ), 7 );
			} 
		} 
	} 
}