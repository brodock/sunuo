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
		private static ArrayList m_Assemblies = new ArrayList();

		public static Assembly[] Assemblies
		{
			get
			{
				return (Assembly[])m_Assemblies.ToArray(typeof(Assembly));
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
														bool debug) {
			CSharpCodeProvider provider = new CSharpCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files = GetScripts(sourcePath, "*.cs");

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
														bool debug) {
			VBCodeProvider provider = new VBCodeProvider();
			ICodeCompiler compiler = provider.CreateCompiler();

			string[] files = GetScripts(sourcePath, "*.vb");

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

		private static bool Compile(string name, string path, bool debug) {
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
				m_Assemblies.Add(Assembly.LoadFrom(csFile));
				m_AdditionalReferences.Add(csFile);
				Console.Write("{0}. ", name);
			} else if (File.Exists(oldFile)) {
				/* old style file name, rename that */
				File.Move(oldFile, csFile);
				m_Assemblies.Add(Assembly.LoadFrom(csFile));
				m_AdditionalReferences.Add(csFile);
				Console.Write("{0}. ", name);
			} else {
				CompilerResults results = CompileCSScripts(name, path, csFile,
														   debug);
				if (results != null) {
					if (results.Errors.HasErrors)
						return false;
					m_Assemblies.Add(results.CompiledAssembly);
				}
			}

			string vbFile = Path.Combine(cache.FullName, name + "-vb.dll");
			if (File.Exists(vbFile)) {
				m_Assemblies.Add(Assembly.LoadFrom(vbFile));
				m_AdditionalReferences.Add(vbFile);
				Console.Write("{0}/VB. ", name);
			} else {
				CompilerResults results = CompileVBScripts(name, path, vbFile, debug);
				if (results != null) {
					if (results.Errors.HasErrors)
						return false;
					m_Assemblies.Add(results.CompiledAssembly);
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

			if ( m_AdditionalReferences.Count > 0 )
				m_AdditionalReferences.Clear();

			/* first compile ./Scripts/ for RunUO compatibility */
			string compatScripts = Path.Combine(Core.BaseDirectory, "Scripts");
			if (Directory.Exists(compatScripts)) {
				bool result = Compile("runuo_compat", compatScripts, debug);
				if (!result)
					return false;
			}

			/* now compile all libraries in ./local/src/ */
			DirectoryInfo srcDir = Core.LocalDirectoryInfo
				.CreateSubdirectory("src");
			foreach (DirectoryInfo sub in srcDir.GetDirectories()) {
				bool result = Compile(sub.Name, sub.FullName, debug);
				if (!result)
					return false;
			}

			Console.WriteLine();

			Console.Write( "Scripts: Verifying..." );
			Core.VerifySerialization();
			Console.WriteLine( "done ({0} items, {1} mobiles)", Core.ScriptItems, Core.ScriptMobiles );

			return true;
		}

		public static void Configure() {
			ArrayList invoke = new ArrayList();

			foreach (Assembly a in m_Assemblies) {
				Type[] types = a.GetTypes();

				for ( int i = 0; i < types.Length; ++i )
				{
					MethodInfo m = types[i].GetMethod( "Configure", BindingFlags.Static | BindingFlags.Public );

					if ( m != null )
						invoke.Add( m );
					//m.Invoke( null, null );
				}
			}

			invoke.Sort( new CallPriorityComparer() );

			for ( int i = 0; i < invoke.Count; ++i )
				((MethodInfo)invoke[i]).Invoke( null, null );
		}

		public static void Initialize() {
			ArrayList invoke = new ArrayList();

			foreach (Assembly a in m_Assemblies) {
				Type[] types = a.GetTypes();

				for ( int i = 0; i < types.Length; ++i )
				{
					MethodInfo m = types[i].GetMethod( "Initialize", BindingFlags.Static | BindingFlags.Public );

					if ( m != null )
						invoke.Add( m );
					//m.Invoke( null, null );
				}
			}

			invoke.Sort( new CallPriorityComparer() );

			for ( int i = 0; i < invoke.Count; ++i )
				((MethodInfo)invoke[i]).Invoke( null, null );
		}

		private static Hashtable m_TypeCaches = new Hashtable();
		private static TypeCache m_NullCache;

		public static TypeCache GetTypeCache( Assembly asm )
		{
			if ( asm == null )
			{
				if ( m_NullCache == null )
					m_NullCache = new TypeCache( null );

				return m_NullCache;
			}

			TypeCache c = (TypeCache)m_TypeCaches[asm];

			if ( c == null )
				m_TypeCaches[asm] = c = new TypeCache( asm );

			return c;
		}

		public static Type FindTypeByFullName( string fullName )
		{
			return FindTypeByFullName( fullName, true );
		}

		public static Type FindTypeByFullName( string fullName, bool ignoreCase )
		{
			foreach (Assembly a in m_Assemblies) {
				Type type = GetTypeCache(a).GetTypeByFullName( fullName, ignoreCase );
				if (type != null)
					return type;
			}

			return GetTypeCache( Core.Assembly ).GetTypeByFullName( fullName, ignoreCase );
		}

		public static Type FindTypeByName( string name )
		{
			return FindTypeByName( name, true );
		}

		public static Type FindTypeByName( string name, bool ignoreCase )
		{
			foreach (Assembly a in m_Assemblies) {
				Type type = GetTypeCache(a).GetTypeByName( name, ignoreCase );
				if (type != null)
					return type;
			}

			return GetTypeCache( Core.Assembly ).GetTypeByName( name, ignoreCase );
		}

		private static string[] GetScripts(string path, string type) {
			ArrayList list = new ArrayList();

			GetScripts(list, path, type);

			return (string[])list.ToArray( typeof( string ) );
		}

		private static void GetScripts( ArrayList list, string path, string type )
		{
			foreach ( string dir in Directory.GetDirectories( path ) )
				GetScripts( list, dir, type );

			list.AddRange( Directory.GetFiles( path, type ) );
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

		public TypeCache( Assembly asm )
		{
			if ( asm == null )
				m_Types = Type.EmptyTypes;
			else
				m_Types = asm.GetTypes();

			m_Names = new TypeTable( m_Types.Length );
			m_FullNames = new TypeTable( m_Types.Length );

			Type typeofTypeAliasAttribute = typeof( TypeAliasAttribute );

			for ( int i = 0; i < m_Types.Length; ++i )
			{
				Type type = m_Types[i];

				m_Names.Add( type.Name, type );
				m_FullNames.Add( type.FullName, type );

				if ( type.IsDefined( typeofTypeAliasAttribute, false ) )
				{
					object[] attrs = type.GetCustomAttributes( typeofTypeAliasAttribute, false );

					if ( attrs != null && attrs.Length > 0 )
					{
						TypeAliasAttribute attr = attrs[0] as TypeAliasAttribute;

						if ( attr != null )
						{
							for ( int j = 0; j < attr.Aliases.Length; ++j )
								m_FullNames.Add( attr.Aliases[j], type );
						}
					}
				}
			}
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
