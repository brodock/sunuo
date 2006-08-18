using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Misc;
using Server.Network;

namespace Server
{
	public class SocketOptions
	{
		private const int CoalesceBufferSize = 512; // MSS that the core will use when buffering packets, a value of 0 will turn this buffering off and Nagle on
		private const bool CoalescePerSlice = true; // If buffering is enabled, should there be a slight delay after the first packet to produce optimal buffering?

		private static int[] m_AdditionalPorts = new int[0];
		//private static int[] m_AdditionalPorts = new int[]{ 2594 };

		public static void Initialize()
		{
			EventSink.SocketConnect += new SocketConnectEventHandler( EventSink_SocketConnect );
			SendQueue.CoalesceBufferSize = CoalesceBufferSize;
			SendQueue.CoalescePerSlice = CoalescePerSlice;

			if ( m_AdditionalPorts.Length > 0 )
				EventSink.ServerStarted += new ServerStartedEventHandler( EventSink_ServerStarted );
		}

		public static void EventSink_ServerStarted()
		{
			for ( int i = 0; i < m_AdditionalPorts.Length; ++i )
				Core.MessagePump.AddListener( new Listener( m_AdditionalPorts[i] ) );
		}

		private static void EventSink_SocketConnect( SocketConnectEventArgs e )
		{
			if ( !e.AllowConnection )
				return;

			Socket s = e.Socket;

			s.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 15000 );
			s.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 15000 );

			if ( CoalesceBufferSize > 0 )
				s.SetSocketOption( SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1 ); // RunUO uses its own algorithm
		}
	}
}