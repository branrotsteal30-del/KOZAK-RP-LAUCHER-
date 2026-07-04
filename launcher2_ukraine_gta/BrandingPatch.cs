using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace launcher2_ukraine_gta;

internal static class BrandingPatch
{
	private static readonly string[] LegacyBrands = new string[4] { "LEGION GTA", "LEGION MTA", "LEGION", "UKRAINE GTA" };

	private const string CurrentBrand = "KOZAK RP";

	internal static void Apply(Window window, string assetDirectory = null)
	{
		if (window == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(window.Title))
		{
			window.Title = ReplaceLegacyBrand(window.Title);
		}
		HashSet<DependencyObject> visited = new HashSet<DependencyObject>();
		ApplyRecursive(window, visited, assetDirectory);
	}

	private static void ApplyRecursive(DependencyObject node, HashSet<DependencyObject> visited, string assetDirectory)
	{
		if (node == null || !visited.Add(node))
		{
			return;
		}
		if (node is TextBlock textBlock)
		{
			if (!string.IsNullOrEmpty(textBlock.Text))
			{
				textBlock.Text = ReplaceLegacyBrand(textBlock.Text);
			}
			foreach (Inline inline in textBlock.Inlines)
			{
				if (inline is Run run && !string.IsNullOrEmpty(run.Text))
				{
					run.Text = ReplaceLegacyBrand(run.Text);
				}
			}
		}
		else if (node is Run run2 && !string.IsNullOrEmpty(run2.Text))
		{
			run2.Text = ReplaceLegacyBrand(run2.Text);
		}
		else if (node is AccessText accessText && !string.IsNullOrEmpty(accessText.Text))
		{
			accessText.Text = ReplaceLegacyBrand(accessText.Text);
		}
		else if (node is ContentControl contentControl && contentControl.Content is string text)
		{
			contentControl.Content = ReplaceLegacyBrand(text);
		}
		else if (node is HeaderedContentControl headeredContentControl && headeredContentControl.Header is string headerText)
		{
			headeredContentControl.Header = ReplaceLegacyBrand(headerText);
		}
		foreach (DependencyObject child in GetChildren(node))
		{
			ApplyRecursive(child, visited, assetDirectory);
		}
	}

	private static void ApplyImageOverride(Image image, string assetDirectory)
	{
		if (image.Source == null)
		{
			return;
		}
		string sourceText = image.Source.ToString();
		if (string.IsNullOrEmpty(sourceText))
		{
			return;
		}
		string overrideFileName = null;
		if (sourceText.IndexOf("server_mta.png", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			overrideFileName = "server_mta.png";
		}
		else if (sourceText.IndexOf("header_logo.png", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			overrideFileName = "header_logo.png";
		}
		if (overrideFileName == null)
		{
			return;
		}
		string filePath = ResolveImagePath(assetDirectory, overrideFileName);
		if (string.IsNullOrEmpty(filePath))
		{
			return;
		}
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			image.Source = bitmapImage;
		}
		catch
		{
		}
	}

	private static string ResolveImagePath(string assetDirectory, string fileName)
	{
		string baseDir = string.IsNullOrWhiteSpace(assetDirectory) ? AppDomain.CurrentDomain.BaseDirectory : assetDirectory;
		string[] candidates = new string[3]
		{
			Path.Combine(baseDir, fileName),
			Path.Combine(baseDir, "..", "..", "..", fileName),
			Path.Combine(baseDir, "..", "..", fileName)
		};
		foreach (string candidate in candidates)
		{
			try
			{
				string fullPath = Path.GetFullPath(candidate);
				if (File.Exists(fullPath))
				{
					return fullPath;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static IEnumerable<DependencyObject> GetChildren(DependencyObject node)
	{
		HashSet<DependencyObject> yielded = new HashSet<DependencyObject>();
		int childrenCount = 0;
		if (node is Visual || node is Visual3D)
		{
			try
			{
				childrenCount = VisualTreeHelper.GetChildrenCount(node);
			}
			catch
			{
				childrenCount = 0;
			}
		}
		for (int i = 0; i < childrenCount; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(node, i);
			if (child != null && yielded.Add(child))
			{
				yield return child;
			}
		}
		foreach (object child2 in LogicalTreeHelper.GetChildren(node))
		{
			if (child2 is DependencyObject item && yielded.Add(item))
			{
				yield return item;
			}
		}
	}

	private static string ReplaceLegacyBrand(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2 = text;
		foreach (string legacyBrand in LegacyBrands)
		{
			text2 = Regex.Replace(text2, Regex.Escape(legacyBrand), CurrentBrand, RegexOptions.IgnoreCase);
		}
		return text2;
	}
}
