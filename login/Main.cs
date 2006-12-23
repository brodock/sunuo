/***************************************************************************
 *                                  Main.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author: make $
 *   $Date$
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
		private static DirectoryInfo m_BaseDirectoryInfo;
		private static DirectoryInfo m_CacheDirectoryInfo;
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

		public static string ExePath
		{
			get
			{
				if ( m_ExePath == null )
					m_ExePath = Process.GetCurrentProcess().MainModule.FileName;

				return m_ExePath;
			}
		}

		public static string BaseDirectory
		{
			get
			{
				return Config.BaseDirectory;
			}
		}

		public static DirectoryInfo BaseDirectoryInfo {
			get {
				if (m_BaseDirectoryInfo == null)
					m_BaseDirectoryInfo = new DirectoryInfo(BaseDirectory);

				return m_BaseDirectoryInfo;
			}
		}

		public static DirectoryInfo LocalDirectoryInfo {
			get {
				return BaseDirectoryInfo
					.CreateSubdirectory("local");
			}
		}

		public static DirectoryInfo LogDirectoryInfo {
			get {
				return BaseDirectoryInfo
					.CreateSubdirectory("var")
					.CreateSubdirectory("log");
			}
		}

		public static DirectoryInfo CacheDirectoryInfo {
			get {
				if (m_CacheDirectoryInfo == null)
					m_CacheDirectoryInfo = BaseDirectoryInfo
						.CreateSubdirectory("var")
						.CreateSubdirectory("cache");

				return m_CacheDirectoryInfo;
			}
		}

		public static Config.Root Config {
			get { return config; }
		}

		/* current time */
		private static DateTime m_Now = DateTime.Now;
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

				try
				{
					CrashedEventArgs args = new CrashedEventArgs( e.ExceptionObject as Exception );

					EventSink.InvokeCrashed( args );
				}
				catch
				{
				}

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

			timerThread.Join();
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

			if ( BaseDirectory.Length > 0 )
				Directory.SetCurrentDirectory( BaseDirectory );

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
					m_Now = DateTime.Now;

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
