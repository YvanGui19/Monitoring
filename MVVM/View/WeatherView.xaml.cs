using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Monitoring.MVVM.View
{
    /// <summary>
    /// Logique d'interaction pour WeatherView.xaml
    /// </summary>
    public partial class WeatherView : UserControl
    {
        public string city = "";
        public WeatherView()
        {
            InitializeComponent();
            SetHeaderImg();
            SetDay();
        }

        //La Date
        public void SetDay()
        {
            string dateFormated = DateTime.Now.ToString("dddd dd MMMM yyyy");
            dateFormated = char.ToUpper(dateFormated[0]) + dateFormated.Substring(1);
            dateTxt.Content = dateFormated;
        }

        public void SetHeaderImg()
        {
            string meteo = "pluie";

            if (meteo == "pluie")
            {
                //Modification de l'image du header
                headerImg.Source = new BitmapImage(new Uri(@"/Img/night.jpg", UriKind.Relative));
            }
            else
            {

            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)SearchBox.Template.FindName("InnerSearchBox", SearchBox);
            if (textBox != null)
            {
                string city = textBox.Text;
                cityName.Content = city;
            }
        }
    }
}
