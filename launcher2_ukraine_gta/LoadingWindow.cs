using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace launcher2_ukraine_gta;

public class LoadingWindow : Window
{
	private readonly Image _bgPrimary;

	private readonly Image _bgSecondary;

	private readonly Border _textBand;

	private readonly FrameworkElement _logo;

	private readonly Image _logoUncolored;

	private readonly Image _logoColored;

	private readonly RectangleGeometry _logoColoredClip;

	private readonly TextBlock _title;

	private readonly TextBlock _subtitle;

	public LoadingWindow()
	{
		Width = 500.0;
		Height = 540.0;
		ResizeMode = ResizeMode.NoResize;
		WindowStyle = WindowStyle.None;
		AllowsTransparency = true;
		Background = Brushes.Transparent;
		ShowInTaskbar = false;
		Topmost = true;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		UseLayoutRounding = true;
		SnapsToDevicePixels = true;

		Border frame = new Border
		{
			CornerRadius = new CornerRadius(36.0),
			BorderThickness = new Thickness(1.0),
			BorderBrush = new SolidColorBrush(Color.FromRgb(24, 44, 76)),
			Background = new SolidColorBrush(Color.FromRgb(2, 7, 19)),
			ClipToBounds = true
		};

		Grid root = new Grid();
		frame.Child = root;

		_bgPrimary = new Image
		{
			Source = LoadImage("pack://application:,,,/background.png"),
			Stretch = Stretch.UniformToFill,
			Opacity = 0.30,
			RenderTransform = new TranslateTransform(0.0, -8.0)
		};
		root.Children.Add(_bgPrimary);

		_bgSecondary = new Image
		{
			Source = LoadImage("pack://application:,,,/background.png"),
			Stretch = Stretch.UniformToFill,
			Opacity = 0.15,
			Effect = new BlurEffect
			{
				Radius = 2.1
			},
			RenderTransform = new TranslateTransform(0.0, 6.0)
		};
		root.Children.Add(_bgSecondary);

		Rectangle contour = new Rectangle
		{
			Fill = new SolidColorBrush(Color.FromArgb(65, 65, 109, 176)),
			OpacityMask = new VisualBrush
			{
				Visual = new Image
				{
					Source = LoadImage("pack://application:,,,/background.png"),
					Stretch = Stretch.UniformToFill,
					Opacity = 1.0
				}
			},
			Opacity = 0.18
		};
		root.Children.Add(contour);

		Grid topDarken = new Grid
		{
			Background = new LinearGradientBrush(
				Color.FromArgb(138, 2, 6, 18),
				Color.FromArgb(30, 2, 5, 16),
				new Point(0.5, 0.0),
				new Point(0.5, 0.42))
		};
		root.Children.Add(topDarken);

		Grid overlay = new Grid
		{
			Background = new LinearGradientBrush(
				Color.FromArgb(220, 4, 10, 27),
				Color.FromArgb(238, 2, 5, 14),
				new Point(0.5, 0.0),
				new Point(0.5, 1.0))
		};
		root.Children.Add(overlay);

		_textBand = new Border
		{
			Height = 68.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 140.0, 0.0, -10.0),
			Background = new LinearGradientBrush(
				Color.FromArgb(0, 36, 66, 104),
				Color.FromArgb(102, 30, 57, 92),
				new Point(0.0, 0.5),
				new Point(1.0, 0.5))
		};
		root.Children.Add(_textBand);

