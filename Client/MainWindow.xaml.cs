using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
// using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;

using System.Net;
using System.Net.Sockets;
using System.IO;
using ServerData;
using System.Windows.Threading;

//using ServerData;
namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static Socket socket;
        public static IPAddress ipAdress;
        public static string ID;
        public static string login;
        public static Thread thread;
        public static bool isConnected = false;


        public MainWindow()
        {
            InitializeComponent();
        }


        /// <summary>
        /// on window close event update server for close connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (isConnected)
            {
                Packet p = new Packet(PacketType.CloseConnection, ID);

                p.data.Add(login);
                p.data.Add("Exit from Chat");

                socket.Send(p.ToBytes());

                socket.Close();
                isConnected = false;
                thread.Abort();
            }
        }


        /// <summary>
        /// connect Button click event
        /// validate login and ip adress
        /// connect if valid and start new thread for receiving data from server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Login.Text))//login validation
            {
                LoginRequireShowMsg();
            }
            else if (!IPAddress.TryParse(serverIP.Text, out ipAdress))//ip validation
            {
                ValidIPRequireShowMsg();
            }
            else//connection to server
            {
                ClearRequireMsg();

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAdress, 4242);

                try
                {
                    socket.Connect(ipEndPoint);
                    login = Login.Text;

                    isConnected = true;
                    ConnectBtn.IsEnabled = false;
                    SendBtn.IsEnabled = true;

                    thread = new Thread(Data_IN);
                    thread.Start();
                }
                catch (SocketException ex)
                {
                    AddMsgToBoard("Error during connecting to server", "System");
                }
            }
        }


        /// <summary>
        /// send message to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            string msg = Msg.Text;
            Msg.Text = string.Empty;

            Packet p = new Packet(PacketType.Chat, ID);
            p.data.Add(login);
            p.data.Add(msg);
            socket.Send(p.ToBytes());
        }


        /// <summary>
        /// receive data from socket
        /// then data received call to data manager
        /// </summary>
        private void Data_IN()
        {
            byte[] buffer;
            int readBytes;

            while (true)
            {
                try
                {
                    buffer = new byte[socket.SendBufferSize];
                    readBytes = socket.Receive(buffer);

                    if (readBytes > 0)
                    {
                        DataManager(new Packet(buffer));
                    }
                }
                catch (SocketException ex)
                {
                    ConnectionToServerLost();
                }

            }
        }


        /// <summary>
        /// manage all received packages by PacketType
        /// </summary>
        /// <param name="p"></param>
        private void DataManager(Packet p)
        {
            switch (p.packetType)
            {
                case PacketType.Registration:
                    ID = p.data[0];
                    Packet packet = new Packet(PacketType.Chat, ID);
                    packet.data.Add(login);
                    packet.data.Add("Enter to Chat");
                    socket.Send(packet.ToBytes());
                    break;


                case PacketType.Chat:
                case PacketType.CloseConnection:
                    AddMsgToBoard(p.data[1], p.data[0]);
                    break;
            }
        }


        /// <summary>
        /// Server connection lost
        /// add message to user, close socket and thread
        /// enable to user try to connect to server
        /// </summary>
        private void ConnectionToServerLost()
        {
            AddMsgToBoard("Server disconnected", "Server");

            this.Dispatcher.Invoke(new Action(() =>
            {
                ConnectBtn.IsEnabled = true;
                SendBtn.IsEnabled = false;
            }));

            socket.Close();
            thread.Abort();
        }

        /// <summary>
        /// add new message to chat board
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        private void AddMsgToBoard(string msg, string user)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                MsgBoard.Text += Environment.NewLine +
                                 DateTime.Now.ToString() +
                                 " " +
                                 user +
                                 " say: " +
                                 msg;
                MsgBoardScroll.ScrollToEnd();
                MsgBoard.UpdateLayout();
            }));

        }


        /// <summary>
        /// show validation message for login
        /// </summary>
        private void LoginRequireShowMsg()
        {
            IPRequire.Visibility = System.Windows.Visibility.Hidden;
            ValidIPRequire.Visibility = System.Windows.Visibility.Hidden;

            LoginRequire.Visibility = System.Windows.Visibility.Visible;
        }
        /// <summary>
        /// show validation message for IP
        /// </summary>
        private void ValidIPRequireShowMsg()
        {
            LoginRequire.Visibility = System.Windows.Visibility.Hidden;
            IPRequire.Visibility = System.Windows.Visibility.Hidden;

            ValidIPRequire.Visibility = System.Windows.Visibility.Visible;

        }
        /// <summary>
        /// hidde all validation messages
        /// </summary>
        private void ClearRequireMsg()
        {
            LoginRequire.Visibility = System.Windows.Visibility.Hidden;
            IPRequire.Visibility = System.Windows.Visibility.Hidden;
            ValidIPRequire.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
