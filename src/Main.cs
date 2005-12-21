/***************************************************************************
 *                                  Main.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author$
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

[assembly: log4net.Config.XmlConfigurator(Watch=true)]

namespace Server
{
	public delegate void Slice();

	public class Core
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static bool m_Crashed;
		private static Thread timerThread;
		private static string m_BaseDirectory;
		private static DirectoryInfo m_BaseDirectoryInfo;
		private static DirectoryInfo m_CacheDirectoryInfo;
		private static string m_ExePath;
		private static Assembly m_Assembly;
		private static Process m_Process;
		private static Thread m_Thread;
		private static bool m_Service;
		private static MultiTextWriter m_MultiConOut;

		private static Config config;

		private static bool m_AOS;
		private static bool m_SE;

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
					m_ProfileTime += DateTime.Now - m_ProfileStart;

				m_ProfileStart = ( m_Profiling ? DateTime.Now : DateTime.MinValue );
			}
		}

		public static TimeSpan ProfileTime
		{
			get
			{
				if ( m_ProfileStart > DateTime.MinValue )
					return m_ProfileTime + (DateTime.Now - m_ProfileStart);

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
				log.Warn(String.Format("Warning: data file {0} not found", path));
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
				return m_AOS || m_SE;
			}
			set
			{
				m_AOS = value;
			}
		}

		public static bool SE
		{
			get
			{
				return m_SE;
			}
			set
			{
				m_SE = value;
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
				if ( m_BaseDirectory == null )
				{
					try
					{
						m_BaseDirectory = ExePath;

						if ( m_BaseDirectory.Length > 0 )
							m_BaseDirectory = Path.GetDirectoryName( m_BaseDirectory );
					}
					catch
					{
						m_BaseDirectory = "";
					}
				}

				return m_BaseDirectory;
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

		public static Config Config {
			get { return config; }
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

				bool close = false;

				try
				{
					CrashedEventArgs args = new CrashedEventArgs( e.ExceptionObject as Exception );

					EventSink.InvokeCrashed( args );

					close = args.Close;
				}
				catch
				{
				}

				if ( !close && !m_Service )
				{
					Console.WriteLine( "This exception is fatal, press return to exit" );
					Console.ReadLine();
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

			timerThread.Join();
			log.Info( "done" );
		}

		public static void Main( string[] args )
		{
			m_Assembly = Assembly.GetEntryAssembly();

			/* print a banner */
			Version ver = m_Assembly.GetName().Version;
			Console.WriteLine("SunUO Version {0}.{1}.{2} http://www.sunuo.org/",
							  ver.Major, ver.Minor, ver.Revision);
			Console.WriteLine("  on {0}, runtime {1}",
							  Environment.OSVersion, Environment.Version);

			if ((int)Environment.OSVersion.Platform == 128)
				Console.WriteLine("Please make sure you have Mono 1.1.7 or newer! (mono -V)");

			Console.WriteLine();

			/* prepare SunUO */
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler( CurrentDomain_UnhandledException );
			AppDomain.CurrentDomain.ProcessExit += new EventHandler( CurrentDomain_ProcessExit );

			bool debug = false;

			for ( int i = 0; i < args.Length; ++i )
			{
				if ( Insensitive.Equals( args[i], "-debug" ) )
					debug = true;
				else if ( Insensitive.Equals( args[i], "-service" ) )
					m_Service = true;
				else if ( Insensitive.Equals( args[i], "-profile" ) )
					Profiling = true;
				else if (args[i] == "--logfile") {
					string logfile = args[++i];
					StreamWriter writer = new StreamWriter(new FileStream(logfile, FileMode.Append, FileAccess.Write));
					writer.AutoFlush = true;
					Console.SetOut(writer);
					Console.SetError(writer);
				}
			}

			config = new Config(Path.Combine(BaseDirectoryInfo.CreateSubdirectory("etc").FullName, "sunuo.xml"));

			try
			{
				m_MultiConOut = new MultiTextWriter(Console.Out);
				Console.SetOut(m_MultiConOut);

				if (m_Service) {
					string filename = Path.Combine(LogDirectoryInfo.FullName, "console.log");
					m_MultiConOut.Add(new FileLogger(filename));
				}
			}
			catch
			{
			}

			m_Thread = Thread.CurrentThread;
			m_Process = Process.GetCurrentProcess();

			if ( m_Thread != null )
				m_Thread.Name = "Core Thread";

			if ( BaseDirectory.Length > 0 )
				Directory.SetCurrentDirectory( BaseDirectory );

			Timer.TimerThread ttObj = new Timer.TimerThread();
			timerThread = new Thread( new ThreadStart( ttObj.TimerMain ) );
			timerThread.Name = "Timer Thread";

			if (!ScriptCompiler.Compile(debug))
				return;

			Console.Write("Verifying scripts:");
			m_ItemCount = 0;
			m_MobileCount = 0;
			foreach (Library l in ScriptCompiler.Libraries) {
				int itemCount = 0, mobileCount = 0;
				Console.Write(" {0}[", l.Name);
				l.Verify(ref itemCount, ref mobileCount);
				Console.Write("{0} items, {1} mobiles]", itemCount, mobileCount);
				m_ItemCount += itemCount;
				m_MobileCount += mobileCount;
			}
			Console.WriteLine(" - done ({0} items, {1} mobiles)", m_ItemCount, m_MobileCount);

			try {
				ScriptCompiler.Configure();
			} catch (TargetInvocationException e) {
				log.Fatal("Configure exception: {0}", e.InnerException);
				return;
			}

			if (!config.Exists)
				config.Save();

			World.Load();

			try {
				ScriptCompiler.Initialize();
			} catch (TargetInvocationException e) {
				log.Fatal("Initialize exception: {0}", e.InnerException);
				return;
			}

			Region.Load();

			m_MessagePump = new MessagePump( new Listener( Listener.Port ) );

			timerThread.Start();

			NetState.Initialize();
			Encryption.Initialize();

			EventSink.InvokeServerStarted();

			try
			{
				while ( !m_Closing )
				{
					Thread.Sleep( 1 );

					Mobile.ProcessDeltaQueue();
					Item.ProcessDeltaQueue();

					Timer.Slice();
					m_MessagePump.Slice();

					NetState.FlushAll();
					NetState.ProcessDisposedQueue();

					if ( Slice != null )
						Slice();
				}
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

	public class FileLogger : TextWriter, IDisposable
	{
		private string m_FileName;
		private bool m_NewLine;
		public const string DateFormat = "[MMMM dd hh:mm:ss.f tt]: ";

		public string FileName{ get{ return m_FileName; } }

		public FileLogger( string file ) : this( file, false )
		{
		}

		public FileLogger( string file, bool append )
		{
			m_FileName = file;
			using ( StreamWriter writer = new StreamWriter( new FileStream( m_FileName, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read ) ) )
			{
				writer.WriteLine( ">>>Logging started on {0}.", DateTime.Now.ToString( "f" ) ); //f = Tuesday, April 10, 2001 3:51 PM 
			}
			m_NewLine = true;
		}

		public override void Write( char ch )
		{
			using ( StreamWriter writer = new StreamWriter( new FileStream( m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read ) ) )
			{
				if ( m_NewLine )
				{
					writer.Write( DateTime.Now.ToString( DateFormat ) );
					m_NewLine = false;
				}
				writer.Write( ch );
			}
		}

		public override void Write( string str )
		{
			using ( StreamWriter writer = new StreamWriter( new FileStream( m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read ) ) )
			{
				if ( m_NewLine )
				{
					writer.Write( DateTime.Now.ToString( DateFormat ) );
					m_NewLine = false;
				}
				writer.Write( str );
			}
		}

		public override void WriteLine( string line )
		{
			using ( StreamWriter writer = new StreamWriter( new FileStream( m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read ) ) )
			{
				if ( m_NewLine )
					writer.Write( DateTime.Now.ToString( DateFormat ) );
				writer.WriteLine( line );
				m_NewLine = true;
			}
		}

		public override System.Text.Encoding Encoding
		{
			get{ return System.Text.Encoding.Default; }
		}
	}
	
	public class MultiTextWriter : TextWriter
	{
		private ArrayList m_Streams;

		public MultiTextWriter( params TextWriter[] streams )
		{
			m_Streams = new ArrayList( streams );

			if ( m_Streams.Count < 0 )
				throw new ArgumentException( "You must specify at least one stream." );
		}

		public void Add( TextWriter tw )
		{
			m_Streams.Add( tw );
		}

		public void Remove( TextWriter tw )
		{
			m_Streams.Remove( tw );
		}

		public override void Write( char ch )
		{
			for (int i=0;i<m_Streams.Count;i++)
				((TextWriter)m_Streams[i]).Write( ch );
		}

		public override void WriteLine( string line )
		{
			for (int i=0;i<m_Streams.Count;i++)
				((TextWriter)m_Streams[i]).WriteLine( line );
		}

		public override void WriteLine( string line, params object[] args )
		{
			WriteLine( String.Format( line, args ) );
		}

		public override System.Text.Encoding Encoding
		{
			get{ return System.Text.Encoding.Default; }
		}
	}
}
