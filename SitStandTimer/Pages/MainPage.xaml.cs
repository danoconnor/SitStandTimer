using SitStandTimer.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SitStandTimer
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            DataContext = ViewModel = new MainPageVM();
        }

        public MainPageVM ViewModel { get; private set; }

        private void NavToConfigurationPage(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ConfigurationPage), null);
        }
    }
}
