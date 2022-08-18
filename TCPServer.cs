using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Threading;

namespace Poverka_Service
{
    class TCPServer
    {
        private TcpListener m_server = null;
        private int ipNport = -1;
        private ArrayList m_socketListenersList = null;
        private Thread m_serverThread = null;
        private Thread m_purgingThread = null;
        private bool m_stopServer = false;
        private bool m_stopPurging = false;

        public void setPort(int port) {
            ipNport = port;
        }
        public TCPServer(int port,string pathToSave) {
            try
            {
                m_server = new TcpListener(port);
                TCPSocketListener.DEFAULT_FILE_STORE_LOC = pathToSave;
                if (!Directory.Exists(TCPSocketListener.DEFAULT_FILE_STORE_LOC))
                {
                    Directory.CreateDirectory(
                      TCPSocketListener.DEFAULT_FILE_STORE_LOC);
                }
                    
            }
            catch (Exception ex)
            {
                m_server = null;
            }
        }
        public void StartServer()
        {
            if (m_server != null)
            {
                // Create a ArrayList for storing SocketListeners before
                // starting the server.
                m_socketListenersList = new ArrayList();

                // Start the Server and start the thread to listen client 
                // requests.
                m_server.Start();
                m_serverThread = new Thread(new ThreadStart(ServerThreadStart));
                m_serverThread.Start();

                // Create a low priority thread that checks and deletes client
                // SocktConnection objcts that are marked for deletion.
                m_purgingThread = new Thread(new ThreadStart(PurgingThreadStart));
                m_purgingThread.Priority = ThreadPriority.Lowest;
                m_purgingThread.Start();
            }
        }
        public void StopServer()
        {
            if (m_server != null)
            {
                // It is important to Stop the server first before doing
                // any cleanup. If not so, clients might being added as
                // server is running, but supporting data structures
                // (such as m_socketListenersList) are cleared. This might
                // cause exceptions.

                // Stop the TCP/IP Server.
                m_stopServer = true;
                m_server.Stop();

                // Wait for one second for the the thread to stop.
                m_serverThread.Join(1000);

                // If still alive; Get rid of the thread.
                if (m_serverThread.IsAlive)
                {
                    m_serverThread.Abort();
                }
                m_serverThread = null;

                m_stopPurging = true;
                m_purgingThread.Join(1000);
                if (m_purgingThread.IsAlive)
                {
                    m_purgingThread.Abort();
                }
                m_purgingThread = null;

                // Free Server Object.
                m_server = null;

                // Stop All clients.
                StopAllSocketListers();
            }
        }
        /// <summary>
		/// Method that stops all clients and clears the list.
		/// </summary>
		private void StopAllSocketListers()
        {
            foreach (TCPSocketListener socketListener
                         in m_socketListenersList)
            {
                socketListener.StopSocketListener();
            }
            // Remove all elements from the list.
            m_socketListenersList.Clear();
            m_socketListenersList = null;
        }
        private void ServerThreadStart()
        {
            // Client Socket variable;
            Socket clientSocket = null;
            TCPSocketListener socketListener = null;
            while (!m_stopServer)
            {
                try
                {
                    // Wait for any client requests and if there is any 
                    // request from any client accept it (Wait indefinitely).
                    clientSocket = m_server.AcceptSocket();

                    // Create a SocketListener object for the client.
                    socketListener = new TCPSocketListener(clientSocket);

                    // Add the socket listener to an array list in a thread 
                    // safe fashon.
                    //Monitor.Enter(m_socketListenersList);
                    lock (m_socketListenersList)
                    {
                        m_socketListenersList.Add(socketListener);
                    }
                    //Monitor.Exit(m_socketListenersList);

                    // Start a communicating with the client in a different
                    // thread.
                    socketListener.StartSocketListener();
                }
                catch (SocketException se)
                {
                    m_stopServer = true;
                }
            }
        }
        private void PurgingThreadStart()
        {
            while (!m_stopPurging)
            {
                ArrayList deleteList = new ArrayList();

                // Check for any clients SocketListeners that are to be
                // deleted and put them in a separate list in a thread sage
                // fashon.
                //Monitor.Enter(m_socketListenersList);
                lock (m_socketListenersList)
                {
                    foreach (TCPSocketListener socketListener
                                 in m_socketListenersList)
                    {
                        if (socketListener.IsMarkedForDeletion())
                        {
                            deleteList.Add(socketListener);
                            socketListener.StopSocketListener();
                        }
                    }

                    // Delete all the client SocketConnection ojects which are
                    // in marked for deletion and are in the delete list.
                    for (int i = 0; i < deleteList.Count; ++i)
                    {
                        m_socketListenersList.Remove(deleteList[i]);
                    }
                }
                //Monitor.Exit(m_socketListenersList);

                deleteList = null;
                Thread.Sleep(10000);
            }
        }
    }
}
