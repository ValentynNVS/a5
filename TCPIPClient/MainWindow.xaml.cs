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
using System.Timers;

namespace TCPIPClient
{
    public partial class MainWindow : Window
    {
        static string sessionId;
        private DispatcherTimer gameTimer; // Timer for UI thread updates
        private int timeRemaining; // Tracks remaining time

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
            if (!Int32.TryParse(PortTextBox.Text, out Int32 port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535).", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            String server = IpAddressTextBox.Text;
            String name = NameTextBox.Text;
            Int32 timeLimit;
            Int32.TryParse(TimeLimitTextBox.Text, out timeLimit);

            if (server == "" || port == 0 || name == "" || timeLimit == 0)
            {
                MessageBox.Show("Please fill in info first.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClient client = new TcpClient();
            try
            {
                client = new TcpClient(server, port);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (client.Connected)
                {
                    client.Close();
                }
            }

            if (!client.Connected)
            {
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

            if (!Int32.TryParse(TimeLimitTextBox.Text, out timeRemaining) || timeRemaining <= 0)
            {
                MessageBox.Show("Please enter a valid time limit in seconds.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            StartTimer(timeRemaining);
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
            if (!Int32.TryParse(PortTextBox.Text, out Int32 port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535).", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
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


        private void StartTimer(int seconds)
        {
            timeRemaining = seconds;
            TimerTextBlock.Text = $"{timeRemaining} seconds remaining";

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (timeRemaining > 0)
            {
                timeRemaining--;
                TimerTextBlock.Text = $"{timeRemaining} seconds remaining";
            }
            else
            {
                gameTimer.Stop();
                TimerTextBlock.Text = "Time's up!";
                EndGameButton_Click(this, new RoutedEventArgs());
            }
        }

        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TimeLimitTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void IpAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
