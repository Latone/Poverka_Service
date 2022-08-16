using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Poverka_Service
{
    class TCPSocketListener
    {
        public static String DEFAULT_FILE_STORE_LOC = "C:\\TCP\\";

        public enum STATE { FILE_NAME_READ, DATA_READ, FILE_CLOSED };

        private Socket m_clientSocket = null;
        private Thread m_clientListenerThread = null;

        /// <summary>
		/// Variables that are accessed by other classes indirectly.
		/// </summary>
        private bool m_stopClient = false;
        private bool m_markedForDeletion = false;

        /// <summary>
        /// Working Variables.
        /// </summary>
        private StringBuilder m_oneLineBuf = new StringBuilder();
        private STATE m_processState = STATE.FILE_NAME_READ;
        private long m_totalClientDataSize = 0;
        private StreamWriter m_cfgFile = null;
        private DateTime m_lastReceiveDateTime;
        private DateTime m_currentReceiveDateTime;

        public TCPSocketListener(Socket clientSocket)
        {
            m_clientSocket = clientSocket;
        }

        //Let us see the 'StartSocketListener' method.
        public void StartSocketListener()
        {
            if (m_clientSocket != null)
            {
                m_clientListenerThread =
                  new Thread(new ThreadStart(SocketListenerThreadStart));

                m_clientListenerThread.Start();
            }
        }
        private void SocketListenerThreadStart()
        {
            int size = 0;
            Byte[] byteBuffer = new Byte[1024];

            m_lastReceiveDateTime = DateTime.Now;
            m_currentReceiveDateTime = DateTime.Now;

            Timer t = new Timer(new TimerCallback(CheckClientCommInterval),
              null, 15000, 15000);

            while (!m_stopClient)
            {
                try
                {
                    size = m_clientSocket.Receive(byteBuffer);
                    m_currentReceiveDateTime = DateTime.Now;
                    ParseReceiveBuffer(byteBuffer, size);
                }
                catch (SocketException se)
                {
                    m_stopClient = true;
                    m_markedForDeletion = true;
                }
            }
            t.Change(Timeout.Infinite, Timeout.Infinite);
            t = null;
        }
        public void StopSocketListener()
        {
            if (m_clientSocket != null)
            {
                m_stopClient = true;
                m_clientSocket.Close();

                // Wait for one second for the the thread to stop.
                m_clientListenerThread.Join(1000);

                // If still alive; Get rid of the thread.
                if (m_clientListenerThread.IsAlive)
                {
                    m_clientListenerThread.Abort();
                }
                m_clientListenerThread = null;
                m_clientSocket = null;
                m_markedForDeletion = true;
            }
        }
        private void ParseReceiveBuffer(Byte[] byteBuffer, int size)
        {
            string data = Encoding.ASCII.GetString(byteBuffer, 0, size);
            int lineEndIndex = 0;

            // Check whether data from client has more than one line of 
            // information, where each line of information ends with "CRLF"
            // ("\r\n"). If so break data into different lines and process
            // separately.
            do
            {
                lineEndIndex = data.IndexOf("\r\n");
                if (lineEndIndex != -1)
                {
                    m_oneLineBuf = m_oneLineBuf.Append(data, 0, lineEndIndex + 2);
                    ProcessClientData(m_oneLineBuf.ToString());
                    m_oneLineBuf.Remove(0, m_oneLineBuf.Length);
                    data = data.Substring(lineEndIndex + 2,
                        data.Length - lineEndIndex - 2);
                }
                else
                {
                    // Just append to the existing buffer.
                    m_oneLineBuf = m_oneLineBuf.Append(data);
                }
            } while (lineEndIndex != -1);
        }
        private void ProcessClientData(String oneLine)
        {
            switch (m_processState)
            {
                case STATE.FILE_NAME_READ:
                    m_processState = STATE.DATA_READ;
                    int length = oneLine.Length;
                    if (length <= 2)
                    {
                        m_processState = STATE.FILE_CLOSED;
                        length = -1;
                    }
                    else
                    {
                        try
                        {
                            m_cfgFile = new StreamWriter(DEFAULT_FILE_STORE_LOC + oneLine.Substring(0, length - 2));
                        }
                        catch (Exception e)
                        {
                            m_processState = STATE.FILE_CLOSED;
                            length = -1;
                        }
                    }

                    try
                    {
                        m_clientSocket.Send(BitConverter.GetBytes(length));
                    }
                    catch (SocketException se)
                    {
                        m_processState = STATE.FILE_CLOSED;
                    }
                    break;
                case STATE.DATA_READ:
                    m_totalClientDataSize += oneLine.Length;
                    m_cfgFile.Write(oneLine);
                    m_cfgFile.Flush();
                    if (oneLine.ToUpper().Equals("\r\n"))
                    {
                        try
                        {
                            m_cfgFile.Close();
                            m_cfgFile = null;
                            m_clientSocket.Send(BitConverter.GetBytes(m_totalClientDataSize));
                        }
                        catch (SocketException se)
                        {
                        }
                        m_processState = STATE.FILE_CLOSED;
                        m_markedForDeletion = true;
                    }
                    break;
                case STATE.FILE_CLOSED:
                    break;
                default:
                    break;
            }
        }
        public bool IsMarkedForDeletion()
        {
            return m_markedForDeletion;
        }
        private void CheckClientCommInterval(object o)
        {
            if (m_lastReceiveDateTime.Equals(m_currentReceiveDateTime))
            {
                this.StopSocketListener();
            }
            else
            {
                m_lastReceiveDateTime = m_currentReceiveDateTime;
            }
        }
    }
}
