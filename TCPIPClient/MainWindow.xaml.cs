using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace TCPIPClient
{
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _networkStream;
        private string _userName;
        private int _timeLimit;
        private string sessionId;
        private bool _isConnected = false;
        private DispatcherTimer _timer; // Timer for countdown
        private int _timeRemaining;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Update every second
            _timer.Tick += Timer_Tick; // Event handler to update timer
        }

       private async void ConnectButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Read and trim input fields
        string ipAddress = IpAddressTextBox.Text.Trim();
        string portText = PortTextBox.Text.Trim();
        _userName = NameTextBox.Text.Trim();
        string timeLimitText = TimeLimitTextBox.Text.Trim();

        // Validate inputs using string.IsNullOrWhiteSpace
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(portText) || string.IsNullOrWhiteSpace(_userName) || string.IsNullOrWhiteSpace(timeLimitText))
        {
            MessageBox.Show("Please fill in all fields before connecting.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate numeric inputs using TryParse
        if (!int.TryParse(portText, out int port) || !int.TryParse(timeLimitText, out _timeLimit) || port <= 0 || _timeLimit <= 0)
        {
            MessageBox.Show("Invalid port or time limit. Please enter valid positive numeric values.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Create a TcpClient
        _client = new TcpClient(ipAddress, port);

        // Get a network stream for reading and writing
        _networkStream = _client.GetStream();

        // Translate the message into ASCII and store it as a byte array
        Byte[] data = System.Text.Encoding.ASCII.GetBytes("CreatePlayerSession");

        // Send the message to the connected server
        _networkStream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: CreatePlayerSession");

        // Buffer to store the response bytes
        data = new Byte[256];

        // String to store the response ASCII representation
        string responseData = string.Empty;

        // Read the response bytes
        Int32 bytes = await _networkStream.ReadAsync(data, 0, data.Length);
        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Received: {0}", responseData);
        TargetWordTextBlock.Text = $"Target Word: {responseData}";

                // Process session ID
         sessionId = responseData;
        _isConnected = true;

        // Update UI
        StatusTextBlock.Text = $"Status: Connected to {ipAddress}:{port}";
        MessageBox.Show("Connected to server successfully!", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);

        // Start the timer with the time limit
        _timeRemaining = _timeLimit;
        _timer.Start();

        // Listen for server messages
        await Task.Run(() => ListenForServerMessages());
    }
    catch (ArgumentNullException ex)
    {
        Console.WriteLine("ArgumentNullException: {0}", ex);
    }
    catch (SocketException ex)
    {
        Console.WriteLine("SocketException: {0}", ex);
        MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

    //string ipAddress = IpAddressTextBox.Text.Trim();
    //string portText = PortTextBox.Text.Trim();
    //_userName = NameTextBox.Text.Trim();
    //string timeLimitText = TimeLimitTextBox.Text.Trim();

    //if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(portText) || string.IsNullOrWhiteSpace(_userName) || string.IsNullOrWhiteSpace(timeLimitText))
    //{
    //    MessageBox.Show("Please fill in all fields before connecting.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    //    return;
    //}

    //if (!int.TryParse(portText, out int port) || !int.TryParse(timeLimitText, out _timeLimit) || _timeLimit <= 0)
    //{
    //    MessageBox.Show("Invalid port or time limit. Please enter valid numeric values.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    //    return;
    //}

    //try
    //{
    //    _client = new TcpClient(ipAddress, port);
    //    _networkStream = _client.GetStream();
    //    _isConnected = true;

    //    Byte[] data = Encoding.ASCII.GetBytes("CreatePlayerSession");
    //    _networkStream.Write(data, 0, data.Length);

    //    StatusTextBlock.Text = $"Status: Connected to {ipAddress}:{port}";
    //    MessageBox.Show("Connected to server successfully!", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);

    //    // Start the timer with the time limit
    //    _timeRemaining = _timeLimit;
    //    _timer.Start();

    //    // Listen for server messages
    //    await Task.Run(() => ListenForServerMessages());
    //}
    //catch (Exception ex)
    //{
    //    MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
    //}

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_timeRemaining > 0)
            {
                _timeRemaining--;
                TimerTextBlock.Text = $"Time Remaining: {_timeRemaining} sec";
            }
            else
            {
                _timer.Stop();
                MessageBox.Show("Time is up!", "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
                // Optionally, end the game or handle the timeout
            }
        }

        private async Task ListenForServerMessages()
        {
            byte[] buffer = new byte[1024];
            while (_isConnected)
            {
                try
                {
                    // Wait for server response asynchronously
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] messageComponents = serverMessage.Split('|');

                        if (messageComponents.Length > 2 && messageComponents[0] == "TargetWord")
                        {
                            // The server sends the target word
                            string targetWord = messageComponents[1];
                            Dispatcher.Invoke(() => TargetWordTextBlock.Text = $"Target Word: {targetWord}");
                        }

                        sessionId = messageComponents[2]; // Example for session ID extraction

                        Dispatcher.Invoke(() => ResultTextBlock.Text = $"Server: {serverMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusTextBlock.Text = "Status: Disconnected";
                    });
                    _isConnected = false;
                }
            }
        }

        private async void SubmitGuessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("You are not connected to the server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string guess = GuessTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(guess))
            {
                MessageBox.Show("Please enter a guess.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string request = "MakeGuess|" + guess + "|" + sessionId;
                byte[] dataToSend = Encoding.ASCII.GetBytes(request);

                // Send the guess asynchronously
                await _networkStream.WriteAsync(dataToSend, 0, dataToSend.Length);
                Console.WriteLine($"Sent: {guess}");

                if (!_client.Connected)
                {
                    throw new InvalidOperationException("The client is not connected to the server.");
                }
                // Buffer to store the server's response
                byte[] buffer = new byte[2048];
                int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);

                // Process the server's response
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (response.Contains("SessionNotFound"))
                {
                    ResultTextBlock.Text = $"Server Response: {response}";
                }
                else
                {
                    string[] responseComponents = response.Split('|');
                    ResultTextBlock.Text = $"Server Response: {responseComponents[0]} {responseComponents[1]}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during communication: {ex.Message}", "Communication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("You are not connected to the server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to end the game?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Disconnect();
            }
        }

        private void EndGame(string message)
        {
            Disconnect();
            MessageBox.Show(message, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Disconnect()
        {
            if (_client != null)
            {
                string closingMessage = "EndPlayerSession|" + sessionId;
                byte[] closeRequest = Encoding.ASCII.GetBytes(closingMessage);

                _networkStream.Write(closeRequest, 0, closeRequest.Length);

                byte[] buffer = new byte[256];
                int bytes = _networkStream.Read(buffer, 0, buffer.Length);
                string responseMessage = Encoding.ASCII.GetString(buffer, 0, bytes);

                _networkStream?.Close();
                _client.Close();
                _isConnected = false;
                StatusTextBlock.Text = "Status: Disconnected";
                TimerTextBlock.Text = "N/A";
            };
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Disconnect();
        }
    }
}
