using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Net.Http;
using Newtonsoft.Json;

namespace LC_Portfolio
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDataAsync().ConfigureAwait(false); // Ensures not to block the UI thread.
            SetGreetingMessage();
        }

        private async Task InitializeDataAsync()
        {
            await FetchIpInfoAsync();
            // Note: FetchWeatherDataAsync is now correctly invoked after FetchIpInfoAsync
        }

        private async Task FetchWeatherDataAsync(string location)
        {
            try
            {
                // Directly using the API key from environment variables
                string weatherApiKey = ApiKeyManager.GetApiKey("Weather_API");
                if (!string.IsNullOrEmpty(weatherApiKey))
                {
                    string weatherInfo = await GetWeatherAsync(location, weatherApiKey);
                    Dispatcher.Invoke(() => WeatherInfoTextBlock.Text = weatherInfo);
                }
                else
                {
                    throw new InvalidOperationException("Weather API key not found.");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => WeatherInfoTextBlock.Text = $"Failed to fetch weather data: {ex.Message}");
            }
        }

        private async Task<string> FetchIpInfoAsync()
        {
            string ipToken = ApiKeyManager.GetApiKey("IP_API");
            if (!string.IsNullOrEmpty(ipToken))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string ipInfoUrl = $"https://ipinfo.io?token={ipToken}";
                        var response = await client.GetAsync(ipInfoUrl);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic ?ipInfo = JsonConvert.DeserializeObject(responseBody);

                        string location = $"{ipInfo.city}, {ipInfo.country}";
                        Dispatcher.Invoke(() => IpInfoTextBlock.Text = $"IP: {ipInfo.ip}, Location: {location}");

                        // Proceed to fetch weather data now that we have the location
                        await FetchWeatherDataAsync(location);
                        return location;
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => IpInfoTextBlock.Text = $"Failed to fetch IP info: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Dispatcher.Invoke(() => IpInfoTextBlock.Text = "IP token not found.");
                return null;
            }
        }

        private async Task<string> GetWeatherAsync(string location, string apiKey)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://api.openweathermap.org/data/2.5/weather?q={location}&appid={apiKey}&units=metric";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic ?weatherData = JsonConvert.DeserializeObject(responseBody);

                return $"Weather in: {location}, {weatherData.weather[0].main}, {weatherData.main.temp}°C";
            }
        }

        private void MainFrame_ContentRendered(object sender, EventArgs e)
        {
            // Your code here to execute after the Frame's content is rendered
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the window
            this.Close();
        }

        private void Menu1_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Menu1Page());
        }

        private void Menu2_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Menu2Page()); // Assume you have a Menu2Page defined
        }

        //Add more click event handlers for other buttons...
        private void SetGreetingMessage()
        {
            string username = Environment.UserName;
            string greeting = GetGreetingBasedOnTimeOfDay();
            GreetingTextBlock.Text = $"{greeting} \n {username}";
        }

        private string GetGreetingBasedOnTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            if (hour < 12)
            {
                return "Good Morning";
            }
            else if (hour < 18)
            {
                return "Good Afternoon";
            }
            else
            {
                return "Good Evening";
            }
        }

    }
}