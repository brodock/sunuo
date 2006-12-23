/***************************************************************************
 *                             ScriptCompiler.cs
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
using System.Collections;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace Server
{
	public class ScriptCompiler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static ArrayList libraries;

		public static Library[] Libraries {
			get {
				return (Library[])libraries.ToArray(typeof(Library));
			}
		}

		/** find a loaded library by its name */
		public static Library GetLibrary(string name) {
			foreach (Library l in libraries)
				if (l.Name == name)
					return l;
			return null;
		}

		public static Assembly[] Assemblies
		{
			get
			{
				ArrayList assemblies = new ArrayList(libraries.Count);
				for (int i = libraries.Count - 1; i >= 0; --i) {
					Library l = (Library)libraries[i];
					assemblies.Add(l.Assembly);
				}
				return (Assembly[])assemblies.ToArray(typeof(Assembly));
			}
		}

		private static ArrayList m_AdditionalReferences;

		private static string[] GetReferenceAssemblies() {
			ArrayList list = new ArrayList();

			string path = Path.Combine(Core.Config.ConfigDirectory, "Assemblies.cfg");

			if ( File.Exists( path ) )
			{
				using ( StreamReader ip = new StreamReader( path ) )
				{
					string line;

					while ( (line = ip.ReadLine()) != null )
					{
						if ( line.Length > 0 && !line.StartsWith( "#" ) )
							list.Add( line );
					}
				}
			}

			list.AddRange( m_AdditionalReferences );

			return (string[])list.ToArray( typeof( string ) );
		}

		private static Hashtable ReadStampFile(string filename) {
			if (!File.Exists(filename))
				return null;

			FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
			BinaryReader br = new BinaryReader(fs);
			int version = br.ReadInt32();
			if (version != 1)
				return null;

			Hashtable stamps = new Hashtable();

			uint count = br.ReadUInt32();
			for (uint i = 0; i < count; i++) {
				string fn = br.ReadString();
				long ticks = br.ReadInt64();
				stamps[fn] = new DateTime(ticks);
			}

			br.Close();
			fs.Close();

			return stamps;
		}

		private static void WriteStampFile(string filename, Hashtable stamps) {
			FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
			BinaryWriter bw = new BinaryWriter(fs);
			bw.Write((int)1);

			/*Version ver = Core.Assembly.GetName().Version;
			bw.Write(ver.Major);
			bw.Write(ver.Minor);
			bw.Write(ver.Build);
			bw.Write(ver.Revision);*/

			bw.Write((uint)stamps.Count);
			IDictionaryEnumerator e = stamps.GetEnumerator();
			while (e.MoveNext()) {
				bw.Write((string)e.Key);
				bw.Write((long)((DateTime)e.Value).Ticks);
			}

			bw.Close();
			fs.Close();
		}

		private static bool CheckStamps(Hashtable files,
										string stampFile) {
			Hashtable stamps = ReadStampFile(stampFile);
			if (stamps == null)
				return false;

			IDictionaryEnumerator e = files.GetEnumerator();
			while (e.MoveNext()) {
				string filename = (string)e.Key;
				if (!stamps.ContainsKey(filename))
					return false;

				DateTime newStamp = (DateTime)e.Value;
				DateTime oldStamp = (DateTime)stamps[filename];

				if (oldStamp != newStamp)
					return false;

				stamps.Remove(filename);
			}

			return stamps.Count == 0;
		}

		private static CompilerResults CompileCSScripts(ICollection fileColl,
														string assemblyFile,
														Config.Library libConfig,
														bool debug) {
			CSharpCodeProvider provider = new CSharpCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files;

			log.InfoFormat("Compiling library {0}, {1} C# sources",
						   libConfig.Name, fileColl.Count);

			string tempFile = compiler.GetType().FullName == "Mono.CSharp.CSharpCodeCompiler"
				? Path.GetTempFileName() : null;
			if (tempFile == String.Empty)
				tempFile = null;
			if (tempFile == null) {
				files = new string[fileColl.Count];
				fileColl.CopyTo(files, 0);
			} else {
				/* to prevent an "argument list too long" error, we
				   write a list of file names to a temporary file
				   and add them with @filename */
				StreamWriter w = new StreamWriter(tempFile, false);
				foreach (string file in fileColl) {
					w.Write("\"" + file + "\" ");
				}
				w.Close();

				files = new string[0];
			}

			CompilerParameters parms = new CompilerParameters( GetReferenceAssemblies(), assemblyFile, debug );
			if (tempFile != null)
				parms.CompilerOptions += "@" + tempFile;

			if (libConfig.WarningLevel >= 0)
				parms.WarningLevel = libConfig.WarningLevel;

			CompilerResults results = null;
			try {
				results = compiler.CompileAssemblyFromFileBatch( parms, files );
			} catch (System.ComponentModel.Win32Exception e) {
				/* from WinError.h:
				 * #define ERROR_FILE_NOT_FOUND 2L
				 * #define ERROR_PATH_NOT_FOUND 3L
				 */
				if (e.NativeErrorCode == 2 || e.NativeErrorCode == 3) {
					log.Fatal("Could not find the compiler - are you sure MCS is installed?");
					log.Info("On Debian, try: apt-get install mono-mcs");
					Environment.Exit(2);
				} else {
					throw e;
				}
			}

			if (tempFile != null)
				File.Delete(tempFile);

			m_AdditionalReferences.Add(assemblyFile);

			if ( results.Errors.Count > 0 )
			{
				int errorCount = 0, warningCount = 0;

				foreach ( CompilerError e in results.Errors )
				{
					if ( e.IsWarning )
						++warningCount;
					else
						++errorCount;
				}

				if ( errorCount > 0 )
					log.ErrorFormat("Compilation failed ({0} errors, {1} warnings)",
									errorCount, warningCount);
				else
					log.InfoFormat("Compilation complete ({0} warnings)", warningCount);

				foreach ( CompilerError e in results.Errors )
				{
					String msg = String.Format("{0}: {1}: (line {2}, column {3}) {4}",
											   e.FileName, e.ErrorNumber, e.Line, e.Column, e.ErrorText);
					if (e.IsWarning)
						log.Warn(msg);
					else
						log.Error(msg);
				}
			}
			else
			{
				log.Info("Compilation complete");
			}

			return results;
		}

		private static CompilerResults CompileVBScripts(ICollection fileColl,
														string assemblyFile,
														Config.Library libConfig,
														bool debug) {
			VBCodeProvider provider = new VBCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files = new string[fileColl.Count];
			fileColl.CopyTo(files, 0);

			log.InfoFormat("Compiling library {0}, {1} C# sources",
						   libConfig.Name, files.Length);

			CompilerResults results = compiler.CompileAssemblyFromFileBatch( new CompilerParameters( GetReferenceAssemblies(), assemblyFile, true ), files );

			m_AdditionalReferences.Add(assemblyFile);

			if ( results.Errors.Count > 0 )
			{
				int errorCount = 0, warningCount = 0;

				foreach ( CompilerError e in results.Errors )
				{
					if ( e.IsWarning )
						++warningCount;
					else
						++errorCount;
				}

				if ( errorCount > 0 )
					log.ErrorFormat("Compilation failed ({0} errors, {1} warnings)",
									errorCount, warningCount);
				else
					log.InfoFormat("Compilation complete ({0} warnings)", warningCount);

				foreach ( CompilerError e in results.Errors )
				{
					String msg = String.Format("{0}: {1}: (line {2}, column {3}) {4}",
											   e.FileName, e.ErrorNumber, e.Line, e.Column, e.ErrorText);
					if (e.IsWarning)
						log.Warn(msg);
					else
						log.Error(msg);
				}
			}
			else
			{
				log.Info("Compilation complete");
			}

			return results;
		}

		private static void Overlay(string base1, Hashtable files1,
									string base2, Hashtable files2) {
			foreach (string filename in files2.Keys) {
				files1.Remove(base1 + Path.DirectorySeparatorChar + filename.Substring(base2.Length + 1));
				files1[filename] = files2[filename];
			}
		}

		private static Hashtable GetScripts(Config.Library libConfig, IEnumerable overlays,
											string type) {
			Hashtable files = GetScripts(libConfig, type);

			if (overlays != null) {
				foreach (Config.Library overlay in overlays) {
					Hashtable files2 = GetScripts(overlay, type);

					Overlay(libConfig.SourcePath.FullName, files,
							overlay.SourcePath.FullName, files2);
				}
			}

			return files;
		}

		private static bool Compile(Config.Library libConfig,
									bool debug) {
			/* check if there is source code for this library */
			if (libConfig.SourcePath == null) {
				if (libConfig.BinaryPath == null) {
					log.WarnFormat("library {0} does not exist", libConfig.Name);
					return true;
				} else if (!libConfig.BinaryPath.Exists) {
					log.WarnFormat("library {0} does not exist: {1}",
								   libConfig.Name, libConfig.BinaryPath);
					return false;
				}

				log.InfoFormat("Loading library {0}", libConfig.Name);
				libraries.Add(new Library(libConfig,
										  Assembly.LoadFrom(libConfig.BinaryPath.FullName)));
				m_AdditionalReferences.Add(libConfig.BinaryPath.FullName);
				return true;
			} else if (!libConfig.SourcePath.Exists) {
				log.WarnFormat("library {0} does not exist", libConfig.Name);
				return true;
			}

			DirectoryInfo cache = Core.CacheDirectoryInfo
				.CreateSubdirectory("lib")
				.CreateSubdirectory(libConfig.Name);

			if (!cache.Exists) {
				log.ErrorFormat("Failed to create directory {0}", cache.FullName);
				return false;
			}

			ArrayList overlays = null;
			if (libConfig.Overlays != null) {
				overlays = new ArrayList();
				foreach (string name in libConfig.Overlays)
					overlays.Add(Core.Config.GetLibrary(name));
			}

			string csFile = Path.Combine(cache.FullName, libConfig.Name + ".dll");
			Hashtable files = GetScripts(libConfig, overlays, "*.cs");
			if (files.Count > 0) {
				string stampFile = Path.Combine(cache.FullName, libConfig.Name + ".stm");
				if (File.Exists(csFile) && CheckStamps(files, stampFile)) {
					libraries.Add(new Library(libConfig, Assembly.LoadFrom(csFile)));
					m_AdditionalReferences.Add(csFile);
					log.InfoFormat("Loaded binary library {0}", libConfig.Name);
				} else {
					/* work around a serious faction bug: the factions
					   code (Reflector.cs) assumes alphabetical
					   directory entry order; simulate this by sorting
					   the array. See my bug report:
					   http://www.runuo.com/forum/showthread.php?p=373540 */
					ArrayList sorted = new ArrayList(files.Keys);
					sorted.Sort();

					CompilerResults results = CompileCSScripts(sorted,
															   csFile,
															   libConfig,
															   debug);
					if (results != null) {
						if (results.Errors.HasErrors)
							return false;
						libraries.Add(new Library(libConfig, results.CompiledAssembly));
						WriteStampFile(stampFile, files);
					}
				}
			}

			string vbFile = Path.Combine(cache.FullName, libConfig.Name + "-vb.dll");
			files = GetScripts(libConfig, overlays, "*.vb");
			if (files.Count > 0) {
				string stampFile = Path.Combine(cache.FullName, libConfig.Name + "-vb.stm");
				if (File.Exists(vbFile) && CheckStamps(files, stampFile)) {
					libraries.Add(new Library(libConfig,
											  Assembly.LoadFrom(vbFile)));
					m_AdditionalReferences.Add(vbFile);
					log.InfoFormat("Loaded binary library {0}/VB", libConfig.Name);
				} else {
					/* workaround again */
					ArrayList sorted = new ArrayList(files.Keys);
					sorted.Sort();

					CompilerResults results = CompileVBScripts(sorted, vbFile,
															   libConfig,
															   debug);
					if (results != null) {
						if (results.Errors.HasErrors)
							return false;
						libraries.Add(new Library(libConfig,
												  results.CompiledAssembly));
					}
				}
			}

			return true;
		}

		/**
		 * enqueue a library for compilation, resolving all
		 * dependencies first
		 *
		 * @param dst this array will receive the libraries in the correct order
		 * @param libs source libraries
		 * @param queue somewhat like a stack of libraries currently waiting
		 * @param libConfig the library to be added
		 */
		private static void EnqueueLibrary(ArrayList dst, ArrayList libs,
										   Hashtable queue, Config.Library libConfig) {
			string[] depends = libConfig.Depends;

			if (libConfig.Name == "core" || libConfig.Disabled) {
				libs.Remove(libConfig);
				return;
			}

			if (!libConfig.Exists) {
				libs.Remove(libConfig);
				log.WarnFormat("library {0} does not exist", libConfig.Name);
				return;
			}

			/* first resolve dependencies */
			if (depends != null) {
				queue[libConfig.Name] = 1;

				foreach (string depend in depends) {
					/* if the depended library is already in the
					 * queue, there is a circular dependency */
					if (queue.ContainsKey(depend)) {
						log.ErrorFormat("Circular library dependency {0} on {1}",
										libConfig.Name, depend);
						throw new ApplicationException();
					}

					Config.Library next = Core.Config.GetLibrary(depend);
					if (next == null || !next.Exists) {
						log.ErrorFormat("Unresolved library dependency: {0} depends on {1}, which does not exist",
										libConfig.Name, depend);
						throw new ApplicationException();
					}

					if (next.Disabled) {
						log.ErrorFormat("Unresolved library dependency: {0} depends on {1}, which is disabled",
										libConfig.Name, depend);
						throw new ApplicationException();
					}

					if (!dst.Contains(next))
						EnqueueLibrary(dst, libs, queue, next);
				}

				queue.Remove(libConfig.Name);
			}

			/* then add it to 'dst' */
			dst.Add(libConfig);
			libs.Remove(libConfig);
		}

		private static ArrayList SortLibrariesByDepends() {
			ArrayList libs = new ArrayList(Core.Config.Libraries);
			Hashtable queue = new Hashtable();
			ArrayList dst = new ArrayList();

			/* handle "./Scripts/" first, for most compatibility */
			Config.Library libConfig = Core.Config.GetLibrary("legacy");
			if (libConfig != null)
				EnqueueLibrary(dst, libs, queue, libConfig);

			while (libs.Count > 0)
				EnqueueLibrary(dst, libs, queue, (Config.Library)libs[0]);

			return dst;
		}

		public static bool Compile( bool debug )
		{
			if (m_AdditionalReferences != null)
				throw new ApplicationException("already compiled");

			m_AdditionalReferences = new ArrayList();

			libraries = new ArrayList();
			libraries.Add(new Library(Core.Config.GetLibrary("core"),
									  Core.Assembly));
			m_AdditionalReferences.Add(Core.ExePath);

			/* prepare overlays */
			foreach (Config.Library libConfig in Core.Config.Libraries) {
				if (libConfig.Overlays == null || !libConfig.Exists ||
					libConfig.Name == "core")
					continue;

				if (libConfig.SourcePath == null) {
					log.ErrorFormat("Can't overlay the binary library {0}",
									libConfig.Name);
					throw new ApplicationException();
				}

				foreach (string name in libConfig.Overlays) {
					Config.Library overlay = Core.Config.GetLibrary(name);
					if (overlay == null || !overlay.Exists) {
						log.ErrorFormat("Can't overlay {0} with {1}, because it does not exist",
										libConfig.Name, name);
						throw new ApplicationException();
					}

					if (overlay.SourcePath == null) {
						log.ErrorFormat("Can't overlay {0} with {1}, because it is binary only",
										libConfig.Name, overlay.Name);
						throw new ApplicationException();
					}

					overlay.Disabled = true;
				}
			}

			foreach (Config.Library libConfig in Core.Config.Libraries) {
				if (libConfig.Overlays != null && libConfig.Exists &&
					libConfig.Name != "core" && libConfig.Disabled) {
					log.ErrorFormat("Can't overlay library {0} which is already used as overlay for another library",
									libConfig.Name);
					throw new ApplicationException();
				}
			}

			/* collect Config.Library objects, sort them and compile */
			ArrayList libConfigs = SortLibrariesByDepends();

			foreach (Config.Library libConfig in libConfigs) {
				bool result = Compile(libConfig, debug);
				if (!result)
					return false;
			}

			/* delete unused cache directories */
			DirectoryInfo cacheDir = Core.CacheDirectoryInfo
				.CreateSubdirectory("lib");
			foreach (DirectoryInfo sub in cacheDir.GetDirectories()) {
				string libName = sub.Name.ToLower();
				if (GetLibrary(libName) == null)
					sub.Delete(true);
			}

			/* done */
			return true;
		}

		public static void Configure() {
			foreach (Library l in libraries)
				l.Configure();
		}

		public static void Initialize() {
			foreach (Library l in libraries)
				l.Initialize();
		}

		private static Hashtable m_TypeCaches = new Hashtable();
		private static TypeCache m_NullCache;

		private static Library findLibrary(Assembly asm) {
			foreach (Library l in libraries) {
				if (l.Assembly == asm)
					return l;
			}

			return null;
		}

		public static TypeCache GetTypeCache( Assembly asm )
		{
			if ( asm == null )
			{
				if ( m_NullCache == null )
					m_NullCache = new TypeCache(Type.EmptyTypes,
												new TypeTable(0),
												new TypeTable(0));

				return m_NullCache;
			}

			TypeCache c = (TypeCache)m_TypeCaches[asm];

			if (c == null) {
				Library l = findLibrary(asm);
				if (l == null)
					throw new ApplicationException("Invalid assembly");
				m_TypeCaches[asm] = c = l.TypeCache;
			}

			return c;
		}

		public static Type FindTypeByFullName( string fullName )
		{
			return FindTypeByFullName( fullName, true );
		}

		public static Type FindTypeByFullName( string fullName, bool ignoreCase )
		{
			for (int i = libraries.Count - 1; i >= 0; --i) {
				Library l = (Library)libraries[i];
				Type type = l.TypeCache.GetTypeByFullName(fullName, ignoreCase);
				if (type != null)
					return type;
			}

			return null;
		}

		public static Type FindTypeByName( string name )
		{
			return FindTypeByName( name, true );
		}

		public static Type FindTypeByName( string name, bool ignoreCase )
		{
			for (int i = libraries.Count - 1; i >= 0; --i) {
				Library l = (Library)libraries[i];
				Type type = l.TypeCache.GetTypeByName(name, ignoreCase);
				if (type != null)
					return type;
			}

			return null;
		}

		private static Hashtable GetScripts(Config.Library libConfig,
											string type) {
			Hashtable list = new Hashtable();

			GetScripts(libConfig, list, libConfig.SourcePath.FullName, type);

			return list;
		}

		private static void GetScripts(Config.Library libConfig,
									   Hashtable list, string path, string type) {
			foreach (string dir in Directory.GetDirectories(path)) {
				string baseName = Path.GetFileName(dir).ToLower();
				if (baseName == ".svn" || baseName == "_svn" ||
					baseName == "_darcs" || baseName == ".git" ||
					baseName == ".hg" || baseName == "cvs")
					continue;

				GetScripts(libConfig, list, dir, type);
			}

			foreach (string filename in Directory.GetFiles(path, type)) {
				/* XXX: pass relative filename only */
				if (libConfig == null || !libConfig.GetIgnoreSource(filename))
					list[filename] = File.GetLastWriteTime(filename);
			}
		}
	}

	public class TypeCache
	{
		private Type[] m_Types;
		private TypeTable m_Names, m_FullNames;

		public Type[] Types{ get{ return m_Types; } }
		public TypeTable Names{ get{ return m_Names; } }
		public TypeTable FullNames{ get{ return m_FullNames; } }

		public Type GetTypeByName( string name, bool ignoreCase )
		{
			return m_Names.Get( name, ignoreCase );
		}

		public Type GetTypeByFullName( string fullName, bool ignoreCase )
		{
			return m_FullNames.Get( fullName, ignoreCase );
		}

		public TypeCache(Type[] types, TypeTable names, TypeTable fullNames) {
			m_Types = types;
			m_Names = names;
			m_FullNames = fullNames;
		}
	}

	public class TypeTable
	{
		private Hashtable m_Sensitive, m_Insensitive;

		public void Add( string key, Type type )
		{
			m_Sensitive[key] = type;
			m_Insensitive[key] = type;
		}

		public Type Get( string key, bool ignoreCase )
		{
			if ( ignoreCase )
				return (Type)m_Insensitive[key];

			return (Type)m_Sensitive[key];
		}

		public TypeTable( int capacity )
		{
			m_Sensitive = new Hashtable( capacity );
			m_Insensitive = new Hashtable( capacity, CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default );
		}
	}
}
