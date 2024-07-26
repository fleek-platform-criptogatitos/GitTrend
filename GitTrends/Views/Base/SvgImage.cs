﻿using System.ComponentModel;
using GitTrends.Mobile.Common;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Behaviors;

namespace GitTrends;

public class SvgImage : Image
{
	public static readonly BindableProperty SvgColorProperty =
		BindableProperty.Create(nameof(SvgColor), typeof(Color), typeof(SvgImage), Colors.White, propertyChanged: HandleGetTextColorPropertyChanged);

	const int _defaultHeight = 18;
	const int _defaultWidth = 18;

	readonly IDeviceInfo _deviceInfo;

	public SvgImage(in IDeviceInfo deviceInfo, in string svgFileName, in Color svgColor, double widthRequest = _defaultWidth, double heightRequest = _defaultHeight)
		: this(deviceInfo, svgColor, widthRequest, heightRequest)
	{
		if (!Path.GetExtension(svgFileName).EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException($"{nameof(svgFileName)} must end with `.svg`");

		Source = svgFileName;
	}

	public SvgImage(IDeviceInfo deviceInfo, Color color, double widthRequest = _defaultWidth, double heightRequest = _defaultHeight)
		: this(deviceInfo, widthRequest, heightRequest)
	{
		SvgColor = color;
	}

	public SvgImage(IDeviceInfo deviceInfo, double widthRequest = _defaultWidth, double heightRequest = _defaultHeight)
	{
		_deviceInfo = deviceInfo;

		PropertyChanged += HandlePropertyChanged;
		ThemeService.PreferenceChanged += HandlePreferenceChanged;

		this.Fill();

		WidthRequest = widthRequest;
		HeightRequest = heightRequest;

		Behaviors.Add(new IconTintColorBehavior()
			.Bind(IconTintColorBehavior.TintColorProperty,
				getter: static image => image.SvgColor,
				source: this));
	}

	public Color SvgColor
	{
		get => (Color)GetValue(SvgColorProperty);
		set
		{
			if (!Equals(value, SvgColor))
			{
				SetValue(SvgColorProperty, value);
				OnPropertyChanged();
			}
		}
	}

	static void HandleGetTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var svgImage = (SvgImage)bindable;
		svgImage.SvgColor = (Color)newValue;
	}

	void HandlePreferenceChanged(object? sender, PreferredTheme e)
	{
		throw new NotImplementedException();
	}

	void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == SourceProperty.PropertyName)
		{
			if (_deviceInfo.Platform == DevicePlatform.iOS && Source is IFileImageSource fileImageSource)
				Source = Path.ChangeExtension(fileImageSource.File, ".png");
		}
	}
}