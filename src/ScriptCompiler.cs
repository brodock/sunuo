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

		private static ArrayList m_AdditionalReferences = new ArrayList();

		public static string[] GetReferenceAssemblies()
		{
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

			list.Add( Core.ExePath );

			list.AddRange( m_AdditionalReferences );

			return (string[])list.ToArray( typeof( string ) );
		}

		private static CompilerResults CompileCSScripts(string name,
														string sourcePath,
														string assemblyFile,
														LibraryConfig libConfig,
														bool debug) {
			CSharpCodeProvider provider = new CSharpCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files = GetScripts(libConfig, sourcePath, "*.cs");

			if ( files.Length == 0 )
				return null;

			Console.Write("{0}[C#,{1}", name, files.Length);

			string tempFile = compiler.GetType().FullName == "Mono.CSharp.CSharpCodeCompiler"
				? Path.GetTempFileName() : null;
			if (tempFile == String.Empty)
				tempFile = null;
			if (tempFile != null) {
				/* to prevent an "argument list too long" error, we
				   write a list of file names to a temporary file
				   and add them with @filename */
				StreamWriter w = new StreamWriter(tempFile, false);
				foreach (string file in files) {
					w.Write("\"" + file + "\" ");
				}
				w.Close();

				files = new string[]{"@" + tempFile};
			}

			CompilerParameters parms = new CompilerParameters( GetReferenceAssemblies(), assemblyFile, debug );

			if ( !debug )
				parms.CompilerOptions = "/debug- /optimize+"; // doesn't seem to have any effect

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
														string sourcePath,
														string assemblyFile,
														LibraryConfig libConfig,
														bool debug) {
			VBCodeProvider provider = new VBCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files = GetScripts(libConfig, sourcePath, "*.vb");

			if ( files.Length == 0 )
				return null;

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

		private static bool Compile(string name, string path,
									LibraryConfig libConfig,
									bool debug) {
			DirectoryInfo cache = Core.CacheDirectoryInfo
				.CreateSubdirectory("lib")
				.CreateSubdirectory(name);

			if (!cache.Exists) {
				Console.WriteLine("Failed to create directory {0}", cache.FullName);
				return false;
			}

			string oldFile = Path.Combine(cache.FullName, name + "-cs.dll");
			string csFile = Path.Combine(cache.FullName, name + ".dll");
			if (File.Exists(csFile)) {
				libraries.Add(new Library(libConfig, name,
										  Assembly.LoadFrom(csFile)));
				m_AdditionalReferences.Add(csFile);
				Console.Write("{0}. ", name);
			} else if (File.Exists(oldFile)) {
				/* old style file name, rename that */
				File.Move(oldFile, csFile);
				libraries.Add(new Library(libConfig, name,
										  Assembly.LoadFrom(csFile)));
				m_AdditionalReferences.Add(csFile);
				Console.Write("{0}. ", name);
			} else {
				CompilerResults results = CompileCSScripts(name, path, csFile,
														   libConfig,
														   debug);
				if (results != null) {
					if (results.Errors.HasErrors)
						return false;
					libraries.Add(new Library(libConfig, name,
											  results.CompiledAssembly));
				}
			}

			string vbFile = Path.Combine(cache.FullName, name + "-vb.dll");
			if (File.Exists(vbFile)) {
				libraries.Add(new Library(libConfig, name,
										  Assembly.LoadFrom(vbFile)));
				m_AdditionalReferences.Add(vbFile);
				Console.Write("{0}/VB. ", name);
			} else {
				CompilerResults results = CompileVBScripts(name, path, vbFile,
														   libConfig,
														   debug);
				if (results != null) {
					if (results.Errors.HasErrors)
						return false;
					libraries.Add(new Library(libConfig, name,
											  results.CompiledAssembly));
				}
			}

			return true;
		}

		public static bool Compile()
		{
			return Compile( false );
		}

		public static bool Compile( bool debug )
		{
			Console.Write("Compiling scripts: ");

			libraries = new ArrayList();
			libraries.Add(new Library(Core.Config.GetLibraryConfig("core"),
									  "core", Core.Assembly));

			if ( m_AdditionalReferences.Count > 0 )
				m_AdditionalReferences.Clear();

			/* rename old files for backwards compatibility */
			DirectoryInfo cacheDir = Core.CacheDirectoryInfo
				.CreateSubdirectory("lib");
			string compatDir = Path.Combine(cacheDir.FullName, "runuo_compat");
			if (Directory.Exists(compatDir)) {
				string newDir = Path.Combine(cacheDir.FullName, "legacy");
				File.Move(Path.Combine(compatDir, "runuo_compat.dll"),
						  Path.Combine(compatDir, "legacy.dll"));
				Directory.Move(compatDir, newDir);
			}

			/* first compile ./Scripts/ for RunUO compatibility */
			string compatScripts = Path.Combine(Core.BaseDirectory, "Scripts");
			if (Directory.Exists(compatScripts)) {
				bool result = Compile("legacy", compatScripts,
									  Core.Config.GetLibraryConfig("legacy"),
									  debug);
				if (!result)
					return false;
			}

			/* now compile all libraries in ./local/src/ */
			DirectoryInfo srcDir = Core.LocalDirectoryInfo
				.CreateSubdirectory("src");
			foreach (DirectoryInfo sub in srcDir.GetDirectories()) {
				bool result = Compile(sub.Name.ToLower(), sub.FullName,
									  Core.Config.GetLibraryConfig(sub.Name),
									  debug);
				if (!result)
					return false;
			}

			/* delete unused cache directories */
			foreach (DirectoryInfo sub in cacheDir.GetDirectories()) {
				if (GetLibrary(sub.Name) == null)
					sub.Delete(true);
			}

			/* load libraries from ./local/lib/ */
			DirectoryInfo libDir = Core.LocalDirectoryInfo
				.CreateSubdirectory("lib");
			foreach (FileInfo libFile in libDir.GetFiles("*.dll")) {
				string fileName = libFile.Name;
				string libName = fileName.Substring(0, fileName.Length - 4);
				if (GetLibrary(libName) != null) {
					Console.WriteLine("Warning: duplicate library '{0}' in ./local/src/{1}/ and ./local/lib/{2}",
									  libName, libName, fileName);
					continue;
				}

				libraries.Add(new Library(Core.Config.GetLibraryConfig(libName),
										  libName,
										  Assembly.LoadFrom(libFile.FullName)));
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

		private static string[] GetScripts(LibraryConfig libConfig,
										   string path, string type) {
			ArrayList list = new ArrayList();

			GetScripts(libConfig, list, path, type);

			return (string[])list.ToArray( typeof( string ) );
		}

		private static void GetScripts(LibraryConfig libConfig,
									   ArrayList list, string path, string type) {
			foreach ( string dir in Directory.GetDirectories( path ) )
				GetScripts(libConfig, list, dir, type);

			foreach (string filename in Directory.GetFiles(path, type)) {
				/* XXX: pass relative filename only */
				if (libConfig == null || !libConfig.GetIgnoreSource(filename))
					list.Add(filename);
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
