using SitStandTimer.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SitStandTimer
{
    public sealed partial class MainPage : PageBase
    {
        public MainPage()
        {
            this.InitializeComponent();
            HamburgerMenuDisplayMode = SplitViewDisplayMode.CompactOverlay;

            DataContext = ViewModel = new MainPageVM();
        }

        public MainPageVM ViewModel { get; private set; }

        private void NavToConfigurationPage(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ConfigurationPage), null);
        }
    }
}
