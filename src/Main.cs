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
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using Server;
using Server.Network;
using Server.Network.Encryption;
using Server.Accounting;
using Server.Gumps;
using Server.Profiler;

namespace Server
{
	public delegate void Slice();

	public class Core
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static bool m_Crashed;
		private static Thread timerThread;
		private static string m_ExePath;
		private static Assembly m_Assembly;
		private static Process m_Process;
		private static Thread m_Thread;
		private static bool m_Service;
		private static MultiTextWriter m_MultiConOut = new MultiTextWriter();

		private static Config.Root config;

		private static bool m_Profiling;
		private static DateTime m_ProfileStart;
		private static TimeSpan m_ProfileTime;

		private static MessagePump m_MessagePump;

		public static MessagePump MessagePump
		{
			get{ return m_MessagePump; }
			set{ m_MessagePump = value; }
		}

		public static Slice Slice;

		public static bool Profiling
		{
			get{ return m_Profiling; }
			set
			{
				if ( m_Profiling == value )
					return;

				m_Profiling = value;

				if ( m_ProfileStart > DateTime.MinValue )
					m_ProfileTime += DateTime.UtcNow - m_ProfileStart;

				m_ProfileStart = ( m_Profiling ? DateTime.UtcNow : DateTime.MinValue );
			}
		}

		public static TimeSpan ProfileTime
		{
			get
			{
				if ( m_ProfileStart > DateTime.MinValue )
					return m_ProfileTime + (DateTime.UtcNow - m_ProfileStart);

				return m_ProfileTime;
			}
		}

		public static bool Service{ get{ return m_Service; } }
		public static ArrayList DataDirectories {
			get { return config.DataDirectories; }
		}
		public static Assembly Assembly{ get{ return m_Assembly; } set{ m_Assembly = value; } }
		public static Process Process{ get{ return m_Process; } }
		public static Thread Thread{ get{ return m_Thread; } }
		public static MultiTextWriter MultiConsoleOut{ get{ return m_MultiConOut; } }

		private static AutoResetEvent m_Signal = new AutoResetEvent(true);
		public static void WakeUp() {
			m_Signal.Set();
		}

		public static string FindDataFile( string path )
		{
			foreach (string dir in config.DataDirectories) {
				string fullPath = Path.Combine(dir, path);

				if ( File.Exists( fullPath ) )
					return fullPath;
			}

			/* workaround for insane filename case */
			if (path.IndexOf('/') == -1) {
				string lp = path.ToLower();
				foreach (string dir in config.DataDirectories) {
					DirectoryInfo di = new DirectoryInfo(dir);
					if (!di.Exists)
						continue;

					foreach (FileInfo fi in di.GetFiles()) {
						if (fi.Name.ToLower() == lp)
							return fi.FullName;
					}
				}
			}

			if (log.IsWarnEnabled)
				log.WarnFormat("Warning: data file {0} not found", path);
			return null;
		}

		public static string FindDataFile( string format, params object[] args )
		{
			return FindDataFile( String.Format( format, args ) );
		}

		public static bool AOS
		{
			get
			{
				return Config.Features["age-of-shadows"] || SE;
			}
			set
			{
				log.Warn("A script attempted to modify Core.AOS - please configure that option in etc/sunuo.xml instead");
			}
		}

		public static bool SE
		{
			get
			{
				return Config.Features["samurai-empire"];
			}
			set
			{
				log.Warn("A script attempted to modify Core.SE - please configure that option in etc/sunuo.xml instead");
			}
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

		/* main loop profiler */
		private static MainProfile m_TotalProfile;
		private static MainProfile m_CurrentProfile;

		public static MainProfile TotalProfile {
			get { return m_TotalProfile; }
		}

		public static MainProfile CurrentProfile {
			get { return m_CurrentProfile; }
		}

		public static void ResetCurrentProfile() {

			m_CurrentProfile = new MainProfile(m_Now);
		}

		private static void ClockProfile(MainProfile.TimerId id) {
			DateTime prev = m_Now;
			m_Now = DateTime.UtcNow;

			TimeSpan diff = m_Now - prev;
			m_TotalProfile.Add(id, diff);
			m_CurrentProfile.Add(id, diff);
		}


		private static void CurrentDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			log.Fatal(e.ExceptionObject);

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

			log.Info( "Exiting..." );

			if ( !m_Crashed )
				EventSink.InvokeShutdown( new ShutdownEventArgs() );

			if (timerThread != null && timerThread.IsAlive)
				timerThread.Join();
			log.Info( "done" );
		}

		public static void Shutdown(bool restart) {
			int sunuo_exit = -1;
			try {
				/* the environment variable SUNUO_EXIT specifies which
				   exit code expresses "we're exiting and don't want
				   to be restarted"; at the same time, we know we've
				   been started from run.sh */
				string value = Environment.GetEnvironmentVariable("SUNUO_EXIT");
				if (value != null)
					sunuo_exit = Int32.Parse(value);
			} catch (Exception e) {
				log.Error(e);
			}

			if (sunuo_exit >= 0)
				Environment.Exit(restart ? 0 : sunuo_exit);

			if (restart) {
				try {
					Process.Start(ExePath);
				} catch (Exception e) {
					log.Fatal(e);
				}
			}

			Core.Process.Kill();
		}

