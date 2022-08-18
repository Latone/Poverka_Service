using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Sockets;

namespace Poverka_Service
{
    public partial class Poverka_TCP_Listener : ServiceBase
    {
        private EventLog eventLog1;
        private int eventId = 1;
        private TCPServer server;
        public Poverka_TCP_Listener(string[] args)
        {
            InitializeComponent();

           /* string eventSourceName = "MySource";
            string logName = "MyNewLog";

            if (args.Length > 0)
            {
                eventSourceName = args[0];
                //server = new TCPServer(int.Parse(args[0]));
            }

            if (args.Length > 1)
            {
                logName = args[1];
            }

            eventLog1 = new EventLog();

            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, logName);
            }

            eventLog1.Source = "MySource";
            eventLog1.Log = "MyNewLog";*/
        }

        protected override void OnStart(string[] args)
        {
           // eventLog1.WriteEntry("Something onstart");
            if (args.Length > 0)
            {
                //eventSourceName = args[0];
                server = new TCPServer(int.Parse(args[0]), args[1]);
            }
            else
                server = new TCPServer(30001, "C:\\TCP\\");
            //server.setPort(30001);
            server.StartServer();

            /*Timer timer = new Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();*/
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.
            //eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
        }

        protected override void OnStop()
        {
            server.StopServer();
            server = null;

          //  eventLog1.WriteEntry("In OnStop.");
        }
    }
}
