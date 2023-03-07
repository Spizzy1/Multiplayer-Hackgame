using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Hackerman
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Select username");
            string Username = Console.ReadLine();
            MainMenu();

            void MainMenu()
            {
                begin:
                Console.Clear();
                Console.WriteLine("Welcome: " + Username + " to test P2P hacking game");
                Console.WriteLine("Do you want to check information, create a session or join a session?");
                Console.WriteLine("create a session: C or Create");
                Console.WriteLine("join a session: J or Join");
                Console.WriteLine("information: inf, info or information");
                string command = Console.ReadLine();
                command.ToLower();

                if(command != "c" && command != "create" && command != "j" && command != "join" && command != "inf" && command != "info" && command != "information")
                {
                    Console.Clear();
                    Console.WriteLine("Invalid command input, returning to main menu.");
                    Console.ReadLine();
                    goto begin;
                }
                else
                {
                    Initialize(command);
                }
            }
            void Initialize(string pickedCommand)
            {

                Console.Clear();
                Console.WriteLine("Command chosen: " + pickedCommand);
                Console.Read();
                switch (pickedCommand)
                {
                    case "c":
                    case "create":
                        CreateSession();
                        break;
                    case "j":
                    case "join":
                        JoinSession();
                        break;
                    case "inf":
                    case "info":
                    case "information":
                        InfoMenu();
                        break;

                }
            }
            void InfoMenu()
            {

            }
            void CreateSession()
            {
                Process p = new Process();
                string dir = Directory.GetCurrentDirectory();
                p.StartInfo.FileName = Path.Combine(dir + @"\Server.exe");
                p.StartInfo.Arguments = "echo Hello!";
                p.StartInfo.CreateNoWindow = false;
                Console.WriteLine(p.StartInfo.FileName);
                string test = Console.ReadLine();
                p.Start();

                Client client = null;

                while (true)
                {
                    Console.WriteLine("Trying to connect to server...");
                    try
                    {
                        Int32 port = 13000;
                        IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                        client = new Client(port, "127.0.0.1");
                        Console.WriteLine("Connected to local server...");
                        break;
                    }
                    catch
                    {

                    }
                }
                Thread.Sleep(100000);
            }
            void JoinSession()
            {

            }
            void PlayGame()
            {

            }
        }
    }
    public class Client {

        // Called by producers to send data over the socket.
        public void SendData(byte[] data)
        {
            _sender.SendData(data);
        }

        // Consumers register to receive data.
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public Client(Int32 port, string IP)
        {
            _client = new TcpClient(IP, port);
            _stream = _client.GetStream();

            _receiver = new Receiver(_stream);
            _sender = new Sender(_stream);

            _receiver.DataReceived += OnDataReceived;
        }
        private TcpClient _client;
        private NetworkStream _stream;

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            var handler = DataReceived;
            if (handler != null) DataReceived(this, e);  // re-raise event
        }
        private sealed class Receiver
        {
            internal event EventHandler<DataReceivedEventArgs> DataReceived;

            internal Receiver(NetworkStream stream)
            {
                _stream = stream;
                _thread = new Thread(Run);
                _thread.Start();
            }
            private AutoResetEvent ShutdownEvent = new AutoResetEvent(false);
            private void Run()
            {
                try
                {
                    // ShutdownEvent is a ManualResetEvent signaled by
                    // Client when its time to close the socket.
                    while (!ShutdownEvent.WaitOne(0))
                    {
                        try
                        {
                            // We could use the ReadTimeout property and let Read()
                            // block.  However, if no data is received prior to the
                            // timeout period expiring, an IOException occurs.
                            // While this can be handled, it leads to problems when
                            // debugging if we are wanting to break when exceptions
                            // are thrown (unless we explicitly ignore IOException,
                            // which I always forget to do).
                             byte[] _data = new byte[1024];
                            StringBuilder myCompleteMessage = new StringBuilder();
                            if (!_stream.DataAvailable)
                            {
                                // Give up the remaining time slice.
                                Thread.Sleep(1);
                            }
                            else
                            {

                                int readData = _stream.Read(_data, 0, _data.Length);
                                if (readData > 0)
                                {
                                    do
                                    {
                                        myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(_data, 0, readData));
                                    } while (_stream.DataAvailable);
                                    Console.WriteLine(myCompleteMessage);
                                    Thread.Sleep(10);
                                }
                                else
                                {
                                    ShutdownEvent.Set();
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            // Handle the exception...
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle the exception...
                }
                finally
                {
                    _stream.Close();
                }
            }

            private NetworkStream _stream;
            private Thread _thread;
        }
        void deCodeData()
        {

        }
        private sealed class Sender
        {
            internal void SendData(byte[] data)
            {
                // transition the data to the thread and send it...
            }

            internal Sender(NetworkStream stream)
            {
                _stream = stream;
                _thread = new Thread(Run);
                _thread.Start();
            }

            private void Run()
            {
                // main thread loop for sending data...
            }

            private NetworkStream _stream;
            private Thread _thread;
        }
        private Receiver _receiver;
        private Sender _sender;
    }
}
