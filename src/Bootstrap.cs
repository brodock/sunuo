/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2006 Max Kellermann <max@duempel.org>
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; version 2 of the License.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 */

using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Server
{
	public class Bootstrap
	{
		public static void Main( string[] args )
		{
			/* print a banner */
			Version ver = Assembly.GetEntryAssembly().GetName().Version;
			Console.WriteLine("SunUO Version {0}.{1}.{2} http://www.sunuo.org/",
							  ver.Major, ver.Minor, ver.Revision);
			Console.WriteLine("  on {0}, runtime {1}",
							  Environment.OSVersion, Environment.Version);

			if ((int)Environment.OSVersion.Platform == 128)
				Console.WriteLine("Please make sure you have Mono 1.1.7 or newer! (mono -V)");

			Console.WriteLine();

			/* parse command line */

			bool debug = false, service = false, profiling = false;

			string baseDirectory = null;
			string configFile = null;

			for ( int i = 0; i < args.Length; ++i )
			{
				switch (args[i]) {
				case "-debug":
				case "--debug":
					debug = true;
					break;

				case "-service":
				case "--service":
					service = true;
					break;

				case "-profile":
				case "--profile":
					profiling = true;
					break;

				case "-c":
				case "--config":
					if (i == args.Length - 1) {
						Console.Error.WriteLine("file name expected after {0}",
												args[i]);
						return;
					}

					configFile = args[++i];

					if (!File.Exists(configFile)) {
						Console.Error.WriteLine("{0} does not exist", configFile);
						return;
					}

					break;

				case "-b":
				case "--base":
					if (i == args.Length - 1) {
						Console.Error.WriteLine("directory name expected after {0}",
												args[i]);
						return;
					}

					baseDirectory = args[++i];

					if (!Directory.Exists(baseDirectory)) {
						Console.Error.WriteLine("{0} does not exist", baseDirectory);
						return;
					}

					break;

				default:
					Console.Error.WriteLine("Unrecognized command line argument: {0}",
											args[i]);
					return;
				}
			}

			if (baseDirectory == null)
				baseDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

			if (configFile == null) {
				string confDirectory = new DirectoryInfo(baseDirectory)
					.CreateSubdirectory("etc").FullName;
				configFile = Path.Combine(confDirectory, "sunuo.xml");
			}

			/* prepare environment */

			Directory.SetCurrentDirectory(baseDirectory);

			/* load configuration */

			Config.Root config = new Config.Root(baseDirectory, configFile);

			/* enter stage II */

			Core.Start(config, debug, service, profiling);
		}
	}
}
