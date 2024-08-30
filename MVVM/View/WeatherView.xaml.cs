using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace Monitoring.MVVM.View
{
    /// <summary>
    /// Logique d'interaction pour WeatherView.xaml
    /// </summary>
    public partial class WeatherView : UserControl
    {
        public string baseApi = "https://api.openweathermap.org/data/2.5/weather?q=";
        public string city = "Paris";
        public string key;
        public string jsonString = string.Empty;

        public WeatherView()
        {
            string? apiKey = Environment.GetEnvironmentVariable("apiWeatherKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("apiWeatherKey is not set.");
            }
            else
            {
                key = apiKey;
            }
            InitializeComponent();
            //SetHeaderImg();
            SetDay();
            LoadWeatherDataAsync();
        }

        private async void LoadWeatherDataAsync()
        {
            await GetApiResponseAsync();
            SetUiInfos();
        }

        public async Task GetApiResponseAsync()
        {
            using HttpClient hc = new HttpClient();
            try
            {
                jsonString = await hc.GetStringAsync(baseApi + city + ",&APPID=" + key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching data: {ex.Message}");
            }

            //Encodage UTF8 de la réponse
            byte[] bytes = Encoding.Default.GetBytes(jsonString);
            jsonString = Encoding.UTF8.GetString(bytes);
         }


        public void SetUiInfos()
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                JObject o = JObject.Parse(jsonString);
                // Utilisez Dispatcher pour mettre à jour l'UI sur le thread principal
                Dispatcher.Invoke(() =>
                {
                    weatherDesc.Content = o["weather"]?[0]?["description"]?.ToString();

                    double tempInKelvin = o["main"]?["temp"]?.ToObject<double>() ?? 0;
                    int tempInCelsius = (int)(tempInKelvin - 273.15);
                    temperature.Content = $"{tempInCelsius} °C";

                    double apiWindSpeed = o["wind"]?["speed"]?.ToObject<double>() ?? 0;
                    int windSpeedRounded = (int)Math.Round(apiWindSpeed);
                    windSpeed.Content = $"{windSpeedRounded} km/h";

                    //vérifie la météo pour adpater l'image de fond
                    CheckWeatherDesc(o);

                });
            }
            else
            {
                MessageBox.Show("No data available");
            }

        }

        public void CheckWeatherDesc(JObject o)
        {
            if (o["weather"]?[0]?["description"]?.ToString().Contains("cloud") == true)
            {
                SetHeaderImg("cloud.jpg");
            }
            if (o["weather"]?[0]?["description"]?.ToString().Contains("rain") == true)
            {
                SetHeaderImg("rain.jpg");
            }
            if (o["weather"]?[0]?["description"]?.ToString().Contains("clear")== true)
            {
                SetHeaderImg("sun.jpg");
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)SearchBox.Template.FindName("InnerSearchBox", SearchBox);
            if (textBox != null)
            {
                city = textBox.Text; // Mettre à jour la variable de classe city
                cityName.Content = city;
                await GetApiResponseAsync();
                SetUiInfos(); // Mettre à jour l'UI après avoir récupéré les nouvelles données
            }
        }

        //La Date
        public void SetDay()
        {
            string dateFormated = DateTime.Now.ToString("dddd dd MMMM yyyy");
            dateFormated = char.ToUpper(dateFormated[0]) + dateFormated.Substring(1);
            dateTxt.Content = dateFormated;
        }

        private void SetHeaderImg(string imageName)
        {
            try
            {
                string imagePath = $"pack://application:,,,/Img/{imageName}";
                headerImg.Source = new BitmapImage(new Uri(imagePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}");
            }
        }
    }
}
