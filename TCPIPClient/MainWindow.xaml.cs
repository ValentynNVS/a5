using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TCPIPClient
{
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _networkStream;
        private string _userName;
        private int _timeLimit;
        private bool _isConnected = false;

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
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);
                _networkStream = _client.GetStream();
                _isConnected = true;

                StatusTextBlock.Text = $"Status: Connected to {ipAddress}:{port}";
                MessageBox.Show("Connected to server successfully!", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);

                await Task.Run(() => ListenForServerMessages());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ListenForServerMessages()
        {
            byte[] buffer = new byte[1024];
            while (_isConnected)
            {
                try
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
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
                MessageBox.Show("You are not connected to a server.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string guess = GuessTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(guess))
            {
                MessageBox.Show("Please enter a word guess.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] guessBytes = Encoding.UTF8.GetBytes(guess);
            try
            {
                await _networkStream.WriteAsync(guessBytes, 0, guessBytes.Length);
                ResultTextBlock.Text = $"You guessed: {guess}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send guess: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to end the game?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            if (_client != null)
            {
                _networkStream?.Close();
                _client.Close();
                _isConnected = false;
                StatusTextBlock.Text = "Status: Disconnected";
                MessageBox.Show("Disconnected from server.", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Disconnect();
        }
    }
}
