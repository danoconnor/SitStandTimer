using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace SitStandTimer.Converters
{
    public class SymbolToGlyphStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            object convertedValue = value;
            if (value is Symbol)
            {
                Symbol sym = (Symbol)value;
                
                // I'm having trouble correctly converting the symbol's int value to a unicode string.
                // For now just use a switch statement since we only have three symbols we want to convert at the moment
                switch (sym)
                {
                    case Symbol.Play:
                        convertedValue = "\uE102";
                        break;
                    case Symbol.Pause:
                        convertedValue = "\uE103";
                        break;
                    case Symbol.Next:
                        convertedValue = "\uE101";
                        break;
                }
            }

            return convertedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility retVal = Visibility.Collapsed;
            if (value is bool)
            {
                retVal = (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }

            return retVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
    