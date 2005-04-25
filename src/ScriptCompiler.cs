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
				foreach (Library l in libraries) {
					assemblies.Add(l.Assembly);
				}
				return (Assembly[])assemblies.ToArray(typeof(Assembly));
			}
		}

		private static ArrayList m_AdditionalReferences;

		private static string[] GetReferenceAssemblies() {
			ArrayList list = new ArrayList();

			string path = Path.Combine( Core.BaseDirectory, "Data/Assemblies.cfg" );

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

		private static CompilerResults CompileCSScripts(string name,
														ICollection fileColl,
														string assemblyFile,
														LibraryConfig libConfig,
														bool debug) {
			CSharpCodeProvider provider = new CSharpCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files;

			Console.Write("{0}[C#,{1}", name, fileColl.Count);

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

			CompilerResults results = compiler.CompileAssemblyFromFileBatch( parms, files );

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

				Console.WriteLine();
				if ( errorCount > 0 )
					Console.WriteLine( "failed ({0} errors, {1} warnings)", errorCount, warningCount );
				else
					Console.WriteLine( "done ({0} errors, {1} warnings)", errorCount, warningCount );

				foreach ( CompilerError e in results.Errors )
				{
					Console.WriteLine( " - {0}: {1}: {2}: (line {3}, column {4}) {5}", e.IsWarning ? "Warning" : "Error", e.FileName, e.ErrorNumber, e.Line, e.Column, e.ErrorText );
				}
			}
			else
			{
				Console.Write("] ");
			}

			return results;
		}

		private static CompilerResults CompileVBScripts(string name,
														ICollection fileColl,
														string assemblyFile,
														LibraryConfig libConfig,
														bool debug) {
			VBCodeProvider provider = new VBCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files = new string[fileColl.Count];
			fileColl.CopyTo(files, 0);

			Console.Write("{0}[VB,{1}", name, files.Length);

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

				Console.WriteLine();
				if ( errorCount > 0 )
					Console.WriteLine( "failed ({0} errors, {1} warnings)", errorCount, warningCount );
				else
					Console.WriteLine( "done ({0} errors, {1} warnings)", errorCount, warningCount );

				foreach ( CompilerError e in results.Errors )
				{
					Console.WriteLine( " - {0}: {1}: {2}: (line {3}, column {4}) {5}", e.IsWarning ? "Warning" : "Error", e.FileName, e.ErrorNumber, e.Line, e.Column, e.ErrorText );
				}
			}
			else
			{
				Console.Write("] ");
			}

			return results;
		}

		private static bool Compile(LibraryConfig libConfig,
                                            bool debug) {
			string name = libConfig.Name;
			string path = libConfig.SourcePath == null
                            ? null : libConfig.SourcePath.FullName;

			/* honor the Disabled flag */
			if (libConfig.Disabled)
				return true;

			if (libConfig.Name == "core")
				return true;

			/* check if there is source code for this library */
			if (libConfig.SourcePath == null) {
				if (libConfig.BinaryPath == null) {
					Console.WriteLine("Warning: library {0} does not exist",
									  libConfig.Name);
					return true;
				} else if (!libConfig.BinaryPath.Exists) {
					Console.WriteLine("Warning: library {0} does not exist: {1}",
									  libConfig.Name, libConfig.BinaryPath);
					return false;
				}

                                Console.Write("{0}", libConfig.Name);
				libraries.Add(new Library(libConfig, libConfig.Name,
                                                          Assembly.LoadFrom(libConfig.SourcePath.FullName)));
                                m_AdditionalReferences.Add(libConfig.SourcePath.FullName);
                                Console.Write(". ");
                                return true;
			} else if (!libConfig.SourcePath.Exists) {
				Console.WriteLine("Warning: library {0} does not exist: {1}",
								  libConfig.Name, libConfig.SourcePath);
			}

			DirectoryInfo cache = Core.CacheDirectoryInfo
				.CreateSubdirectory("lib")
				.CreateSubdirectory(name);

			if (!cache.Exists) {
				Console.WriteLine("Failed to create directory {0}", cache.FullName);
				return false;
			}

			string csFile = Path.Combine(cache.FullName, name + ".dll");
			Hashtable files = GetScripts(libConfig, path, "*.cs");
			if (files.Count > 0) {
				string stampFile = Path.Combine(cache.FullName, name + ".stm");
				if (File.Exists(csFile) && CheckStamps(files, stampFile)) {
					libraries.Add(new Library(libConfig, name,
											  Assembly.LoadFrom(csFile)));
					m_AdditionalReferences.Add(csFile);
					Console.Write("{0}. ", name);
				} else {
					/* work around a serious faction bug: the factions
					   code (Reflector.cs) assumes alphabetical
					   directory entry order; simulate this by sorting
					   the array. See my bug report:
					   http://www.runuo.com/forum/showthread.php?p=373540 */
					ArrayList sorted = new ArrayList(files.Keys);
					sorted.Sort();

					CompilerResults results = CompileCSScripts(name, sorted,
															   csFile,
															   libConfig,
															   debug);
					if (results != null) {
						if (results.Errors.HasErrors)
							return false;
						libraries.Add(new Library(libConfig, name,
												  results.CompiledAssembly));
						WriteStampFile(stampFile, files);
					}
				}
			}

			string vbFile = Path.Combine(cache.FullName, name + "-vb.dll");
			files = GetScripts(libConfig, path, "*.vb");
			if (files.Count > 0) {
				string stampFile = Path.Combine(cache.FullName, name + "-vb.stm");
				if (File.Exists(vbFile) && CheckStamps(files, stampFile)) {
					libraries.Add(new Library(libConfig, name,
											  Assembly.LoadFrom(vbFile)));
					m_AdditionalReferences.Add(vbFile);
					Console.Write("{0}/VB. ", name);
				} else {
					/* workaround again */
					ArrayList sorted = new ArrayList(files.Keys);
					sorted.Sort();

					CompilerResults results = CompileVBScripts(name, sorted, vbFile,
															   libConfig,
															   debug);
					if (results != null) {
						if (results.Errors.HasErrors)
							return false;
						libraries.Add(new Library(libConfig, name,
												  results.CompiledAssembly));
					}
				}
			}

			return true;
		}

		public static bool Compile( bool debug )
		{
			Console.Write("Compiling scripts: ");

			if (m_AdditionalReferences != null)
				throw new ApplicationException("already compiled");

			m_AdditionalReferences = new ArrayList();

			libraries = new ArrayList();
			libraries.Add(new Library(Core.Config.GetLibraryConfig("core"),
									  "core", Core.Assembly));
			m_AdditionalReferences.Add(Core.ExePath);

			/* first compile ./Scripts/ for RunUO compatibility */
			LibraryConfig legacyConfig = Core.Config.GetLibraryConfig("legacy");
			if (legacyConfig != null && legacyConfig.SourcePath != null &&
                            legacyConfig.SourcePath.Exists) {
				bool result = Compile(legacyConfig, debug);
				if (!result)
					return false;
			}

			/* now compile all libraries in ./local/src/ */
			foreach (LibraryConfig libConfig in Core.Config.Libraries) {
				if (libConfig.Name == "legacy")
					continue;

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
			Console.WriteLine();

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
			foreach (Library l in libraries) {
				Type type = GetTypeCache(l.Assembly).GetTypeByFullName(fullName, ignoreCase);
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
			foreach (Library l in libraries) {
				Type type = GetTypeCache(l.Assembly).GetTypeByName(name, ignoreCase);
				if (type != null)
					return type;
			}

			return null;
		}

		private static Hashtable GetScripts(LibraryConfig libConfig,
											string path, string type) {
			Hashtable list = new Hashtable();

			GetScripts(libConfig, list, path, type);

			return list;
		}

		private static void GetScripts(LibraryConfig libConfig,
									   Hashtable list, string path, string type) {
			foreach ( string dir in Directory.GetDirectories( path ) )
				GetScripts(libConfig, list, dir, type);

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