		private static void Run() {
			m_Now = DateTime.UtcNow;
			m_TotalProfile = new MainProfile(m_Now);
			m_CurrentProfile = new MainProfile(m_Now);

			while ( !m_Closing )
			{
				m_Now = DateTime.UtcNow;

				/* wait until event happens */

				m_Signal.WaitOne();

				ClockProfile(MainProfile.TimerId.Idle);

				/* process mobiles */

				Mobile.ProcessDeltaQueue();

				ClockProfile(MainProfile.TimerId.MobileDelta);

				/* process items */

				Item.ProcessDeltaQueue();

				ClockProfile(MainProfile.TimerId.ItemDelta);

				/* process timers */

				Timer.Slice();

				ClockProfile(MainProfile.TimerId.Timers);

				/* network */

				m_MessagePump.Slice();

				NetState.FlushAll();
				NetState.ProcessDisposedQueue();

				ClockProfile(MainProfile.TimerId.Network);

				if ( Slice != null )
					Slice();

				/* done with this iteration */
				m_TotalProfile.Next();
				m_CurrentProfile.Next();
			}
		}

		public static void Initialize(Config.Root _config, bool _service, bool _profiling) {
			config = _config;
			m_Service = _service;
			Profiling = _profiling;

			m_Assembly = Assembly.GetEntryAssembly();

			/* prepare SunUO */
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler( CurrentDomain_UnhandledException );
			AppDomain.CurrentDomain.ProcessExit += new EventHandler( CurrentDomain_ProcessExit );

			/* redirect Console to file in service mode */
			if (m_Service) {
				string filename = Path.Combine(Config.LogDirectory, "console.log");
				FileStream stream = new FileStream(filename, FileMode.Create,
												   FileAccess.Write, FileShare.Read);
				StreamWriter writer = new StreamWriter(stream);
				Console.SetOut(writer);
				Console.SetError(writer);
			}

			m_Thread = Thread.CurrentThread;
			m_Process = Process.GetCurrentProcess();

			if ( m_Thread != null )
				m_Thread.Name = "Core Thread";

			if ( BaseDirectory.Length > 0 )
				Directory.SetCurrentDirectory( BaseDirectory );
		}

		public static void Start(bool repair) {
			if (!ScriptCompiler.Compile(true))
				return;

			m_ItemCount = 0;
			m_MobileCount = 0;
			foreach (Library l in ScriptCompiler.Libraries) {
				int itemCount = 0, mobileCount = 0;
				l.Verify(ref itemCount, ref mobileCount);
				log.InfoFormat("Library {0} verified: {1} items, {2} mobiles",
							   l.Name, itemCount, mobileCount);
				m_ItemCount += itemCount;
				m_MobileCount += mobileCount;
			}
			log.InfoFormat("All libraries verified: {0} items, {1} mobiles)",
						   m_ItemCount, m_MobileCount);

			try {
				TileData.Configure();

				ScriptCompiler.Configure();
			} catch (TargetInvocationException e) {
				log.Fatal("Configure exception: {0}", e.InnerException);
				return;
			}

			if (!config.Exists)
				config.Save();

			World.Load();
			if (World.LoadErrors > 0) {
				log.ErrorFormat("There were {0} errors during world load.", World.LoadErrors);
				if (repair) {
					log.Error("The world load errors are being ignored for now, and will not reappear once you save this world.");
				} else {
					log.Error("Try 'SunUO --repair' to repair this world save, or restore an older non-corrupt save.");
					return;
				}
			}

			try {
				ScriptCompiler.Initialize();
			} catch (TargetInvocationException e) {
				log.Fatal("Initialize exception: {0}", e.InnerException);
				return;
			}

			Region.Load();

			m_MessagePump = new MessagePump();
			foreach (IPEndPoint ipep in Config.Network.Bind)
				m_MessagePump.AddListener(new Listener(ipep));

			Timer.TimerThread ttObj = new Timer.TimerThread();
			timerThread = new Thread(new ThreadStart(ttObj.TimerMain));
			timerThread.Name = "Timer Thread";
			timerThread.Start();

			NetState.Initialize();
			Encryption.Initialize();

			EventSink.InvokeServerStarted();

			log.Info("SunUO initialized, entering main loop");

			try
			{
				Run();
			}
			catch ( Exception e )
			{
				CurrentDomain_UnhandledException( null, new UnhandledExceptionEventArgs( e, true ) );
			}

			if ( timerThread.IsAlive )
				timerThread.Abort();
		}

		private static int m_GlobalMaxUpdateRange = 24;

		public static int GlobalMaxUpdateRange
		{
			get{ return m_GlobalMaxUpdateRange; }
			set{ m_GlobalMaxUpdateRange = value; }
		}

		private static int m_ItemCount, m_MobileCount;

		public static int ScriptItems { get { return m_ItemCount; } }
		public static int ScriptMobiles { get { return m_MobileCount; } }
	}

	public class MultiTextWriter
	{
		public MultiTextWriter()
		{
		}

		public void Add( TextWriter tw )
		{
		}

		public void Remove( TextWriter tw )
		{
		}
	}
}
