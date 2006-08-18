using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Server;
using Server.Network;

namespace Server.Misc
{
	public class IPLimiter
	{
		public static bool Enabled = true;
		public static bool SocketBlock = true; // true to block at connection, false to block at login request

		public const int MaxAddresses = 10;

		public static bool Verify( IPAddress ourAddress )
		{
			if ( !Enabled )
				return true;

			ArrayList netStates = NetState.Instances;

			int count = 0;

			for ( int i = 0; i < netStates.Count; ++i )
			{
				NetState compState = (NetState)netStates[i];

				if ( ourAddress.Equals( compState.Address ) )
				{
					++count;

					if ( count > MaxAddresses )
						return false;
				}
			}

			return true;
		}
	}
}