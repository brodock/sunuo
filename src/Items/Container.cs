/***************************************************************************
 *                               Container.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Container.cs,v 1.9 2005/01/22 04:25:04 krrios Exp $
 *   $Author: krrios $
 *   $Date: 2005/01/22 04:25:04 $
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Collections;
using Server.Network;

namespace Server.Items
{
	public delegate void OnItemConsumed( Item item, int amount );
	public delegate int CheckItemGroup( Item a, Item b );

	public delegate void ContainerSnoopHandler( Container cont, Mobile from );

	public class Container : Item
	{
		private static ContainerSnoopHandler m_SnoopHandler;

		public static ContainerSnoopHandler SnoopHandler
		{
			get{ return m_SnoopHandler; }
			set{ m_SnoopHandler = value; }
		}

		private int m_DropSound;
		private int m_GumpID;
		private int m_MaxItems;

		[CommandProperty( AccessLevel.GameMaster )]
		public int GumpID
		{
			get{ return ( m_GumpID == -1 ? DefaultGumpID : m_GumpID ); }
			set{ m_GumpID = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int DropSound
		{
			get{ return ( m_DropSound == -1 ? DefaultDropSound : m_DropSound ); }
			set{ m_DropSound = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxItems
		{
			get{ return ( m_MaxItems == -1 ? DefaultMaxItems : m_MaxItems ); }
			set{ m_MaxItems = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int MaxWeight
		{
			get
			{
				int maxWeight;

				if ( Parent is Container )
				{
					maxWeight = ((Container)Parent).MaxWeight;

					if ( maxWeight > 0 )
						maxWeight = Math.Max( maxWeight, DefaultMaxWeight );
				}
				else
				{
					maxWeight = DefaultMaxWeight;
				}

				return maxWeight;
			}
		}

		public virtual Rectangle2D Bounds{ get{ return new Rectangle2D( 44, 65, 142, 94 ); } }
		public virtual int DefaultGumpID{ get{ return 0x3C; } }
		public virtual int DefaultDropSound{ get{ return 0x48; } }
		public virtual int DefaultMaxItems{ get{ return m_GlobalMaxItems; } }
		public virtual int DefaultMaxWeight{ get{ return m_GlobalMaxWeight; } }

		public virtual bool CanStore( Mobile m )
		{
			return Movable || IsLockedDown || IsSecure || m == Parent;
		}

		public virtual int GetDroppedSound( Item item )
		{
			int dropSound = item.GetDropSound();

			return dropSound != -1 ? dropSound : DropSound;
		}

		public override void OnSnoop( Mobile from )
		{
			if ( m_SnoopHandler != null )
				m_SnoopHandler( this, from );
		}

		public bool CheckHold( Mobile m, Item item, bool message )
		{
			return CheckHold( m, item, message, true, 0, 0 );
		}

		public bool CheckHold( Mobile m, Item item, bool message, bool checkItems )
		{
			return CheckHold( m, item, message, checkItems, 0, 0 );
		}

		public virtual bool CheckHold( Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight )
		{
			if ( m.AccessLevel < AccessLevel.GameMaster )
			{
				if ( !CanStore( m ) )
				{
					if ( message )
						SendCantStoreMessage( m, item );

					return false;
				}

				int maxItems = this.MaxItems;

				if ( checkItems && maxItems != 0 && (this.TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems )
				{
					if ( message )
						SendFullItemsMessage( m, item );

					return false;
				}
				else
				{
					int maxWeight = this.MaxWeight;

					if ( maxWeight != 0 && (this.TotalWeight + plusWeight + item.TotalWeight + item.PileWeight) > maxWeight )
					{
						if ( message )
							SendFullWeightMessage( m, item );

						return false;
					}
				}
			}

			object parent = this.Parent;

			while ( parent != null )
			{
				if ( parent is Container )
					return ((Container)parent).CheckHold( m, item, message, checkItems, plusItems, plusWeight );
				else if ( parent is Item )
					parent = ((Item)parent).Parent;
				else
					break;
			}

			return true;
		}

		public virtual void SendFullItemsMessage( Mobile to, Item item )
		{
			to.SendMessage( "That container cannot hold more items." );
		}

		public virtual void SendFullWeightMessage( Mobile to, Item item )
		{
			to.SendMessage( "That container cannot hold more weight." );
		}

		public virtual void SendCantStoreMessage( Mobile to, Item item )
		{
			to.SendLocalizedMessage( 500176 ); // That is not your container, you can't store things here.
		}

		public virtual bool OnDragDropInto( Mobile from, Item item, Point3D p )
		{
			if ( !CheckHold( from, item, true, true ) )
				return false;

			item.Location = new Point3D( p.m_X, p.m_Y, 0 );
			AddItem( item );

			from.SendSound( GetDroppedSound( item ), GetWorldLocation() );

			return true;
		}

		private class GroupComparer : IComparer
		{
			private CheckItemGroup m_Grouper;

			public GroupComparer( CheckItemGroup grouper )
			{
				m_Grouper = grouper;
			}

			public int Compare( object x, object y )
			{
				Item a = (Item)x;
				Item b = (Item)y;

				return m_Grouper( a, b );
			}
		}

		public bool ConsumeTotalGrouped( Type type, int amount, bool recurse, OnItemConsumed callback, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			Item[] typedItems = FindItemsByType( type, recurse );

			ArrayList groups = new ArrayList();
			int idx = 0;

			while ( idx < typedItems.Length )
			{
				Item a = typedItems[idx++];
				ArrayList group = new ArrayList();

				group.Add( a );

				while ( idx < typedItems.Length )
				{
					Item b = typedItems[idx];
					int v = grouper( a, b );

					if ( v == 0 )
						group.Add( b );
					else
						break;

					++idx;
				}

				groups.Add( group );
			}

			Item[][] items = new Item[groups.Count][];
			int[] totals = new int[groups.Count];

			bool hasEnough = false;

			for ( int i = 0; i < groups.Count; ++i )
			{
				items[i] = (Item[])(((ArrayList)groups[i]).ToArray( typeof( Item ) ));

				for ( int j = 0; j < items[i].Length; ++j )
					totals[i] += items[i][j].Amount;

				if ( totals[i] >= amount )
					hasEnough = true;
			}

			if ( !hasEnough )
				return false;

			for ( int i = 0; i < items.Length; ++i )
			{
				if ( totals[i] >= amount )
				{
					int need = amount;

					for ( int j = 0; j < items[i].Length; ++j )
					{
						Item item = items[i][j];

						int theirAmount = item.Amount;

						if ( theirAmount < need )
						{
							if ( callback != null )
								callback( item, theirAmount );

							item.Delete();
							need -= theirAmount;
						}
						else
						{
							if ( callback != null )
								callback( item, need );

							item.Consume( need );
							break;
						}
					}

					break;
				}
			}

			return true;
		}

		public int ConsumeTotalGrouped( Type[] types, int[] amounts, bool recurse, OnItemConsumed callback, CheckItemGroup grouper )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();
			else if ( grouper == null )
				throw new ArgumentNullException();

			Item[][][] items = new Item[types.Length][][];
			int[][] totals = new int[types.Length][];

			for ( int i = 0; i < types.Length; ++i )
			{
				Item[] typedItems = FindItemsByType( types[i], recurse );

				ArrayList groups = new ArrayList();
				int idx = 0;

				while ( idx < typedItems.Length )
				{
					Item a = typedItems[idx++];
					ArrayList group = new ArrayList();

					group.Add( a );

					while ( idx < typedItems.Length )
					{
						Item b = typedItems[idx];
						int v = grouper( a, b );

						if ( v == 0 )
							group.Add( b );
						else
							break;

						++idx;
					}

					groups.Add( group );
				}

				items[i] = new Item[groups.Count][];
				totals[i] = new int[groups.Count];

				bool hasEnough = false;

				for ( int j = 0; j < groups.Count; ++j )
				{
					items[i][j] = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));

					for ( int k = 0; k < items[i][j].Length; ++k )
						totals[i][j] += items[i][j][k].Amount;

					if ( totals[i][j] >= amounts[i] )
						hasEnough = true;
				}

				if ( !hasEnough )
					return i;
			}

			for ( int i = 0; i < items.Length; ++i )
			{
				for ( int j = 0; j < items[i].Length; ++j )
				{
					if ( totals[i][j] >= amounts[i] )
					{
						int need = amounts[i];

						for ( int k = 0; k < items[i][j].Length; ++k )
						{
							Item item = items[i][j][k];

							int theirAmount = item.Amount;

							if ( theirAmount < need )
							{
								if ( callback != null )
									callback( item, theirAmount );

								item.Delete();
								need -= theirAmount;
							}
							else
							{
								if ( callback != null )
									callback( item, need );

								item.Consume( need );
								break;
							}
						}

						break;
					}
				}
			}

			return -1;
		}

		public int GetBestGroupAmount( Type type, bool recurse, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			int best = 0;

			Item[] typedItems = FindItemsByType( type, recurse );

			ArrayList groups = new ArrayList();
			int idx = 0;

			while ( idx < typedItems.Length )
			{
				Item a = typedItems[idx++];
				ArrayList group = new ArrayList();

				group.Add( a );

				while ( idx < typedItems.Length )
				{
					Item b = typedItems[idx];
					int v = grouper( a, b );

					if ( v == 0 )
						group.Add( b );
					else
						break;

					++idx;
				}

				groups.Add( group );
			}

			for ( int i = 0; i < groups.Count; ++i )
			{
				Item[] items = (Item[])(((ArrayList)groups[i]).ToArray( typeof( Item ) ));
				int total = 0;

				for ( int j = 0; j < items.Length; ++j )
					total += items[j].Amount;

				if ( total >= best )
					best = total;
			}

			return best;
		}

		public int GetBestGroupAmount( Type[] types, bool recurse, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			int best = 0;

			Item[] typedItems = FindItemsByType( types, recurse );

			ArrayList groups = new ArrayList();
			int idx = 0;

			while ( idx < typedItems.Length )
			{
				Item a = typedItems[idx++];
				ArrayList group = new ArrayList();

				group.Add( a );

				while ( idx < typedItems.Length )
				{
					Item b = typedItems[idx];
					int v = grouper( a, b );

					if ( v == 0 )
						group.Add( b );
					else
						break;

					++idx;
				}

				groups.Add( group );
			}

			for ( int j = 0; j < groups.Count; ++j )
			{
				Item[] items = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));
				int total = 0;

				for ( int k = 0; k < items.Length; ++k )
					total += items[k].Amount;

				if ( total >= best )
					best = total;
			}

			return best;
		}

		public int GetBestGroupAmount( Type[][] types, bool recurse, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			int best = 0;

			for ( int i = 0; i < types.Length; ++i )
			{
				Item[] typedItems = FindItemsByType( types[i], recurse );

				ArrayList groups = new ArrayList();
				int idx = 0;

				while ( idx < typedItems.Length )
				{
					Item a = typedItems[idx++];
					ArrayList group = new ArrayList();

					group.Add( a );

					while ( idx < typedItems.Length )
					{
						Item b = typedItems[idx];
						int v = grouper( a, b );

						if ( v == 0 )
							group.Add( b );
						else
							break;

						++idx;
					}

					groups.Add( group );
				}

				for ( int j = 0; j < groups.Count; ++j )
				{
					Item[] items = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));
					int total = 0;

					for ( int k = 0; k < items.Length; ++k )
						total += items[k].Amount;

					if ( total >= best )
						best = total;
				}
			}

			return best;
		}

		public int ConsumeTotalGrouped( Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback, CheckItemGroup grouper )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();
			else if ( grouper == null )
				throw new ArgumentNullException();

			Item[][][] items = new Item[types.Length][][];
			int[][] totals = new int[types.Length][];

			for ( int i = 0; i < types.Length; ++i )
			{
				Item[] typedItems = FindItemsByType( types[i], recurse );

				ArrayList groups = new ArrayList();
				int idx = 0;

				while ( idx < typedItems.Length )
				{
					Item a = typedItems[idx++];
					ArrayList group = new ArrayList();

					group.Add( a );

					while ( idx < typedItems.Length )
					{
						Item b = typedItems[idx];
						int v = grouper( a, b );

						if ( v == 0 )
							group.Add( b );
						else
							break;

						++idx;
					}

					groups.Add( group );
				}

				items[i] = new Item[groups.Count][];
				totals[i] = new int[groups.Count];

				bool hasEnough = false;

				for ( int j = 0; j < groups.Count; ++j )
				{
					items[i][j] = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));

					for ( int k = 0; k < items[i][j].Length; ++k )
						totals[i][j] += items[i][j][k].Amount;

					if ( totals[i][j] >= amounts[i] )
						hasEnough = true;
				}

				if ( !hasEnough )
					return i;
			}

			for ( int i = 0; i < items.Length; ++i )
			{
				for ( int j = 0; j < items[i].Length; ++j )
				{
					if ( totals[i][j] >= amounts[i] )
					{
						int need = amounts[i];

						for ( int k = 0; k < items[i][j].Length; ++k )
						{
							Item item = items[i][j][k];

							int theirAmount = item.Amount;

							if ( theirAmount < need )
							{
								if ( callback != null )
									callback( item, theirAmount );

								item.Delete();
								need -= theirAmount;
							}
							else
							{
								if ( callback != null )
									callback( item, need );

								item.Consume( need );
								break;
							}
						}

						break;
					}
				}
			}

			return -1;
		}

		public int ConsumeTotal( Type[][] types, int[] amounts )
		{
			return ConsumeTotal( types, amounts, true, null );
		}

		public int ConsumeTotal( Type[][] types, int[] amounts, bool recurse )
		{
			return ConsumeTotal( types, amounts, recurse, null );
		}

		public int ConsumeTotal( Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();

			Item[][] items = new Item[types.Length][];
			int[] totals = new int[types.Length];

			for ( int i = 0; i < types.Length; ++i )
			{
				items[i] = FindItemsByType( types[i], recurse );

				for ( int j = 0; j < items[i].Length; ++j )
					totals[i] += items[i][j].Amount;

				if ( totals[i] < amounts[i] )
					return i;
			}

			for ( int i = 0; i < types.Length; ++i )
			{
				int need = amounts[i];

				for ( int j = 0; j < items[i].Length; ++j )
				{
					Item item = items[i][j];

					int theirAmount = item.Amount;

					if ( theirAmount < need )
					{
						if ( callback != null )
							callback( item, theirAmount );

						item.Delete();
						need -= theirAmount;
					}
					else
					{
						if ( callback != null )
							callback( item, need );

						item.Consume( need );
						break;
					}
				}
			}

			return -1;
		}



		public int ConsumeTotal( Type[] types, int[] amounts )
		{
			return ConsumeTotal( types, amounts, true, null );
		}

		public int ConsumeTotal( Type[] types, int[] amounts, bool recurse )
		{
			return ConsumeTotal( types, amounts, recurse, null );
		}

		public int ConsumeTotal( Type[] types, int[] amounts, bool recurse, OnItemConsumed callback )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();

			Item[][] items = new Item[types.Length][];
			int[] totals = new int[types.Length];

			for ( int i = 0; i < types.Length; ++i )
			{
				items[i] = FindItemsByType( types[i], recurse );

				for ( int j = 0; j < items[i].Length; ++j )
					totals[i] += items[i][j].Amount;

				if ( totals[i] < amounts[i] )
					return i;
			}

			for ( int i = 0; i < types.Length; ++i )
			{
				int need = amounts[i];

				for ( int j = 0; j < items[i].Length; ++j )
				{
					Item item = items[i][j];

					int theirAmount = item.Amount;

					if ( theirAmount < need )
					{
						if ( callback != null )
							callback( item, theirAmount );

						item.Delete();
						need -= theirAmount;
					}
					else
					{
						if ( callback != null )
							callback( item, need );

						item.Consume( need );
						break;
					}
				}
			}

			return -1;
		}

		public int GetAmount( Type type )
		{
			return GetAmount( type, true );
		}

		public int GetAmount( Type type, bool recurse )
		{
			Item[] items = FindItemsByType( type, recurse );

			int amount = 0;

			for ( int i = 0; i < items.Length; ++i )
				amount += items[i].Amount;

			return amount;
		}

		public int GetAmount( Type[] types )
		{
			return GetAmount( types, true );
		}

		public int GetAmount( Type[] types, bool recurse )
		{
			Item[] items = FindItemsByType( types, recurse );

			int amount = 0;

			for ( int i = 0; i < items.Length; ++i )
				amount += items[i].Amount;

			return amount;
		}

		public bool ConsumeTotal( Type type, int amount )
		{
			return ConsumeTotal( type, amount, true, null );
		}

		public bool ConsumeTotal( Type type, int amount, bool recurse )
		{
			return ConsumeTotal( type, amount, recurse, null );
		}

		public bool ConsumeTotal( Type type, int amount, bool recurse, OnItemConsumed callback )
		{
			Item[] items = FindItemsByType( type, recurse );

			// First pass, compute total
			int total = 0;

			for ( int i = 0; i < items.Length; ++i )
				total += items[i].Amount;

			if ( total >= amount )
			{
				// We've enough, so consume it

				int need = amount;

				for ( int i = 0; i < items.Length; ++i )
				{
					Item item = items[i];

					int theirAmount = item.Amount;

					if ( theirAmount < need )
					{
						if ( callback != null )
							callback( item, theirAmount );

						item.Delete();
						need -= theirAmount;
					}
					else
					{
						if ( callback != null )
							callback( item, need );

						item.Consume( need );

						return true;
					}
				}
			}

			return false;
		}

		public int ConsumeUpTo( Type type, int amount )
		{
			return ConsumeUpTo( type, amount, true );
		}

		public int ConsumeUpTo( Type type, int amount, bool recurse )
		{
			int consumed = 0;

			Queue toDelete = new Queue();

			RecurseConsumeUpTo( this, type, amount, recurse, ref consumed, toDelete );

			while ( toDelete.Count > 0 )
				((Item)toDelete.Dequeue()).Delete();

			return consumed;
		}

		private static void RecurseConsumeUpTo( Item current, Type type, int amount, bool recurse, ref int consumed, Queue toDelete )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				ArrayList list = current.Items;

				for ( int i = 0; i < list.Count; ++i )
				{
					Item item = (Item)list[i];

					if ( type.IsAssignableFrom( item.GetType() ) )
					{
						int need = amount - consumed;
						int theirAmount = item.Amount;

						if ( theirAmount <= need )
						{
							toDelete.Enqueue( item );
							consumed += theirAmount;
						}
						else
						{
							item.Amount -= need;
							consumed += need;

							return;
						}
					}
					else if ( recurse && item is Container )
					{
						RecurseConsumeUpTo( item, type, amount, recurse, ref consumed, toDelete );
					}
				}
			}
		}

		private static ArrayList m_FindItemsList = new ArrayList();

		public Item[] FindItemsByType( Type type )
		{
			return FindItemsByType( type, true );
		}

		public Item[] FindItemsByType( Type type, bool recurse )
		{
			if ( m_FindItemsList.Count > 0 )
				m_FindItemsList.Clear();

			RecurseFindItemsByType( this, type, recurse, m_FindItemsList );

			return (Item[])m_FindItemsList.ToArray( typeof( Item ) );
		}

		private static void RecurseFindItemsByType( Item current, Type type, bool recurse, ArrayList list )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				ArrayList items = current.Items;

				for ( int i = 0; i < items.Count; ++i )
				{
					Item item = (Item)items[i];

					if ( type.IsAssignableFrom( item.GetType() ) )// item.GetType().IsAssignableFrom( type ) )
						list.Add( item );

					if ( recurse && item is Container )
						RecurseFindItemsByType( item, type, recurse, list );
				}
			}
		}

		public Item[] FindItemsByType( Type[] types )
		{
			return FindItemsByType( types, true );
		}

		public Item[] FindItemsByType( Type[] types, bool recurse )
		{
			if ( m_FindItemsList.Count > 0 )
				m_FindItemsList.Clear();

			RecurseFindItemsByType( this, types, recurse, m_FindItemsList );

			return (Item[])m_FindItemsList.ToArray( typeof( Item ) );
		}

		private static void RecurseFindItemsByType( Item current, Type[] types, bool recurse, ArrayList list )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				ArrayList items = current.Items;

				for ( int i = 0; i < items.Count; ++i )
				{
					Item item = (Item)items[i];

					if ( InTypeList( item, types ) )
						list.Add( item );

					if ( recurse && item is Container )
						RecurseFindItemsByType( item, types, recurse, list );
				}
			}
		}

		public Item FindItemByType( Type type )
		{
			return FindItemByType( type, true );
		}

		public Item FindItemByType( Type type, bool recurse )
		{
			return RecurseFindItemByType( this, type, recurse );
		}

		private static Item RecurseFindItemByType( Item current, Type type, bool recurse )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				ArrayList list = current.Items;

				for ( int i = 0; i < list.Count; ++i )
				{
					Item item = (Item)list[i];

					if ( type.IsAssignableFrom( item.GetType() ) )
					{
						return item;
					}
					else if ( recurse && item is Container )
					{
						Item check = RecurseFindItemByType( item, type, recurse );

						if ( check != null )
							return check;
					}
				}
			}

			return null;
		}

		public Item FindItemByType( Type[] types )
		{
			return FindItemByType( types, true );
		}

		public Item FindItemByType( Type[] types, bool recurse )
		{
			return RecurseFindItemByType( this, types, recurse );
		}

		private static Item RecurseFindItemByType( Item current, Type[] types, bool recurse )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				ArrayList list = current.Items;

				for ( int i = 0; i < list.Count; ++i )
				{
					Item item = (Item)list[i];

					if ( InTypeList( item, types ) )
					{
						return item;
					}
					else if ( recurse && item is Container )
					{
						Item check = RecurseFindItemByType( item, types, recurse );

						if ( check != null )
							return check;
					}
				}
			}

			return null;
		}

		private static bool InTypeList( Item item, Type[] types )
		{
			Type t = item.GetType();

			for ( int i = 0; i < types.Length; ++i )
				if ( types[i].IsAssignableFrom( t ) )
					return true;

			return false;
		}

		private static void SetSaveFlag( ref SaveFlag flags, SaveFlag toSet, bool setIf )
		{
			if ( setIf )
				flags |= toSet;
		}

		private static bool GetSaveFlag( SaveFlag flags, SaveFlag toGet )
		{
			return ( (flags & toGet) != 0 );
		}

		[Flags]
		private enum SaveFlag : byte
		{
			None					= 0x00000000,
			MaxItems				= 0x00000001,
			GumpID					= 0x00000002,
			DropSound				= 0x00000004,
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 ); // version

			SaveFlag flags = SaveFlag.None;

			SetSaveFlag( ref flags, SaveFlag.MaxItems,		m_MaxItems != -1 );
			SetSaveFlag( ref flags, SaveFlag.GumpID,		m_GumpID != -1 );
			SetSaveFlag( ref flags, SaveFlag.DropSound,		m_DropSound != -1 );

			writer.Write( (byte) flags );

			if ( GetSaveFlag( flags, SaveFlag.MaxItems ) )
				writer.WriteEncodedInt( (int) m_MaxItems );

			if ( GetSaveFlag( flags, SaveFlag.GumpID ) )
				writer.WriteEncodedInt( (int) m_GumpID );

			if ( GetSaveFlag( flags, SaveFlag.DropSound ) )
				writer.WriteEncodedInt( (int) m_DropSound );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
				{
					SaveFlag flags = (SaveFlag)reader.ReadByte();

					if ( GetSaveFlag( flags, SaveFlag.MaxItems ) )
						m_MaxItems = reader.ReadEncodedInt();
					else
						m_MaxItems = -1;

					if ( GetSaveFlag( flags, SaveFlag.GumpID ) )
						m_GumpID = reader.ReadEncodedInt();
					else
						m_GumpID = -1;

					if ( GetSaveFlag( flags, SaveFlag.DropSound ) )
						m_DropSound = reader.ReadEncodedInt();
					else
						m_DropSound = -1;

					break;
				}
				case 1:
				{
					m_MaxItems = reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					if ( version < 1 )
						m_MaxItems = m_GlobalMaxItems;

					m_GumpID = reader.ReadInt();
					m_DropSound = reader.ReadInt();

					if ( m_GumpID == DefaultGumpID )
						m_GumpID = -1;

					if ( m_DropSound == DefaultDropSound )
						m_DropSound = -1;

					if ( m_MaxItems == DefaultMaxItems )
						m_MaxItems = -1;

					//m_Bounds = new Rectangle2D( reader.ReadPoint2D(), reader.ReadPoint2D() );
					reader.ReadPoint2D();
					reader.ReadPoint2D();

					break;
				}
			}
		}

		private static int m_GlobalMaxItems = 125;
		private static int m_GlobalMaxWeight = 400;

		public static int GlobalMaxItems{ get{ return m_GlobalMaxItems; } set{ m_GlobalMaxItems = value; } }
		public static int GlobalMaxWeight{ get{ return m_GlobalMaxWeight; } set{ m_GlobalMaxWeight = value; } }

		public Container( int itemID ) : base( itemID )
		{
			m_GumpID = -1;
			m_DropSound = -1;
			m_MaxItems = -1;
		}

		public Container( Serial serial ) : base( serial )
		{
		}

		public virtual bool OnStackAttempt( Mobile from, Item stack, Item dropped )
		{
			if ( !CheckHold( from, dropped, true, false ) )
				return false;

			return stack.StackWith( from, dropped );
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( TryDropItem( from, dropped, true ) )
			{
				from.SendSound( GetDroppedSound( dropped ), GetWorldLocation() );

				return true;
			}
			else
			{
				return false;
			}
		}

		public virtual bool TryDropItem( Mobile from, Item dropped, bool sendFullMessage )
		{
			if ( !CheckHold( from, dropped, sendFullMessage, true ) )
				return false;

			ArrayList list = this.Items;

			for ( int i = 0; i < list.Count; ++i )
			{
				Item item = (Item)list[i];

				if ( !(item is Container) && item.StackWith( from, dropped, false ) )
					return true;
			}

			DropItem( dropped );

			return true;
		}

		public virtual void Destroy()
		{
			Point3D loc = GetWorldLocation();
			Map map = Map;

			for ( int i = Items.Count - 1; i >= 0; --i )
			{
				if ( i < Items.Count )
				{
					((Item)Items[i]).SetLastMoved();
					((Item)Items[i]).MoveToWorld( loc, map );
				}
			}

			Delete();
		}

		public virtual void DropItem( Item dropped )
		{
			AddItem( dropped );

			Rectangle2D bounds = dropped.GetGraphicBounds();
			Rectangle2D ourBounds = this.Bounds;

			int x, y;

			if ( bounds.Width >= ourBounds.Width )
				x = (ourBounds.Width - bounds.Width) / 2;
			else
				x = Utility.Random( ourBounds.Width - bounds.Width );

			if ( bounds.Height >= ourBounds.Height )
				y = (ourBounds.Height - bounds.Height) / 2;
			else
				y = Utility.Random( ourBounds.Height - bounds.Height );

			x += ourBounds.X;
			x -= bounds.X;

			y += ourBounds.Y;
			y -= bounds.Y;

			dropped.Location = new Point3D( x, y, 0 );
		}

		public override void OnDoubleClickSecureTrade( Mobile from )
		{
			if ( from.InRange( GetWorldLocation(), 2 ) )
			{
				DisplayTo( from );

				SecureTradeContainer cont = GetSecureTradeCont();

				if ( cont != null )
				{
					SecureTrade trade = cont.Trade;

					if ( trade != null && trade.From.Mobile == from )
						DisplayTo( trade.To.Mobile );
					else if ( trade != null && trade.To.Mobile == from )
						DisplayTo( trade.From.Mobile );
				}
			}
			else
			{
				from.SendLocalizedMessage( 500446 ); // That is too far away.
			}
		}

		public virtual bool DisplaysContent{ get{ return true; } }

		public virtual bool CheckContentDisplay( Mobile from )
		{
			if ( !DisplaysContent )
				return false;

			object root = this.RootParent;

			if ( root == null || root is Item || root == from || from.AccessLevel > AccessLevel.Player )
				return true;

			return false;
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( CheckContentDisplay( from ) )
				LabelTo( from, "({0} items, {1} stones)", TotalItems, TotalWeight );
		}

		private ArrayList m_Openers;

		public ArrayList Openers
		{
			get{ return m_Openers; }
			set{ m_Openers = value; }
		}

		public virtual bool IsPublicContainer{ get{ return false; } }

		public override void OnDelete()
		{
			base.OnDelete();

			m_Openers = null;
		}

		public virtual void DisplayTo( Mobile to )
		{
			if ( !IsPublicContainer )
			{
				bool contains = false;

				if ( m_Openers != null )
				{
					Point3D worldLoc = GetWorldLocation();
					Map map = this.Map;

					for ( int i = 0; i < m_Openers.Count; ++i )
					{
						Mobile mob = (Mobile)m_Openers[i];

						if ( mob == to )
						{
							contains = true;
						}
						else
						{
							int range = GetUpdateRange( mob );

							if ( mob.Map != map || !mob.InRange( worldLoc, range ) )
								m_Openers.RemoveAt( i-- );
						}
					}
				}

				if ( !contains )
				{
					if ( m_Openers == null )
						m_Openers = new ArrayList( 4 );

					m_Openers.Add( to );
				}
				else if ( m_Openers != null && m_Openers.Count == 0 )
				{
					m_Openers = null;
				}
			}

			to.Send( new ContainerDisplay( this ) );
			to.Send( new ContainerContent( to, this ) );

			if ( ObjectPropertyList.Enabled )
			{
				ArrayList items = this.Items;

				for ( int i = 0; i < items.Count; ++i )
					to.Send( ((Item)items[i]).OPLPacket );
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( DisplaysContent )//CheckContentDisplay( from ) )
				list.Add( 1050044, "{0}\t{1}", TotalItems, TotalWeight );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel > AccessLevel.Player || from.InRange( this.GetWorldLocation(), 2 ) )
				DisplayTo( from );
			else
				from.SendLocalizedMessage( 500446 ); // That is too far away.
		}
	}
}