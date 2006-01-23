using System;
using System.Collections;
using Server.StringQueries;

namespace Server.StringQueries
{
	
    
	public class StringQueryCollection : CollectionBase
	{

		public StringQueryCollection() : 
			base()
		{
		}
        
		public StringQuery this[int index]
		{
			get
			{
				return ( (StringQuery)( this.List[index] ) );
			}
			set
			{
				this.List[index] = value;
			}
		}

		public int Add( StringQuery value )
		{
			return this.List.Add( value );
		}

		public bool Contains( StringQuery value )
		{
			return this.List.Contains( value );
		}
        
		public int IndexOf( StringQuery value )
		{
			return this.List.IndexOf( value );
		}
        
		public void Remove( StringQuery value )
		{
			this.List.Remove( value );
		}
        
		public new StringQueryEnumerator GetEnumerator()
		{
			return new StringQueryEnumerator(this);
		}
        
		public void Insert( int index, StringQuery value )
		{
			this.List.Insert( index, value );
		}
        
		public class StringQueryEnumerator : IEnumerator
		{
            
			private int _index;
            
			private StringQuery _currentElement;
            
			private StringQueryCollection _collection;
            
			internal StringQueryEnumerator(StringQueryCollection collection)
			{
				_index = -1;
				_collection = collection;
			}
            
			public StringQuery Current
			{
				get
				{
					if (((_index == -1) 
						|| (_index >= _collection.Count)))
					{
						throw new System.IndexOutOfRangeException("Enumerator not started.");
					}
					else
					{
						return _currentElement;
					}
				}
			}
            
			object IEnumerator.Current
			{
				get
				{
					if (((_index == -1) 
						|| (_index >= _collection.Count)))
					{
						throw new System.IndexOutOfRangeException("Enumerator not started.");
					}
					else
					{
						return _currentElement;
					}
				}
			}
            
			public void Reset()
			{
				_index = -1;
				_currentElement = null;
			}
            
			public bool MoveNext()
			{
				if ((_index 
					< (_collection.Count - 1)))
				{
					_index = (_index + 1);
					_currentElement = this._collection[_index];
					return true;
				}
				_index = _collection.Count;
				return false;
			}
		}
	}
}
