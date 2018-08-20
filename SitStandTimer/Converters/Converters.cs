using SitStandTimer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    public class ScheduleTypeToStringConverter : IValueConverter
    {
        public ScheduleTypeToStringConverter()
        {
            _convertedValues = new Dictionary<string, ScheduleType>();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ScheduleType)
            {
                return convertScheduleType((ScheduleType)value);
            }
            else if (value is IEnumerable<ScheduleType>)
            {
                IEnumerable<ScheduleType> valuesList = (IEnumerable<ScheduleType>)value;
                return valuesList.Select(type => convertScheduleType(type));
            }

            Debug.Assert(false, "Unexpected value type in coverter");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                return _convertedValues[(string)value];
            }

            return value;
        }

        private string convertScheduleType(ScheduleType value)
        {
            string retVal = "Unknown value";
            switch (value)
            {
                case ScheduleType.Indefinite:
                    retVal = "Indefinitely";
                    break;
                case ScheduleType.NumTimes:
                    retVal = "Number of rotations";
                    break;
                case ScheduleType.Scheduled:
                    retVal = "On set times and days";
                    break;
                default:
                    Debug.Assert(false, $"Unknown value: {value}");
                    retVal = "Unknown value";
                    break;
            }

            if (!_convertedValues.ContainsKey(retVal))
            {
                _convertedValues.Add(retVal, value);
            }

            return retVal;
        }

        private Dictionary<string, ScheduleType> _convertedValues;
    }
}
    