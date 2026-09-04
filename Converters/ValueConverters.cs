using System.Globalization;
using NexTap.Models;

namespace NexTap.Converters;

public class InvertedBoolConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is bool b && !b;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is bool b && !b;
}

/// <summary>Maps a CardCover to its pastel accent Color for the small swatch shown in list rows.</summary>
public class CoverToColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not CardCover cover)
			return Colors.Transparent;

		var key = cover.AccentColorKey();
		return Application.Current?.Resources.TryGetValue(key, out var color) == true
			? color
			: Colors.Transparent;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

public class CoverToNameConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is CardCover cover ? cover.DisplayName() : string.Empty;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>True when the bound CardCover equals the ConverterParameter (a CardCover), for cover-picker highlighting.</summary>
public class CoverEqualsConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not CardCover current || parameter is not CardCover target)
			return false;

		return current == target;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Generic bool->string. ConverterParameter format: "TrueText|FalseText".</summary>
public class BoolToStringConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not bool b || parameter is not string param)
			return string.Empty;

		var parts = param.Split('|');
		if (parts.Length != 2)
			return string.Empty;

		return b ? parts[0] : parts[1];
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}


public class CoverToTextColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not CardCover cover)
			return Colors.White;

		return cover is CardCover.Midnight or CardCover.Crimson or CardCover.Purple
			? Colors.White
			: Colors.White;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
