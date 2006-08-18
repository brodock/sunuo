using System;
using Server;
using Server.Accounting;

namespace Server.Misc
{
	public class AccountPrompt
	{
		// This script prompts the console for a username and password when 0 accounts have been loaded
		public static void Initialize()
		{
			if ( Accounts.Table.Count == 0 && !Core.Service )
			{
				Console.WriteLine( "This server has no accounts." );
				Console.WriteLine( "Do you want to create an administrator account now? (y/n)" );

				if ( Console.ReadLine().StartsWith( "y" ) )
				{
					Console.Write( "Username: " );
					string username = Console.ReadLine();

					Console.Write( "Password: " );
					string password = Console.ReadLine();

					Account a = Accounts.AddAccount( username, password );

					a.AccessLevel = AccessLevel.Administrator;

					Console.WriteLine( "Account created, continuing" );
				}
				else
				{
					Console.WriteLine( "Account not created." );
				}
			}
		}
	}
}