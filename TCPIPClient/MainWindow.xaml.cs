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
using System.Windows.Media;

/*
*   FILE          : MainWindow.cs
*   PROJECT       : PROG2121 - A05
*   PROGRAMMER    : Ahmed & Valentyn
*   FIRST VERSION : 11/11/2024
*   DESCRIPTION   :
*      This is the file where the logic befind the xaml file is implemented. We retrive and send data to the server there
*/

namespace TCPIPClient
{

    public partial class MainWindow : Window
    {
        static string sessionId;
        static bool connected;
        private DispatcherTimer gameTimer; // Timer for UI thread updates
        private int timeRemaining; // Tracks remaining time

            /*
       *  Method  : MainWindow()
       *  Summary : Constructor to initialize the main window.
       *  Params  : None
       *  Return  : None
       */
        public MainWindow()
        {
            InitializeComponent();
        }

                /*
         *  Method  : ConnectButton_Click()
         *  Summary : Handles connection to the server and sets up the game session.
         *  Params  : 
         *     object sender: The sender of the event (button clicked).
         *     RoutedEventArgs e: Event arguments.
         *  Return  : None
         */
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

            if (connected)
            {
                MessageBox.Show("Already connected.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
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
            TargetWordTextBlock.Text = "Target word received successfully!";
            TargetWordTextBlock1.Text = responseData;
            if (responseData.Contains("|"))
            {
                string[] responseComponents = responseData.Split('|');
                TargetWordTextBlock1.Text = "String: " + responseComponents[0] + "\nTotal number of words: " + responseComponents[1];
            }
            StatusTextBlock.Text = "Status: Connected";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            IpAddressTextBox.IsEnabled = false;
            PortTextBox.IsEnabled = false;
            NameTextBox.IsEnabled = false;
            TimeLimitTextBox.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            StartTimer(timeRemaining);

            ResultTextBlock.Text = "The game is on";
            ResultTextBlock.Foreground =    new SolidColorBrush(Colors.Green);
            // Close everything.
            stream.Close();
            client.Close();
        }

                /*
         *  Method  : SubmitGuessButton_Click()
         *  Summary : Sends the player's guess to the server and handles the response.
         *  Params  : 
         *     object sender: The sender of the event (button clicked).
         *     RoutedEventArgs e: Event arguments.
         *  Return  : None
         */
        private void SubmitGuessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                MessageBox.Show("You are not connected to a server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string guess = GuessTextBox.Text;

            if (guess == "")
            {
                MessageBox.Show("Please have a guess", "Guess Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create a TcpClient.
            // Note, for this client to work you need to have a TcpServer 
            // connected to the same address as specified by the server, port
            // combination.
            Int32 port = 0;
            Int32.TryParse(PortTextBox.Text, out port);
            String server = IpAddressTextBox.Text;
            TcpClient client = new TcpClient(server, port);

            // Translate the passed message into ASCII and store it as a Byte array.


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

            string[] responseParts = responseData.Split('|');
            int num = 0;
            if (responseParts.Length > 1)
            {
                Int32.TryParse(responseParts[1], out num);
            }
            if (num <= 0)
            {
                Console.WriteLine("No valid words left or the number of valid words is less than or equal to 0.");
                TargetWordTextBlock1.Text = "All words are guessed";
                ShowEndGameDialog();
            }

            // Close everything.
            stream.Close();
            client.Close();
        }

                /*
         *  Method  : StartTimer()
         *  Summary : Starts the countdown timer for the game.
         *  Params  : 
         *     int seconds: The number of seconds to count down.
         *  Return  : None
         */
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

                /*
         *  Method  : GameTimer_Tick()
         *  Summary : Updates the UI every second to reflect the remaining time.
         *  Params  : 
         *     object sender: The sender of the event (timer tick).
         *     EventArgs e: Event arguments.
         *  Return  : None
         */
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
                ShowEndGameDialog();
            }
        }

                /*
         *  Method  : EndGameButton_Click()
         *  Summary : Ends the current game session.
         *  Params  : 
         *     object sender: The sender of the event (button clicked).
         *     RoutedEventArgs e: Event arguments.
         *  Return  : None
         */
        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {
                if (finishGameQuestion() == false)
                {
                    return;
                }

           else
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = 0;
                Int32.TryParse(PortTextBox.Text, out port);
                String server = IpAddressTextBox.Text;
                TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                String message = "EndPlayerSession|" + sessionId;
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

                IpAddressTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                NameTextBox.IsEnabled = true;
                TimeLimitTextBox.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ResultTextBlock.Text = "The game is stopped";
                ResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                StatusTextBlock.Text = "Status: Disconnected";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                gameTimer.Stop();
                // Close everything.
                stream.Close();
                client.Close();

                connected = false;
            }    


          

        }

                /*
         *  Method  : ResetGame()
         *  Summary : Resets the game settings to prepare for a new game session.
         *  Params  : None
         *  Return  : None
         */
        private void ResetGame()
        {
            IpAddressTextBox.IsEnabled = true;
            PortTextBox.IsEnabled = true;
            NameTextBox.IsEnabled = true;
            TimeLimitTextBox.IsEnabled = true;
            ConnectButton.IsEnabled = true;
            ResultTextBlock.Text = "The game is stopped";
            ResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            StatusTextBlock.Text = "Status: Disconnected";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            gameTimer.Stop();
            // Close everything.

            connected = false;

        }

                /*
         *  Method  : ShowEndGameDialog()
         *  Summary : Displays a dialog box to the user at the end of the game, asking if they want to play again.
         *  Params  : None
         *  Return  : None
         */
        private void ShowEndGameDialog()
        {
            // Show a dialog box when the game ends
            MessageBoxResult result = MessageBox.Show("Congratulations! You've finished the game. Would you like to play again?", "Game Over", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                // Reset the game for a new round
                EndGameButton_Click(this, new RoutedEventArgs());
                ResetGame();
            }
            else
            {
                // Stop the game and disconnect
                EndGameButton_Click(this, new RoutedEventArgs());
            }
        }
                /*
         *  Method  : finishGameQuestion()
         *  Summary : Asks the user for confirmation to exit the game session.
         *  Params  : None
         *  Return  : bool: Returns true if the user confirms to exit, false if they cancel.
         */
        private bool finishGameQuestion()
        {
            if (!connected)
            {
                MessageBox.Show("You are not connected to a server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            MessageBoxResult mbResult = MessageBox.Show("Are you sure you want to exit.", "Connection Error", MessageBoxButton.YesNo);
            if (mbResult == MessageBoxResult.No)
            {
                return false;
            }
            return true;
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
