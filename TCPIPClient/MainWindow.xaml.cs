using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TCPIPClient
{
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _networkStream;
        private string _userName;
        private int _timeLimit;
        private int _timeRemaining;
        private string sessionId;
        private bool _isConnected = false;
        private DispatcherTimer _gameTimer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text.Trim();
            string portText = PortTextBox.Text.Trim();
            _userName = NameTextBox.Text.Trim();
            string timeLimitText = TimeLimitTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(portText) || string.IsNullOrWhiteSpace(_userName) || string.IsNullOrWhiteSpace(timeLimitText))
            {
                MessageBox.Show("Please fill in all fields before connecting.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || !int.TryParse(timeLimitText, out _timeLimit) || _timeLimit <= 0)
            {
                MessageBox.Show("Invalid port or time limit. Please enter valid numeric values.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Start connection
                _client = new TcpClient(ipAddress, port);
                _networkStream = _client.GetStream();
                _isConnected = true;

                StatusTextBlock.Text = $"Status: Connected to {ipAddress}:{port}";
                MessageBox.Show("Connected to server successfully!", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);

                // Start game timer
                StartGameTimer();

                // Start listening for server messages
                await Task.Run(() => ListenForServerMessages());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartGameTimer()
        {
            _timeRemaining = _timeLimit;
            UpdateTimeRemainingUI();

            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _gameTimer.Tick += (sender, args) =>
            {
                _timeRemaining--;
                UpdateTimeRemainingUI();

                if (_timeRemaining <= 0)
                {
                    _gameTimer.Stop();
                    EndGame("Time is up! The game has ended.");
                }
            };

            _gameTimer.Start();
        }

        private void UpdateTimeRemainingUI()
        {
            Dispatcher.Invoke(() =>
            {
                TimerTextBlock.Text = $"{_timeRemaining} seconds";
            });
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
                        sessionId = messageComponents.Length > 2 ? messageComponents[2] : null;

                        // Update the UI on the main thread
                        Dispatcher.Invoke(() =>
                        {
                            if (messageComponents.Length > 1)
                            {
                                TargetWordTextBlock.Text = messageComponents[0]; // Update the target word
                                ResultTextBlock.Text = messageComponents[1];     // Display the server's response
                            }
                            else
                            {
                                ResultTextBlock.Text = $"Server: {serverMessage}"; // Display any other server messages
                            }
                        });
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
                // Send the guess to the server
                string request = $"MakeGuess|{guess}|{sessionId}";
                byte[] dataToSend = Encoding.ASCII.GetBytes(request);

                // Send the data to the server
                await _networkStream.WriteAsync(dataToSend, 0, dataToSend.Length);

                byte[] buffer = new byte[256];
                int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Update the UI with the server's response
                Dispatcher.Invoke(() =>
                {
                    ResultTextBlock.Text = $"Server Response: {response}";
                });
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
            _gameTimer?.Stop();
            _networkStream?.Close();
            _client?.Close();
            _isConnected = false;
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = "Status: Disconnected";
                TimerTextBlock.Text = "N/A";
            });
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Disconnect();
        }
    }
}
