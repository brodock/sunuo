/***************************************************************************
 *                                 ZLib.cs
 *                            -------------------
 *   begin                : Feb 20, 2005
 *   copyright            : (C) 2005 Max Kellermann <max@duempel.org>
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
using System.Runtime.InteropServices;

/*
 * This is a hack for zlib compatibility issues in Win32 vs Unix. Unix
 * zlib is named "libz.so", not "zlib.dll" and takes "long" length
 * parameters.
 *
 * To enable this, ignore the file ZLib.cs in the scripts, edit etc/sunuo.xml:
 *
 * <libraries>
 *   <library name="legacy">
 *     <ignore-source name="Misc/ZLib.cs" />
 *   </library>
 * </libraries>
 */
namespace Server {
	public enum ZLibError : int {
		Z_OK = 0,
		Z_STREAM_END = 1,
		Z_NEED_DICT = 2,
		Z_ERRNO = (-1),
		Z_STREAM_ERROR = (-2),
		Z_DATA_ERROR = (-3),
		Z_MEM_ERROR = (-4),
		Z_BUF_ERROR = (-5),
		Z_VERSION_ERROR = (-6),
	}

	public enum ZLibCompressionLevel : int {
		Z_NO_COMPRESSION = 0,
		Z_BEST_SPEED = 1,
		Z_BEST_COMPRESSION = 9,
		Z_DEFAULT_COMPRESSION = (-1)
	}

	public class ZLibWin32 {
		[DllImport("zlib")]
		public static extern string zlibVersion();
		[DllImport("zlib")]
		public static extern ZLibError compress(byte[] dest, ref int destLength,
												byte[] source, int sourceLength);
		[DllImport("zlib")]
		public static extern ZLibError compress2(byte[] dest, ref int destLength,
												 byte[] source, int sourceLength,
												 ZLibCompressionLevel level);
		[DllImport("zlib")]
		public static extern ZLibError uncompress(byte[] dest, ref int destLen,
												  byte[] source, int sourceLen);
	}

	public class ZLibUnix {
		[DllImport("z")]
		public static extern string zlibVersion();
		[DllImport("z")]
		public static extern ZLibError compress(byte[] dest, ref long destLength,
												byte[] source, long sourceLength);
		[DllImport("z")]
		public static extern ZLibError compress2(byte[] dest, ref long destLength,
												 byte[] source, long sourceLength,
												 ZLibCompressionLevel level);
		[DllImport("z")]
		public static extern ZLibError uncompress(byte[] dest, ref long destLen,
												  byte[] source, long sourceLen);
	}

	public class ZLib {
		private static bool unix = (int)Environment.OSVersion.Platform == 128;

		public static string zlibVersion() {
			if (unix)
				return ZLibUnix.zlibVersion();
			else
				return ZLibWin32.zlibVersion();
		}

		public static ZLibError compress(byte[] dest, ref int destLength,
										 byte[] source, int sourceLength) {
			if (unix) {
				long dl2 = destLength;
				ZLibError ret = ZLibUnix.compress(dest, ref dl2,
												  source, sourceLength);
				destLength = (int)dl2;
				return ret;
			} else {
				return ZLibWin32.compress(dest, ref destLength,
										  source, sourceLength);
			}
		}

		public static ZLibError compress2(byte[] dest, ref int destLength,
										  byte[] source, int sourceLength,
										  ZLibCompressionLevel level) {
			if (unix) {
				long dl2 = destLength;
				ZLibError ret = ZLibUnix.compress2(dest, ref dl2,
												   source, sourceLength, level);
				destLength = (int)dl2;
				return ret;
			} else {
				return ZLibWin32.compress2(dest, ref destLength,
										   source, sourceLength, level);
			}
		}

		public static ZLibError uncompress(byte[] dest, ref int destLen,
										   byte[] source, int sourceLen) {
			if (unix) {
				long dl2 = destLen;
				ZLibError ret = ZLibUnix.uncompress(dest, ref dl2,
													source, sourceLen);
				destLen = (int)dl2;
				return ret;
			} else {
				return ZLibWin32.uncompress(dest, ref destLen,
											source, sourceLen);
			}
		}
	}
}
