using System;
using Server.Network;

namespace Server
{
	public class SE
	{
		public const bool Enabled = true;

		public static void Configure()
		{
			Core.SE = Enabled;
		}
	}
}