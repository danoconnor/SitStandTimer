using Windows.UI.Xaml.Controls;

namespace SitStandTimer
{
    public class PageBase : Page
    {
        public PageBase()
        {
            // Default to Inline. Pages that want different behavior can override this
            HamburgerMenuDisplayMode = SplitViewDisplayMode.CompactInline;
            Loaded += onPageLoaded;
        }

        protected SplitViewDisplayMode HamburgerMenuDisplayMode { get; set; }

        private void onPageLoaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AppFrame.Instance.SetSplitViewDisplayMode(HamburgerMenuDisplayMode);
        }
    }
}
