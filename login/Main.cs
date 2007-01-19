/*
 * SunUO
 * $Id$
 *
 * (c) 2005-2006 Max Kellermann <max@duempel.org>
 * based on code (C) The RunUO Software Team
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
using System.Net;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using Server;
using Server.Network;
using Server.Network.Encryption;
using Server.Accounting;

[assembly: log4net.Config.XmlConfigurator(Watch=true)]

namespace Server
{
	public class Core
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static bool m_Crashed;
		private static Thread timerThread;
		private static string m_ExePath;
		private static Assembly m_Assembly;
		private static Thread m_Thread;

		private static Config.Root config;

		private static MessagePump m_MessagePump;

		public static MessagePump MessagePump
		{
			get{ return m_MessagePump; }
			set{ m_MessagePump = value; }
		}

		public static bool Profiling {
			get { return false; }
		}

		public static ArrayList DataDirectories {
			get { return config.DataDirectories; }
		}
		public static Assembly Assembly{ get{ return m_Assembly; } set{ m_Assembly = value; } }
		public static Thread Thread{ get{ return m_Thread; } }

		private static AutoResetEvent m_Signal = new AutoResetEvent(true);
		public static void WakeUp() {
			m_Signal.Set();
		}

		[Obsolete]
		public static string ExePath
		{
			get
			{
				if ( m_ExePath == null )
					m_ExePath = Process.GetCurrentProcess().MainModule.FileName;

				return m_ExePath;
			}
		}

		[Obsolete]
		public static string BaseDirectory
		{
			get
			{
				return Config.BaseDirectory;
			}
		}

		public static Config.Root Config {
			get { return config; }
		}

		/* current time */
		private static DateTime m_Now = DateTime.UtcNow;
		public static DateTime Now {
			get {
				return m_Now;
			}
		}

		private static void CurrentDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			if (e.IsTerminating)
				log.Fatal(e);
			else
				log.Error(e);

			if ( e.IsTerminating )
			{
				m_Crashed = true;
				m_Closing = true;
			}
		}

		private static void CurrentDomain_ProcessExit( object sender, EventArgs e )
		{
			HandleClosed();
		}

		private static bool m_Closing;

		public static bool Closing{ get{ return m_Closing; } set{ m_Closing = value; } }

		private static void HandleClosed()
		{
			if ( m_Closing )
				return;

			m_Closing = true;

			Console.Write( "Exiting..." );

			if ( !m_Crashed )
				EventSink.InvokeShutdown( new ShutdownEventArgs() );

			if (timerThread != null && timerThread.IsAlive) {
				TimerThread.WakeUp();
				timerThread.Join();
			}

			Console.WriteLine( "done" );
		}

		public static void Main( string[] args )
		{
			m_Assembly = Assembly.GetEntryAssembly();

			/* print a banner */
			Version ver = m_Assembly.GetName().Version;
			Console.WriteLine("SunLogin Version {0}.{1}.{2} http://www.sunuo.org/",
							  ver.Major, ver.Minor, ver.Revision);
			Console.WriteLine("  on {0}, runtime {1}",
							  Environment.OSVersion, Environment.Version);

			if ((int)Environment.OSVersion.Platform == 128)
				Console.WriteLine("Please make sure you have Mono 1.1.7 or newer! (mono -V)");

			Console.WriteLine();

			/* prepare SunUO */
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler( CurrentDomain_UnhandledException );
			AppDomain.CurrentDomain.ProcessExit += new EventHandler( CurrentDomain_ProcessExit );

			if (args.Length > 0) {
				Console.WriteLine("SunLogin does not understand command line arguments");
				return;
			}

			string baseDirectory = Path.GetDirectoryName(ExePath);
			string confDirectory = new DirectoryInfo(baseDirectory)
				.CreateSubdirectory("etc").FullName;

			config = new Config.Root(baseDirectory,
									 Path.Combine(confDirectory, "sunuo.xml"));

			Directory.SetCurrentDirectory(config.BaseDirectory);

			m_Thread = Thread.CurrentThread;

			if ( m_Thread != null )
				m_Thread.Name = "Core Thread";

			if (config.BaseDirectory.Length > 0)
				Directory.SetCurrentDirectory(config.BaseDirectory);

			Timer.TimerThread ttObj = new Timer.TimerThread();
			timerThread = new Thread( new ThreadStart( ttObj.TimerMain ) );
			timerThread.Name = "Timer Thread";

			if (!config.Exists)
				config.Save();

			m_MessagePump = new MessagePump();
			foreach (IPEndPoint ipep in Config.Network.Bind)
				m_MessagePump.AddListener(new Listener(ipep));

			timerThread.Start();

			NetState.Initialize();
			Encryption.Initialize();
			ServerList.Initialize();
			ServerQueryTimer.Initialize();
			Server.Accounting.AccountHandler.Initialize();

			EventSink.InvokeServerStarted();

			log.Info("SunLogin initialized, entering main loop");

			try
			{
				while ( !m_Closing )
				{
					m_Signal.WaitOne();
					m_Now = DateTime.UtcNow;

					Timer.Slice();
					m_MessagePump.Slice();

					NetState.FlushAll();
					NetState.ProcessDisposedQueue();
				}
			}
			catch ( Exception e )
			{
				CurrentDomain_UnhandledException( null, new UnhandledExceptionEventArgs( e, true ) );
			}

			if ( timerThread.IsAlive )
				timerThread.Abort();
		}
	}
}
