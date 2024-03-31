using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Verhaeg.IoT.xAP.Client.Managers
{
    public class EventManager : Processor.TaskManager
    {

        // SingleTon
        private static EventManager? _instance = default;
        private static readonly object padlock = new object();

        // Connection details
        private int port;
        private IPAddress ip;
        private UdpClient uc;

        // Event
        public event EventHandler<string>? xap_event;

        private EventManager(int port, string ip) : base("EventManager_xAP")
        {
            Log.Debug("Starting xAP EventManager on port " + port);
            this.ip = IPAddress.Parse(ip);
            this.port = port;
            uc = new UdpClient(port);
        }

        public static EventManager Instance(int port, string ip)
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    _instance = new EventManager(port, ip);
                }
                return _instance;
            }
        }


        protected override void Process()
        {
            System.Threading.Thread.Sleep(2000);
            BeginReceive();
        }

        private void BeginReceive()
        {
            try
            {
                Log.Information("Opening port: " + port);
                uc.BeginReceive(new AsyncCallback(Receive), null);
                Log.Debug("Received UDP packet on port: " + port);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start UDP listener on port " + port);
                Log.Debug(ex.ToString());

                // Try again
                System.Threading.Thread.Sleep(5000);
                BeginReceive();
            }
        }

        private void Receive(IAsyncResult iar)
        {
            Log.Debug("Read message from IPEndPoint.");
            try
            {
                IPEndPoint ipe = new IPEndPoint(ip, 0);
                byte[] bReceived = uc.EndReceive(iar, ref ipe);
                string strReceived = Encoding.ASCII.GetString(bReceived);
                Log.Debug("Content: \n" + strReceived);
                if (xap_event != null)
                {
                    xap_event(this, strReceived);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Was not able to read message from IPEndPoint.");
                Log.Debug(ex.ToString());
            }
            finally
            {
                Log.Debug("Waiting for next xAP message...");
                BeginReceive();
            }
        }
    }
}
