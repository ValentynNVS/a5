using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

namespace TCPIPClient
{
    public partial class MainWindow : Window
    {
        static string sessionId;
        static bool connected;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a TcpClient.
            // Note, for this client to work you need to have a TcpServer 
            // connected to the same address as specified by the server, port
            // combination.
            Int32 port = 0;
            Int32.TryParse(PortTextBox.Text, out port);
            String server = IpAddressTextBox.Text;
            String name = NameTextBox.Text;
            Int32 timeLimit;
            Int32.TryParse(TimeLimitTextBox.Text, out timeLimit);

            if (server == "" || port == 0 || name == "" || timeLimit == 0)
            {
                MessageBox.Show("Please fill in info first.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClient client = new TcpClient(server, port);

            if (client.Connected == false)
            {
                MessageBox.Show("Unable to connect.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Translate the passed message into ASCII and store it as a Byte array.
            String message = "CreatePlayerSession";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

            // Get a client stream for reading and writing.
            //  Stream stream = client.GetStream();

            NetworkStream stream = client.GetStream();

            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Sent: {0}", message);

            // Receive the TcpServer.response.

            // Buffer to store the response bytes.
            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            TargetWordTextBlock.Text = responseData;
            sessionId = responseData.Split('|')[2];
            Console.WriteLine("Received: {0}", responseData);
            connected = true;

            // Close everything.
            stream.Close();
            client.Close();
        }

        private void SubmitGuessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                MessageBox.Show("You are not connected to a server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Create a TcpClient.
            // Note, for this client to work you need to have a TcpServer 
            // connected to the same address as specified by the server, port
            // combination.
            Int32 port = 13000;
            String server = IpAddressTextBox.Text;
            TcpClient client = new TcpClient(server, port);

            // Translate the passed message into ASCII and store it as a Byte array.
            string guess = GuessTextBox.Text;

            String message = "MakeGuess|" + guess + "|" + sessionId;
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

            // Get a client stream for reading and writing.
            //  Stream stream = client.GetStream();

            NetworkStream stream = client.GetStream();

            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Sent: {0}", message);

            // Receive the TcpServer.response.

            // Buffer to store the response bytes.
            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            TargetWordTextBlock.Text = responseData;
            Console.WriteLine("Received: {0}", responseData);

            // Close everything.
            stream.Close();
            client.Close();
        }

        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