		StackPanel content = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, -54.0, 0.0, 0.0)
		};
		root.Children.Add(content);

		Grid logoHost = new Grid
		{
			Width = 154.0,
			Height = 154.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 50.0),
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = new TransformGroup
			{
				Children = new TransformCollection
				{
					new ScaleTransform(1.0, 1.0),
					new TranslateTransform(0.0, 0.0)
				}
			},
			Effect = new DropShadowEffect
			{
				Color = Color.FromRgb(17, 112, 216),
				BlurRadius = 18.0,
				ShadowDepth = 0.0,
				Opacity = 0.44
			}
		};
		_logoUncolored = new Image
		{
			Source = LoadImage("pack://application:,,,/UKRAINEGTA.exe.icon.png"),
			Stretch = Stretch.Uniform,
			Opacity = 0.38
		};
		_logoColoredClip = new RectangleGeometry(new Rect(0.0, 154.0, 154.0, 0.0));
		_logoColored = new Image
		{
			Source = LoadImage("pack://application:,,,/UKRAINEGTA.exe.icon.png"),
			Stretch = Stretch.Uniform,
			Opacity = 0.95,
			Clip = _logoColoredClip
		};
		logoHost.Children.Add(_logoUncolored);
		logoHost.Children.Add(_logoColored);
		_logo = logoHost;
		RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
		content.Children.Add(_logo);

		_title = new TextBlock
		{
			Text = "\u0420\u0430\u0434\u0456 \u0412\u0430\u0441 \u0437\u043d\u043e\u0432\u0443 \u0431\u0430\u0447\u0438\u0442\u0438!",
			Foreground = Brushes.White,
			FontSize = 22.0,
			FontWeight = FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0),
			Opacity = 0.95
		};
		content.Children.Add(_title);

		_subtitle = new TextBlock
		{
			Text = "\u0417\u0430\u0447\u0435\u043a\u0430\u0439\u0442\u0435 \u0431\u0443\u0434\u044c \u043b\u0430\u0441\u043a\u0430, \u0432\u0456\u0434\u0431\u0443\u0432\u0430\u0454\u0442\u044c\u0441\u044f\n\u0437\u0430\u0432\u0430\u043d\u0442\u0430\u0436\u0435\u043d\u043d\u044f \u043b\u0430\u0443\u043d\u0447\u0435\u0440\u0443...",
			Foreground = new SolidColorBrush(Color.FromRgb(116, 124, 141)),
			FontSize = 16.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			Opacity = 0.85
		};
		content.Children.Add(_subtitle);

		Content = frame;
		Loaded += (_, _) => StartAnimations();
	}

	private void StartAnimations()
	{
		Storyboard storyboard = new Storyboard();

		DoubleAnimation bgShiftA = new DoubleAnimation
		{
			From = -8.0,
			To = 6.0,
			Duration = TimeSpan.FromSeconds(10.0),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever,
			EasingFunction = new SineEase
			{
				EasingMode = EasingMode.EaseInOut
			}
		};
		Storyboard.SetTarget(bgShiftA, _bgPrimary);
		Storyboard.SetTargetProperty(bgShiftA, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
		storyboard.Children.Add(bgShiftA);

		DoubleAnimation bgShiftB = new DoubleAnimation
		{
			From = 6.0,
			To = -5.0,
			Duration = TimeSpan.FromSeconds(12.0),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever,
			EasingFunction = new SineEase
			{
				EasingMode = EasingMode.EaseInOut
			}
		};
		Storyboard.SetTarget(bgShiftB, _bgSecondary);
		Storyboard.SetTargetProperty(bgShiftB, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
		storyboard.Children.Add(bgShiftB);

		DoubleAnimation bandPulse = new DoubleAnimation
		{
			From = 0.86,
			To = 1.0,
			Duration = TimeSpan.FromSeconds(1.9),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever,
			EasingFunction = new SineEase
			{
				EasingMode = EasingMode.EaseInOut
			}
		};
		Storyboard.SetTarget(bandPulse, _textBand);
		Storyboard.SetTargetProperty(bandPulse, new PropertyPath(OpacityProperty));
		storyboard.Children.Add(bandPulse);

		if (_logo.RenderTransform is TransformGroup group && group.Children.Count >= 2)
		{
			DoubleAnimation logoScaleX = new DoubleAnimation
			{
				From = 1.0,
				To = 1.035,
				Duration = TimeSpan.FromSeconds(1.7),
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever,
				EasingFunction = new SineEase
				{
					EasingMode = EasingMode.EaseInOut
				}
			};
			Storyboard.SetTarget(logoScaleX, _logo);
			Storyboard.SetTargetProperty(logoScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
			storyboard.Children.Add(logoScaleX);

			DoubleAnimation logoScaleY = new DoubleAnimation
			{
				From = 1.0,
				To = 1.035,
				Duration = TimeSpan.FromSeconds(1.7),
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever,
				EasingFunction = new SineEase
				{
					EasingMode = EasingMode.EaseInOut
				}
			};
			Storyboard.SetTarget(logoScaleY, _logo);
			Storyboard.SetTargetProperty(logoScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
			storyboard.Children.Add(logoScaleY);

			DoubleAnimation logoFloat = new DoubleAnimation
			{
				From = 0.0,
				To = -6.0,
				Duration = TimeSpan.FromSeconds(2.5),
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever,
				EasingFunction = new SineEase
				{
					EasingMode = EasingMode.EaseInOut
				}
			};
			Storyboard.SetTarget(logoFloat, _logo);
			Storyboard.SetTargetProperty(logoFloat, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
			storyboard.Children.Add(logoFloat);
		}

		DoubleAnimation titleBlink = new DoubleAnimation
		{
			From = 0.88,
			To = 1.0,
			Duration = TimeSpan.FromSeconds(1.4),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		Storyboard.SetTarget(titleBlink, _title);
		Storyboard.SetTargetProperty(titleBlink, new PropertyPath(OpacityProperty));
		storyboard.Children.Add(titleBlink);

		DoubleAnimation subtitleBlink = new DoubleAnimation
		{
			From = 0.72,
			To = 0.9,
			Duration = TimeSpan.FromSeconds(1.4),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		Storyboard.SetTarget(subtitleBlink, _subtitle);
		Storyboard.SetTargetProperty(subtitleBlink, new PropertyPath(OpacityProperty));
		storyboard.Children.Add(subtitleBlink);

		DoubleAnimation logoUncoloredPulse = new DoubleAnimation
		{
			From = 0.34,
			To = 0.55,
			Duration = TimeSpan.FromSeconds(1.25),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		Storyboard.SetTarget(logoUncoloredPulse, _logoUncolored);
		Storyboard.SetTargetProperty(logoUncoloredPulse, new PropertyPath(OpacityProperty));
		storyboard.Children.Add(logoUncoloredPulse);

		DoubleAnimation logoColoredPulse = new DoubleAnimation
		{
			From = 0.82,
			To = 1.0,
			Duration = TimeSpan.FromSeconds(1.25),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		Storyboard.SetTarget(logoColoredPulse, _logoColored);
		Storyboard.SetTargetProperty(logoColoredPulse, new PropertyPath(OpacityProperty));
		storyboard.Children.Add(logoColoredPulse);

		RectAnimation logoProgressFill = new RectAnimation
		{
			From = new Rect(0.0, 154.0, 154.0, 0.0),
			To = new Rect(0.0, 0.0, 154.0, 154.0),
			Duration = TimeSpan.FromSeconds(1.85),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever,
			EasingFunction = new SineEase
			{
				EasingMode = EasingMode.EaseInOut
			}
		};
		Storyboard.SetTarget(logoProgressFill, _logoColored);
		Storyboard.SetTargetProperty(logoProgressFill, new PropertyPath("(UIElement.Clip).(RectangleGeometry.Rect)"));
		storyboard.Children.Add(logoProgressFill);

		storyboard.Begin(this, isControllable: false);
	}

	private static BitmapImage LoadImage(string uri)
	{
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.UriSource = new Uri(uri, UriKind.Absolute);
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		return bitmapImage;
	}
}
