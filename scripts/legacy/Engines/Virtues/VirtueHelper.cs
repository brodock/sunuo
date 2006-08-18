using System;

namespace Server
{
	public enum VirtueLevel
	{
		None,
		Seeker,
		Follower,
		Knight
	}

	public enum VirtueName
	{
		Humility,
		Sacrifice,
		Compassion,
		Spirituality,
		Valor,
		Honor,
		Justice,
		Honest
	}

	public class VirtueHelper
	{
		public static bool HasAny( Mobile from, VirtueName virtue )
		{
			return ( from.Virtues.GetValue( (int)virtue ) > 0 );
		}

		public static bool IsHighestPath( Mobile from, VirtueName virtue )
		{
			return ( from.Virtues.GetValue( (int)virtue ) >= 40 );
		}

		public static VirtueLevel GetLevel( Mobile from, VirtueName virtue )
		{
			int v = from.Virtues.GetValue( (int) virtue ) / 10;

			if ( v < 0 )
				v = 0;
			else if ( v > 3 )
				v = 3;

			return (VirtueLevel)v;
		}

		public static bool Award( Mobile from, VirtueName virtue, int amount, ref bool gainedPath )
		{
			int current = from.Virtues.GetValue( (int)virtue );

			if ( current >= 40 )
				return false;

			if ( (current + amount) >= 40 )
				amount = 40 - current;

			VirtueLevel oldLevel = GetLevel( from, virtue );

			from.Virtues.SetValue( (int)virtue, current + amount );

			gainedPath = ( GetLevel( from, virtue ) != oldLevel );

			return true;
		}

		public static bool Atrophy( Mobile from, VirtueName virtue )
		{
			int current = from.Virtues.GetValue( (int)virtue );

			if ( current > 0 )
				from.Virtues.SetValue( (int)virtue, current - 1 );

			return ( current > 0 );
		}

		public static bool IsSeeker( Mobile from, VirtueName virtue )
		{
			return ( GetLevel( from, virtue ) >= VirtueLevel.Seeker );
		}

		public static bool IsFollower( Mobile from, VirtueName virtue )
		{
			return ( GetLevel( from, virtue ) >= VirtueLevel.Follower );
		}

		public static bool IsKnight( Mobile from, VirtueName virtue )
		{
			return ( GetLevel( from, virtue ) >= VirtueLevel.Knight );
		}
	}
}