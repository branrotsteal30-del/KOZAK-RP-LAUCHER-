using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace launcher2_ukraine_gta;

public class MainWindow : Window, IComponentConnector
{
	private class News
	{
		public string ImageUrl { get; set; }

		public string Date { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string Link { get; set; }
	}

	private class GameServer
	{
		public string id { get; set; }

		public string name { get; set; }

		public string players { get; set; }

		public string max_players { get; set; }

		public string address { get; set; }

		public string closed_text { get; set; }

		public System.Windows.Controls.Button element { get; set; }

		public Uri image_url { get; set; } = null;
	}

	private class GameFiles
	{
		public string path { get; set; }

		public string hash { get; set; }

		public long size { get; set; }

		public string can_skip { get; set; }
	}

	private class RuntimeFile
	{
		public string path { get; set; }

		public string file { get; set; }

		public string hash { get; set; }
	}

	public class SettingsManager
	{
		private string FilePath = "settings.json";

		private JObject _launcherSettings;

		public dynamic LauncherSettings => _launcherSettings;

		public SettingsManager(string path)
		{
			FilePath = path;
			LoadSettings();
		}

		private void LoadSettings()
		{
			try
			{
				if (File.Exists(FilePath))
				{
					string text = File.ReadAllText(FilePath);
					_launcherSettings = JObject.Parse(text);
				}
				else
				{
					SetDefaultSettings();
					SaveSettings();
				}
			}
			catch (Exception)
			{
				SetDefaultSettings();
			}
		}

		private void SetDefaultSettings()
		{
			_launcherSettings = new JObject
			{
				["favoriteServerMTA"] = "s6",
				["favoriteServerGTA5"] = "s1",
				["autostart"] = true,
				["autologin"] = true,
				["graphic"] = "high",
				["mta_path"] = "",
				["gta5_path"] = ""
			};
		}

		public void SaveSettings()
		{
			try
			{
				File.WriteAllText(FilePath, ((object)_launcherSettings).ToString());
			}
			catch
			{
			}
		}
	}

	private class ProjectData
	{
		public string instagram_url { get; set; }

		public string telegram_url { get; set; }

		public string discord_url { get; set; }

		public string version { get; set; }

		public long archive_size { get; set; }

		public string archive_path { get; set; }

		public bool is_started { get; set; }

		public DateTime last_exit_proccess { get; set; }

		public DateTime last_start_proccess { get; set; }

		public string client_version { get; set; }
	}

	private double targetOffset = 0.0;

	private DispatcherTimer scrollTimer;

	private bool isRenderedMtaNews = false;

	private bool isRenderedGta5News = false;

	private int currentNewsIndexMta = 0;

	private int currentNewsIndexGta5 = 0;

	private NotifyIcon trayIcon;

	private FolderBrowserDialog folderBrowserDialog;

	private ProjectData MTAProject = new ProjectData
	{
		instagram_url = "https://discord.gg/cv43CBUsn8",
		telegram_url = "https://discord.gg/cv43CBUsn8",
		discord_url = "https://discord.gg/cv43CBUsn8",
		version = "0",
		archive_size = 4718592000L,
		is_started = false,
		client_version = "1.6"
	};

	private ProjectData GTA5Project = new ProjectData
	{
		instagram_url = "https://discord.gg/cv43CBUsn8",
		telegram_url = "https://discord.gg/cv43CBUsn8",
		discord_url = "https://discord.gg/cv43CBUsn8",
		version = "0",
		archive_size = 183500800L,
		archive_path = "http://cdn.ukraine-gta5.online/RAGEMP.7z",
		is_started = false
	};

	private const string AppMutexName = "UKRAINEGTA_LAUNCHER";

	private Mutex mutex_launcher;

	private string _FILE_SETTINGS = "\\settings.json";

	public string _FILE_GAME_ARCHIVE = "\\downloaded\\game.zip";

	public string _FILE_RAGE_MP_ARCHIVE = "\\downloaded\\RAGEMP.7z";

	public string _LOCATION_MTA_GAME = "\\game";

	public string _LOCATION_RAGE_MP = "\\RAGEMP";

	private string _LOCATION_DOWNLOAD = "\\downloaded";

	private string _GAME_URL_DOWNLOAD = string.Empty;

	private const string _OFFLINE_MTA_ARCHIVE_URL = "https://kozak-rp.cdn.express/~/share/09d03b7b810f/game.zip";

	private const string _REMOTE_REINSTALL_CHECK_URL = "https://kozak-rp.cdn.express/~/share/0c4c3ab0a13c/check.txt";

	private const string _REMOTE_REINSTALL_CHECK_FILE = "mta_client_check.txt";

	private readonly RuntimeFile[] _SEVEN_ZIP_RUNTIME_FILES = new RuntimeFile[3]
	{
		new RuntimeFile
		{
			path = "https://cdn.ukraine-gta.online/launcher/files/7z/7za.dll",
			file = "7z/7za.dll",
			hash = "060dd6a25aaf6a9176258495817f37d1"
		},
		new RuntimeFile
		{
			path = "https://cdn.ukraine-gta.online/launcher/files/7z/7za.exe",
			file = "7z/7za.exe",
			hash = "c8f5f4ff809a5fa42de1ad2ea1f0ba6c"
		},
		new RuntimeFile
		{
			path = "https://cdn.ukraine-gta.online/launcher/files/7z/7zxa.dll",
			file = "7z/7zxa.dll",
			hash = "368af651286d82b91924eac5c9f2d053"
		}
	};

	private List<GameFiles> _FILES_TO_DOWNLOAD = new List<GameFiles>();

	private string _GAME_FILES_DOWNLOAD = string.Empty;

	private string _FILES_PATH_AND_HASHES = string.Empty;

	private bool _FORCE_DOWNLOAD = false;

	private bool _REINSTALL_MTA = false;

	private bool _AUTO_REINSTALL_AFTER_CHECK = false;

	public string _CLIENT_PATH_LAUNCHER = string.Empty;

	private string _CLIENT_ID = string.Empty;

	private string _CLIENT_OS = string.Empty;

	public bool _INIT_DOWNLOAD = false;

	public bool _DOWNLOAD_TYPE = false;

	public bool _INIT_UNPACKER = false;

	private string _FAVORITE_MTA_SERVER = "s6";

	private string _FAVORITE_GTA5_SERVER = "s1";

	private bool _CLIENT_ADMIN = false;

	private WebClient _WEB_CLIENT = new WebClient();

	private string _WEB_CLIENT_USERAGENT = "UKRAINE-GTA Launcher client";

	private Stopwatch _STOP_WATCH = new Stopwatch();

	private const string _PREFIX_JSON = "a3!12^c6}566cce4aРЎРѓР вЂ”a+0vl?sf1deb9b_";

	private DispatcherTimer newsMtaTimer = new DispatcherTimer();

	private DispatcherTimer newsGta5Timer = new DispatcherTimer();

	private SettingsManager _SETTINGS;

	private DispatcherTimer SocketWatchDog = new DispatcherTimer();

	private DispatcherTimer _OnlineFetcherTimer = new DispatcherTimer();

	private DispatcherTimer _MonitoringTimer = new DispatcherTimer();

	private System.Net.Sockets.TcpClient _MonitoringClient;

	private long size_to_download = 0L;

	private long last_bytes_received = 0L;

	private int count_error_in_download = 0;

	internal Border borderElement;

	internal System.Windows.Controls.Image HeaderImage;

	internal System.Windows.Controls.Button CabinetButton;

	internal System.Windows.Controls.Image InstagramButton;

	internal System.Windows.Controls.Image TelegramButton;

	internal System.Windows.Controls.Image DiscordButton;

	internal System.Windows.Controls.Button SettingsButton;

	internal Grid LeftMenu;

	internal Border LeftMenuBorder;

	internal System.Windows.Controls.Button DonateButton;

	internal System.Windows.Controls.Button SiteButton;

	internal System.Windows.Controls.Button ForumButton;

	internal TextBlock TotalProjectsOnline;

	internal System.Windows.Controls.Button ProjectMTAButton;

	internal Ellipse projectEllipseBackgroundMta;

	internal TextBlock ProjectMTATotalOnline;

	internal TextBlock GameVersion;

	internal StackPanel AdminMode;

	internal Grid MTABlock;

	internal Grid NewsLinkUrl;

	internal System.Windows.Controls.Image NewsImage;

	internal TextBlock NewsDate;

	internal TextBlock NewsTitle;

	internal TextBlock NewsDescription;

	internal TextBlock TotalOnline;

	internal Grid MtaX2Block;

	internal Grid GridAllReady;

	internal TextBlock AllReadyStatus;

	internal TextBlock AllReadyAdditional;

	internal Grid ButtonPlay;

	internal System.Windows.Controls.Button StartButton;

	internal Grid GridNeedDownload;

	internal TextBlock NeedDownloadSize;

	internal Grid GridDownload;

	internal TextBlock DownloadStatus;

	internal TextBlock DownloadProgressValue;

	internal System.Windows.Controls.ProgressBar DownloadProgressBar;

	internal Grid ButtonStartDownload;

	internal System.Windows.Controls.Button StartDownloadButton;

	internal Grid GridCheckFiles;

	internal TextBlock CheckFilesProgressValue;

	internal System.Windows.Controls.ProgressBar CheckFilesProgressBar;

	internal Grid Gta5Block;

	internal Grid NewsLinkUrlGta5;

	internal System.Windows.Controls.Image NewsImageGta5;

	internal TextBlock NewsDateGta5;

	internal TextBlock NewsTitleGta5;

	internal TextBlock NewsDescriptionGta5;

	internal TextBlock TotalOnlineGta5;

	internal Grid Gta5X2Block;

	internal Grid GridNeedDownloadGTA5;

	internal TextBlock NeedDownloadGTA5Size;

	internal Grid GridAllReadyGTA5;

	internal TextBlock AllReadyStatusGTA5;

	internal TextBlock AllReadyAdditionalGTA5;

	internal Grid ButtonPlayGTA5;

	internal System.Windows.Controls.Button StartButtonGta5;

	internal Grid GridDownloadGTA5;

	internal TextBlock DownloadGTA5Status;

	internal TextBlock DownloadGTA5ProgressValue;

	internal System.Windows.Controls.ProgressBar DownloadGTA5ProgressBar;

	internal Grid ButtonStartDownloadGTA5;

	internal System.Windows.Controls.Button StartDownloadButtonGTA5;

	internal Grid GridnnNeedBuyGTA5;

	internal Grid ButtonBuyGTA5;

	internal Grid DonateMTABlock;

	internal System.Windows.Controls.Image DonateOffer;

	internal System.Windows.Controls.Button DonateOfferLink;

	internal ScrollViewer DonateScroll;

	internal Grid DonateContent;

	internal StackPanel PremiumButtonsMTA;

	internal TextBlock PremiumCost;

	internal StackPanel Vehicle1;

	internal StackPanel Vehicle1Button;

	internal StackPanel Vehicle2;

	internal StackPanel Vehicle2Button;

	internal StackPanel Vehicle3;

	internal StackPanel Vehicle3Button;

	internal StackPanel Case1;

	internal StackPanel Case1Button;

	internal StackPanel Case2;

	internal StackPanel Case2Button;

	internal StackPanel Case3;

	internal StackPanel Case3Button;

	internal Grid DonateGTA5Block;

	internal System.Windows.Controls.Image DonateGTA5Offer;

	internal System.Windows.Controls.Button DonateGTA5OfferLink;

	internal ScrollViewer DonateGTA5Scroll;

	internal Grid DonateGTA5Content;

	internal StackPanel PremiumGTA5Buttons;

	internal TextBlock PremiumGTA5Cost;

	internal StackPanel Vehicle1GTA5;

	internal StackPanel Vehicle1GTA5Button;

	internal StackPanel Vehicle2GTA5;

	internal StackPanel Vehicle2GTA5Button;

	internal StackPanel Vehicle3GTA5;

	internal StackPanel Vehicle3GTA5Button;

	internal StackPanel Case1GTA5;

	internal StackPanel Case1GTA5Button;

	internal StackPanel Case2GTA5;

	internal StackPanel Case2GTA5Button;

	internal StackPanel Case3GTA5;

	internal StackPanel Case3GTA5Button;

	internal Grid SettingsMTABlock;

	internal TextBlock GameFolderPath;

	internal System.Windows.Controls.CheckBox AutoStart;

	internal System.Windows.Controls.CheckBox AutoLogin;

	internal StackPanel GraphicLow;

	internal StackPanel GraphicMedium;

	internal StackPanel GraphicHigh;

	internal TextBlock LauncherIdMTA;

	internal Grid SettingsGTA5Block;

	internal TextBlock RageMpFolderPath;

	internal TextBlock Gta5FolderPath;

	internal TextBlock LauncherIdGTA5;

	internal Grid ServersBlockMTA;

	internal Grid GridFavoriteServer;

	internal System.Windows.Controls.Image FavoriteServerIcon;

	internal TextBlock FavoriteServerName;

	internal TextBlock FavoriteServerPlayerCount;

	internal TextBlock FavoriteServerMaxPlayerCount;

	internal ScrollViewer Scroll;

	internal Grid GridServers;

	internal Grid ServersBlockGta5;

	internal Grid GridFavoriteServerGta5;

	internal TextBlock FavoriteServerNumberGta5;

	internal TextBlock FavoriteServerNameGta5;

	internal TextBlock FavoriteServerPlayerCountGta5;

	internal TextBlock FavoriteServerMaxPlayerCountGta5;

	internal ScrollViewer ScrollGta5;

	internal Grid GridServersGta5;

	private bool _contentLoaded;

	private List<News> NewsListMta { get; set; }

	private List<News> NewsListGta5 { get; set; }

	private List<GameServer> ServerListMTA { get; set; }

	private List<GameServer> ServerListGTA5 { get; set; }

	private FrameworkElement FindElementWithTag(string tag)
	{
		return FindElementWithTag(tag, System.Windows.Application.Current.MainWindow);
	}

	private FrameworkElement FindElementWithTag(string tag, DependencyObject parent)
	{
		if (parent == null)
		{
			return null;
		}
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is FrameworkElement { Tag: not null } frameworkElement && frameworkElement.Tag.ToString() == tag)
			{
				return frameworkElement;
			}
			FrameworkElement frameworkElement2 = FindElementWithTag(tag, child);
			if (frameworkElement2 != null)
			{
				return frameworkElement2;
			}
		}
		return null;
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			DragMove();
		}
		catch
		{
		}
	}

	private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (!(sender is ScrollViewer scrollViewer))
		{
			return;
		}
		e.Handled = true;
		double verticalOffset = scrollViewer.VerticalOffset;
		double scrollableHeight = scrollViewer.ScrollableHeight;
		if (e.Delta > 0)
		{
			if (verticalOffset > 0.0)
			{
				targetOffset = Math.Max(verticalOffset - 76.0, 0.0);
			}
		}
		else if (verticalOffset < scrollableHeight)
		{
			targetOffset = Math.Min(verticalOffset + 76.0, scrollableHeight);
		}
		if (scrollTimer.IsEnabled)
		{
			scrollTimer.Stop();
		}
		if (!scrollTimer.IsEnabled)
		{
			scrollTimer.Tag = scrollViewer;
			scrollTimer.Start();
		}
	}

	private void ScrollTimer_Tick(object sender, EventArgs e)
	{
		Task.Run(delegate
		{
			DispatcherTimer timer = sender as DispatcherTimer;
			if (timer != null)
			{
				ScrollViewer scrollViewer = timer.Tag as ScrollViewer;
				if (scrollViewer != null)
				{
					scrollViewer.Dispatcher.Invoke(delegate
					{
						double verticalOffset = scrollViewer.VerticalOffset;
						double num = targetOffset - verticalOffset;
						if (Math.Abs(num) < 1.0)
						{
							scrollViewer.ScrollToVerticalOffset(targetOffset);
							timer.Stop();
						}
						else
						{
							double num2 = num * 0.2;
							scrollViewer.ScrollToVerticalOffset(verticalOffset + num2);
						}
					});
				}
			}
		});
	}

	private void CopyLauncherIdMTA(object sender, EventArgs e)
	{
		System.Windows.Clipboard.SetText(_CLIENT_ID);
		SystemSounds.Asterisk.Play();
	}

	private void CloseLauncher(object sender, RoutedEventArgs e)
	{
		_SETTINGS.LauncherSettings["autostart"] = AutoStart.IsChecked;
		_SETTINGS.LauncherSettings["autologin"] = AutoLogin.IsChecked;
		if (Gta5Block.Visibility == Visibility.Visible)
		{
			_SETTINGS.LauncherSettings["project"] = "gta5";
		}
		else if (MTABlock.Visibility == Visibility.Visible)
		{
			_SETTINGS.LauncherSettings["project"] = "mta";
		}
		if (IsRunningAsAdmin())
		{
			if (AutoStart.IsChecked == true)
			{
				AddToStartup();
			}
			else
			{
				RemoveFromStartup();
			}
		}
		DebugLog("Exit launcher...\n\n");
		_SETTINGS.SaveSettings();

		// Stop monitoring timer
		_MonitoringTimer.Stop();
		_MonitoringClient?.Close();
		_MonitoringClient?.Dispose();

		System.Windows.Application.Current.Shutdown();
	}

	private void MinimizeLauncher(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void NewsLink(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(NewsLinkUrl.Tag.ToString());
		}
		catch
		{
		}
	}

	private void OpenLink(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			string text = frameworkElement.Tag as string;
			if (!string.IsNullOrEmpty(text))
			{
				Process.Start(text);
			}
		}
	}

	private void SetChecked(object sender, RoutedEventArgs e)
	{
		if (sender is TextBlock textBlock)
		{
			if (textBlock.Text == "Запускати лаунчер при старті Windows")
			{
				AutoStart.IsChecked = !AutoStart.IsChecked;
			}
			else if (textBlock.Text == "Автоматичний вхід після під'єднання на сервер")
			{
				AutoLogin.IsChecked = !AutoLogin.IsChecked;
			}
		}
	}

	private void UpdateUiGraphic(object sender, RoutedEventArgs e)
	{
		if (sender is StackPanel stackPanel)
		{
			GraphicLow.Opacity = 0.3;
			((Ellipse)FindElementWithTag("Radio", GraphicLow)).Style = (Style)FindResource("RadioUnchecked");
			GraphicLow.IsEnabled = true;
			GraphicMedium.Opacity = 0.3;
			((Ellipse)FindElementWithTag("Radio", GraphicMedium)).Style = (Style)FindResource("RadioUnchecked");
			GraphicMedium.IsEnabled = true;
			GraphicHigh.Opacity = 0.3;
			((Ellipse)FindElementWithTag("Radio", GraphicHigh)).Style = (Style)FindResource("RadioUnchecked");
			GraphicHigh.IsEnabled = true;
			stackPanel.Opacity = 1.0;
			((Ellipse)FindElementWithTag("Radio", stackPanel)).Style = (Style)FindResource("RadioChecked");
			stackPanel.IsEnabled = false;
			if (stackPanel.Name == "GraphicLow")
			{
				_SETTINGS.LauncherSettings["graphic"] = "medium";
			}
			else if (stackPanel.Name == "GraphicMedium")
			{
				_SETTINGS.LauncherSettings["graphic"] = "medium";
			}
			else if (stackPanel.Name == "GraphicHigh")
			{
				_SETTINGS.LauncherSettings["graphic"] = "high";
			}
		}
	}

	private void SelectGraphic(object sender, RoutedEventArgs e)
	{
		if (!(sender is StackPanel stackPanel))
		{
			return;
		}
		bool flag = IsExistProcess("KOZAK RP") || IsExistProcess("gta_sa");
		if (flag)
		{
			MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(this, "Зміни набудуть чинності після перезавантаження гри. Перезавантажити зараз?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No, System.Windows.MessageBoxOptions.None);
			if (messageBoxResult != MessageBoxResult.Yes || _CLIENT_ADMIN)
			{
				return;
			}
			killProccess("KOZAK RP");
			killProccess("gta_sa");
		}
		GraphicLow.Opacity = 0.3;
		((Ellipse)FindElementWithTag("Radio", GraphicLow)).Style = (Style)FindResource("RadioUnchecked");
		GraphicLow.IsEnabled = true;
		GraphicMedium.Opacity = 0.3;
		((Ellipse)FindElementWithTag("Radio", GraphicMedium)).Style = (Style)FindResource("RadioUnchecked");
		GraphicMedium.IsEnabled = true;
		GraphicHigh.Opacity = 0.3;
		((Ellipse)FindElementWithTag("Radio", GraphicHigh)).Style = (Style)FindResource("RadioUnchecked");
		GraphicHigh.IsEnabled = true;
		stackPanel.Opacity = 1.0;
		((Ellipse)FindElementWithTag("Radio", stackPanel)).Style = (Style)FindResource("RadioChecked");
		stackPanel.IsEnabled = false;
		if (stackPanel.Name == "GraphicLow")
		{
			_SETTINGS.LauncherSettings["graphic"] = "medium";
		}
		else if (stackPanel.Name == "GraphicMedium")
		{
			_SETTINGS.LauncherSettings["graphic"] = "medium";
		}
		else if (stackPanel.Name == "GraphicHigh")
		{
			_SETTINGS.LauncherSettings["graphic"] = "high";
		}
		ApplyGraphics(is_run: false);
		if (flag)
		{
			PlayGameMTA(null, null);
		}
	}

	private void ApplyGraphics(bool is_run)
	{
		try
		{
			GameFiles[] source = JsonConvert.DeserializeObject<GameFiles[]>(_FILES_PATH_AND_HASHES);
			GameFiles gameFiles = source.FirstOrDefault((GameFiles f) => f.path.EndsWith("gta_low.dat"));
			GameFiles gameFiles2 = source.FirstOrDefault((GameFiles f) => f.path.EndsWith("gta_hight.dat"));
			GameFiles gameFiles3 = source.FirstOrDefault((GameFiles f) => f.path.EndsWith("gta.dat"));
			if (gameFiles == null || gameFiles2 == null || gameFiles3 == null)
			{
				return;
			}
			string text = _CLIENT_PATH_LAUNCHER + "\\" + gameFiles.path.Replace("/", "\\");
			string text2 = _CLIENT_PATH_LAUNCHER + "\\" + gameFiles2.path.Replace("/", "\\");
			string text3 = _CLIENT_PATH_LAUNCHER + "\\" + gameFiles3.path.Replace("/", "\\");
			if (File.Exists(text) && File.Exists(text2) && File.Exists(text3))
			{
				if (_SETTINGS.LauncherSettings["graphic"] == "medium")
				{
					File.Copy(text, text3, overwrite: true);
				}
				else
				{
					File.Copy(text2, text3, overwrite: true);
				}
			}
		}
		catch (IOException ex)
		{
			DebugLog("Error change graphics: " + ex.Message, 2);
			SendErrorReport("graphics", "Не вдалося змінити графіку на " + _SETTINGS.LauncherSettings["graphic"] + ", is_ruun: " + is_run + ", помилка: " + ex.Message);
		}
	}

	private void ChangeMainBlockStyle(string screen, bool isIniting = false)
	{
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		string text6 = null;
		string text7 = null;
		string text8 = null;
		string text9 = null;
		string text10 = null;
		string text11 = null;
		if (!(screen == "mta"))
		{
			if (!(screen == "gta5"))
			{
				if (newsMtaTimer.IsEnabled)
				{
					newsMtaTimer.Stop();
				}
				if (newsGta5Timer.IsEnabled)
				{
					newsGta5Timer.Stop();
				}
				return;
			}
			text = "pack://application:,,,/background_gta5.png";
			text3 = "#00000000";
			text4 = "#08101C";
				text2 = ResolveAssetUri("logo_gta5.png", "pack://application:,,,/logo_gta5.png");
			text5 = "https://ukraine-gta5.com.ua/";
			text6 = "https://forum.ukraine-gta5.com.ua/";
			text11 = "https://ukraine-gta5.com.ua/cabinet/login";
			text7 = GTA5Project.instagram_url;
			text9 = GTA5Project.discord_url;
			text8 = GTA5Project.telegram_url;
			text10 = GTA5Project.version;
			if (newsMtaTimer.IsEnabled)
			{
				newsMtaTimer.Stop();
			}
			if (!isIniting && !newsGta5Timer.IsEnabled)
			{
				newsGta5Timer.Interval = TimeSpan.FromSeconds(10.0);
				// Don't add Tick handler again - it's already added in initialization
				newsGta5Timer.Start();
				DebugLog("GTA5 news timer restarted");
			}
			ProjectMTAButton.Style = (Style)FindResource("MtaProjectButtonStyle");
		}
		else
		{
			text = "pack://application:,,,/background.png";
			text3 = "#00000000";
			text4 = "#0F0F16";
				text2 = "pack://application:,,,/server_mta.png";
			text5 = "https://kozakrp.qniks.me/";
			text6 = "https://kozakrp.qniks.me/";
			text11 = "https://kozakrp.qniks.me/cabinet.php";
			text7 = MTAProject.instagram_url;
			text9 = MTAProject.discord_url;
			text8 = MTAProject.telegram_url;
			text10 = MTAProject.version;
			if (newsGta5Timer.IsEnabled)
			{
				newsGta5Timer.Stop();
			}
			if (!isIniting && !newsMtaTimer.IsEnabled)
			{
				newsMtaTimer.Interval = TimeSpan.FromSeconds(10.0);
				// Don't add Tick handler again - it's already added in initialization
				newsMtaTimer.Start();
				DebugLog("MTA news timer restarted");
			}
			ProjectMTAButton.Style = (Style)FindResource("MtaProjectButtonStyleActive");
		}
		if (text != null)
		{
			ImageBrush background = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(text))
			};
			borderElement.Background = background;
		}
		if (text3 != null && text4 != null)
		{
			Border border = LeftMenu.FindName("LeftMenu") as Border;
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
			linearGradientBrush.StartPoint = new System.Windows.Point(0.0, 0.0);
			linearGradientBrush.EndPoint = new System.Windows.Point(0.0, 1.0);
			System.Windows.Media.Color color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(text3);
			System.Windows.Media.Color color2 = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(text4);
			linearGradientBrush.GradientStops.Add(new GradientStop(color, 0.0));
			linearGradientBrush.GradientStops.Add(new GradientStop(color2, 1.0));
			LeftMenuBorder.Background = linearGradientBrush;
		}
		if (text2 != null && text5 != null)
		{
			HeaderImage.Source = new BitmapImage(new Uri(text2));
			HeaderImage.Tag = text5;
			SiteButton.Tag = text5;
		}
		if (text11 != null)
		{
			CabinetButton.Tag = text11;
		}
		if (text8 != null && text9 != null && text7 != null && text6 != null)
		{
			DiscordButton.Tag = text9;
			TelegramButton.Tag = text8;
			InstagramButton.Tag = text7;
			ForumButton.Tag = text6;
		}
		if (text10 != null)
		{
			GameVersion.Text = "Версія гри: " + text10;
		}
	}

	private void PremiumDaysMTA(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			((System.Windows.Controls.Button)FindElementWithTag("30", PremiumButtonsMTA)).Style = (Style)FindResource("PremiumButtonMTA");
			((System.Windows.Controls.Button)FindElementWithTag("30", PremiumButtonsMTA)).IsEnabled = true;
			((System.Windows.Controls.Button)FindElementWithTag("14", PremiumButtonsMTA)).Style = (Style)FindResource("PremiumButtonMTA");
			((System.Windows.Controls.Button)FindElementWithTag("14", PremiumButtonsMTA)).IsEnabled = true;
			((System.Windows.Controls.Button)FindElementWithTag("7", PremiumButtonsMTA)).Style = (Style)FindResource("PremiumButtonMTA");
			((System.Windows.Controls.Button)FindElementWithTag("7", PremiumButtonsMTA)).IsEnabled = true;
			button.IsEnabled = false;
			button.Style = (Style)FindResource("PremiumButtonMTAActive");
			if ((string)button.Tag == "30")
			{
				PremiumCost.Text = "849";
			}
			else if ((string)button.Tag == "14")
			{
				PremiumCost.Text = "499";
			}
			else if ((string)button.Tag == "7")
			{
				PremiumCost.Text = "279";
			}
		}
	}

	private void PremiumDaysGTA5(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			((System.Windows.Controls.Button)FindElementWithTag("diamond", PremiumGTA5Buttons)).Style = (Style)FindResource("PremiumButtonGTA5");
			((System.Windows.Controls.Button)FindElementWithTag("diamond", PremiumGTA5Buttons)).IsEnabled = true;
			((System.Windows.Controls.Button)FindElementWithTag("platinum", PremiumGTA5Buttons)).Style = (Style)FindResource("PremiumButtonGTA5");
			((System.Windows.Controls.Button)FindElementWithTag("platinum", PremiumGTA5Buttons)).IsEnabled = true;
			((System.Windows.Controls.Button)FindElementWithTag("gold", PremiumGTA5Buttons)).Style = (Style)FindResource("PremiumButtonGTA5");
			((System.Windows.Controls.Button)FindElementWithTag("gold", PremiumGTA5Buttons)).IsEnabled = true;
			((System.Windows.Controls.Button)FindElementWithTag("silver", PremiumGTA5Buttons)).Style = (Style)FindResource("PremiumButtonGTA5");
			((System.Windows.Controls.Button)FindElementWithTag("silver", PremiumGTA5Buttons)).IsEnabled = true;
			button.IsEnabled = false;
			button.Style = (Style)FindResource("PremiumButtonGTA5Active");
			Console.WriteLine((string)button.Tag);
			if ((string)button.Tag == "diamond")
			{
				PremiumGTA5Cost.Text = "1950";
			}
			else if ((string)button.Tag == "platinum")
			{
				PremiumGTA5Cost.Text = "980";
			}
			else if ((string)button.Tag == "gold")
			{
				PremiumGTA5Cost.Text = "510";
			}
			else if ((string)button.Tag == "silver")
			{
				PremiumGTA5Cost.Text = "380";
			}
		}
	}

	private void OpenGameFolder(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
		}
		catch
		{
		}
	}

	private void OpenRageMpFolder(object sender, RoutedEventArgs e)
	{
		string rageMpPath = GetRageMpPath();
		if (rageMpPath == null)
		{
			SetRageMpFolder(sender, e);
			return;
		}
		try
		{
			Process.Start(rageMpPath);
		}
		catch
		{
		}
	}

	private void SetRageMpFolder(object sender, RoutedEventArgs e)
	{
		string rageMpPath = GetRageMpPath();
		if (rageMpPath != null && IsExistRageByPath(rageMpPath))
		{
			try
			{
				Process.Start(rageMpPath);
				return;
			}
			catch
			{
				return;
			}
		}
		string text = BrowsFolderPath("Оберіть дерикторію з RAGE Multiplayer");
		if (text == null)
		{
			return;
		}
		if (!IsExistRageByPath(text))
		{
			System.Windows.MessageBox.Show("Вказаний шлях не є директорією RAGE Multiplayer. Спробуйте обрати інший шлях або перевстановити RAGE Multiplayer.", "UKRAINE GTA5", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\RAGE-MP"))
		{
			registryKey.SetValue("rage_path", text);
		}
		InitializeGTA5Game();
	}

	private void OpeGta5Folder(object sender, RoutedEventArgs e)
	{
		string gta5Path = GetGta5Path();
		if (gta5Path == null || !IsExistGTA5ByPath(gta5Path))
		{
			SetGta5Folder(sender, e);
			return;
		}
		try
		{
			Process.Start(gta5Path);
		}
		catch
		{
		}
	}

	private void SetGta5Folder(object sender, RoutedEventArgs e)
	{
		string gta5Path = GetGta5Path();
		if (gta5Path == null && IsExistGTA5ByPath(gta5Path))
		{
			return;
		}
		string path = BrowsFolderPath("Оберіть дерикторію з Grand Theft Auto V");
		if (path == null)
		{
			return;
		}
		if (!IsExistGTA5ByPath(path))
		{
			System.Windows.MessageBox.Show("Вказаний шлях не є директорією Grand Theft Auto V. Спробуйте обрати інший шлях або перевстановити Grand Theft Auto V.", "UKRAINE GTA5", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\RAGE-MP"))
		{
			registryKey.SetValue("game_v_path", path);
		}
		using (RegistryKey registryKey2 = Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Rockstar Games\\Grand Theft Auto V"))
		{
			registryKey2.SetValue("InstallFolder", path);
		}
		Gta5FolderPath.Dispatcher.Invoke(delegate
		{
			Gta5FolderPath.Text = path;
		});
		InitializeGTA5Game();
	}

	private void OpenDonate(object sender, RoutedEventArgs e)
	{
		if (ProjectMTAButton.Style == (Style)FindResource("MtaProjectButtonStyleActive"))
		{
			OpenDonateMTA();
		}
		else
		{
			OpenDonateGTA5();
		}
	}

	private void OpenDonateMTA()
	{
		Storyboard storyboard = null;
		if (SettingsMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABBlocksSettingsStoryboard");
		}
		else if (SettingsGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksSettingsStoryboard");
		}
		else if (MTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksMainStoryboard");
		}
		else if (Gta5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksGta5Storyboard");
		}
		else if (DonateGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksDonateStoryboard");
		}
		storyboard.Completed += delegate
		{
			DonateButton.Style = (Style)FindResource("DonateButtonStyleActive");
			DonateButton.IsEnabled = false;
			SettingsButton.IsEnabled = true;
			MTABlock.Visibility = Visibility.Collapsed;
			ServersBlockMTA.Visibility = Visibility.Collapsed;
			SettingsMTABlock.Visibility = Visibility.Collapsed;
			SettingsGTA5Block.Visibility = Visibility.Collapsed;
			Gta5Block.Visibility = Visibility.Collapsed;
			ServersBlockGta5.Visibility = Visibility.Collapsed;
			DonateGTA5Block.Visibility = Visibility.Collapsed;
			Storyboard storyboard2 = (Storyboard)FindResource("ShowMTADonateContentStoryboard");
			storyboard2.Begin();
			DonateMTABlock.Visibility = Visibility.Visible;
		};
		storyboard.Begin();
		ChangeMainBlockStyle("donate");
	}

	private void OpenDonateGTA5()
	{
		Storyboard storyboard = null;
		if (SettingsMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABBlocksSettingsStoryboard");
		}
		else if (SettingsGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksSettingsStoryboard");
		}
		else if (MTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksMainStoryboard");
		}
		else if (Gta5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksGta5Storyboard");
		}
		else if (DonateMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABlocksDonateStoryboard");
		}
		storyboard.Completed += delegate
		{
			DonateButton.Style = (Style)FindResource("DonateButtonStyleActive");
			DonateButton.IsEnabled = false;
			SettingsButton.IsEnabled = true;
			MTABlock.Visibility = Visibility.Collapsed;
			ServersBlockMTA.Visibility = Visibility.Collapsed;
			SettingsMTABlock.Visibility = Visibility.Collapsed;
			SettingsGTA5Block.Visibility = Visibility.Collapsed;
			Gta5Block.Visibility = Visibility.Collapsed;
			ServersBlockGta5.Visibility = Visibility.Collapsed;
			DonateMTABlock.Visibility = Visibility.Collapsed;
			Storyboard storyboard2 = (Storyboard)FindResource("ShowGTA5DonateContentStoryboard");
			storyboard2.Begin();
			DonateGTA5Block.Visibility = Visibility.Visible;
		};
		storyboard.Begin();
		ChangeMainBlockStyle("donate_gta5");
	}

	private void OpenSettings(object sender, RoutedEventArgs e)
	{
		Storyboard storyboard = null;
		bool is_mta_screen = ProjectMTAButton.Style == (Style)FindResource("MtaProjectButtonStyleActive");
		if (MTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksMainStoryboard");
		}
		else if (Gta5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksGta5Storyboard");
		}
		else if (is_mta_screen && SettingsGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksSettingsStoryboard");
		}
		else if (!is_mta_screen && SettingsMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABBlocksSettingsStoryboard");
		}
		else if (DonateMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABlocksDonateStoryboard");
		}
		else if (DonateGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksDonateStoryboard");
		}
		storyboard.Completed += delegate
		{
			DonateButton.Style = (Style)FindResource("DonateButtonStyle");
			DonateButton.IsEnabled = true;
			SettingsButton.IsEnabled = false;
			MTABlock.Visibility = Visibility.Collapsed;
			ServersBlockMTA.Visibility = Visibility.Collapsed;
			Gta5Block.Visibility = Visibility.Collapsed;
			ServersBlockGta5.Visibility = Visibility.Collapsed;
			DonateGTA5Block.Visibility = Visibility.Collapsed;
			DonateMTABlock.Visibility = Visibility.Collapsed;
			if (is_mta_screen)
			{
				SettingsGTA5Block.Visibility = Visibility.Collapsed;
			}
			else
			{
				SettingsMTABlock.Visibility = Visibility.Collapsed;
			}
			Storyboard storyboard2 = ((!is_mta_screen) ? ((Storyboard)FindResource("ShowGTA5SettingsContentStoryboard")) : ((Storyboard)FindResource("ShowMTASettingsContentStoryboard")));
			storyboard2.Begin();
			if (is_mta_screen)
			{
				SettingsGTA5Block.Visibility = Visibility.Collapsed;
				SettingsMTABlock.Visibility = Visibility.Visible;
			}
			else
			{
				SettingsGTA5Block.Visibility = Visibility.Visible;
				SettingsMTABlock.Visibility = Visibility.Collapsed;
			}
		};
		storyboard.Begin();
		if (is_mta_screen)
		{
			ChangeMainBlockStyle("settings_mta");
		}
		else
		{
			ChangeMainBlockStyle("settings_gta5");
		}
	}

	private void OpenMTA(object sender, RoutedEventArgs e)
	{
		if (MTABlock.Visibility == Visibility.Visible)
		{
			return;
		}
		Storyboard storyboard = null;
		if (SettingsMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABBlocksSettingsStoryboard");
		}
		else if (SettingsGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksSettingsStoryboard");
		}
		else if (Gta5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksGta5Storyboard");
		}
		else if (DonateMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABlocksDonateStoryboard");
		}
		else if (DonateGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksDonateStoryboard");
		}
		storyboard.Completed += async delegate
		{
			DonateButton.Style = (Style)FindResource("DonateButtonStyle");
			DonateButton.IsEnabled = true;
			SettingsButton.IsEnabled = true;
			SettingsMTABlock.Visibility = Visibility.Collapsed;
			SettingsGTA5Block.Visibility = Visibility.Collapsed;
			DonateMTABlock.Visibility = Visibility.Collapsed;
			DonateGTA5Block.Visibility = Visibility.Collapsed;
			SettingsGTA5Block.Visibility = Visibility.Collapsed;
			Gta5Block.Visibility = Visibility.Collapsed;
			ServersBlockGta5.Visibility = Visibility.Collapsed;
			Storyboard showStoryboard = (Storyboard)FindResource("ShowMainContentStoryboard");
			showStoryboard.Begin();
			MTABlock.Visibility = Visibility.Visible;
			ServersBlockMTA.Visibility = Visibility.Visible;
			if (!isRenderedMtaNews)
			{
				newsMtaTimer.Dispatcher.Invoke(delegate
				{
					newsMtaTimer_Tick(null, null);
				});
				isRenderedMtaNews = true;
			}
			await Dispatcher.Yield(DispatcherPriority.Render);
			SyncUiMtaOnline();
		};
		storyboard.Begin();
		ChangeMainBlockStyle("mta");
	}

	private void OpenGTA5(object sender, RoutedEventArgs e)
	{
		if (Gta5Block.Visibility == Visibility.Visible)
		{
			return;
		}
		Storyboard storyboard = null;
		if (SettingsMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABBlocksSettingsStoryboard");
		}
		else if (SettingsGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksSettingsStoryboard");
		}
		else if (MTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideBlocksMainStoryboard");
		}
		else if (DonateMTABlock.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideMTABlocksDonateStoryboard");
		}
		else if (DonateGTA5Block.Visibility == Visibility.Visible)
		{
			storyboard = (Storyboard)FindResource("HideGTA5BlocksDonateStoryboard");
		}
		storyboard.Completed += async delegate
		{
			DonateButton.Style = (Style)FindResource("DonateButtonStyle");
			DonateButton.IsEnabled = true;
			SettingsButton.IsEnabled = true;
			MTABlock.Visibility = Visibility.Collapsed;
			DonateMTABlock.Visibility = Visibility.Collapsed;
			DonateGTA5Block.Visibility = Visibility.Collapsed;
			SettingsGTA5Block.Visibility = Visibility.Collapsed;
			ServersBlockMTA.Visibility = Visibility.Collapsed;
			SettingsMTABlock.Visibility = Visibility.Collapsed;
			Storyboard showStoryboard = (Storyboard)FindResource("ShowGta5ContentStoryboard");
			showStoryboard.Begin();
			Gta5Block.Visibility = Visibility.Visible;
			ServersBlockGta5.Visibility = Visibility.Visible;
			if (!isRenderedGta5News)
			{
				newsGta5Timer.Dispatcher.Invoke(delegate
				{
					newsGta5Timer_Tick(null, null);
				});
				isRenderedGta5News = true;
			}
			await Dispatcher.Yield(DispatcherPriority.Render);
			SyncUiGta5Online();
		};
		storyboard.Begin();
		ChangeMainBlockStyle("gta5");
	}

	private bool IsRunningAsAdmin()
	{
		try
		{
			WindowsIdentity current = WindowsIdentity.GetCurrent();
			WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
			return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private string SimpleEncrypt(string data, string key)
	{
		try
		{
			using Aes aes = Aes.Create();
			aes.Key = Encoding.UTF8.GetBytes(key);
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			ICryptoTransform cryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			byte[] array = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
			byte[] array2 = new byte[aes.IV.Length + array.Length];
			aes.IV.CopyTo(array2, 0);
			array.CopyTo(array2, aes.IV.Length);
			return Convert.ToBase64String(array2);
		}
		catch
		{
			return string.Empty;
		}
	}

	private string SimpleDecrypt(string encryptedData, string key)
	{
		try
		{
			using Aes aes = Aes.Create();
			aes.Key = Encoding.UTF8.GetBytes(key);
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			byte[] array = Convert.FromBase64String(encryptedData);
			byte[] array2 = new byte[aes.IV.Length];
			byte[] array3 = new byte[array.Length - aes.IV.Length];
			Array.Copy(array, array2, array2.Length);
			Array.Copy(array, array2.Length, array3, 0, array3.Length);
			ICryptoTransform cryptoTransform = aes.CreateDecryptor(aes.Key, array2);
			byte[] bytes = cryptoTransform.TransformFinalBlock(array3, 0, array3.Length);
			return Encoding.UTF8.GetString(bytes);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string GetClientID()
	{
		string text = "none";
		string text2 = "none";
		string text3 = "none";
		int num = 0;
		string empty;
		while (true)
		{
			try
			{
				ManagementClass managementClass = new ManagementClass("win32_processor");
				ManagementObjectCollection instances = managementClass.GetInstances();
				using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = instances.GetEnumerator();
				if (managementObjectEnumerator.MoveNext())
				{
					ManagementBaseObject current = managementObjectEnumerator.Current;
					text = current.Properties["processorID"].Value.ToString();
				}
			}
			catch
			{
			}
			try
			{
				ManagementObject managementObject = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
				managementObject.Get();
				text2 = managementObject["VolumeSerialNumber"].ToString();
			}
			catch
			{
			}
			try
			{
				ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
				ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
				foreach (ManagementObject item in managementObjectCollection)
				{
					text3 = item["SerialNumber"].ToString();
				}
			}
			catch
			{
			}
			string stringSha256Hash = GetStringSha256Hash(text + text2 + text3);
			string text4 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MTA GLAB3", "Launcher");
			string text5 = System.IO.Path.Combine(text4, "info.bin");
			string text6 = System.IO.Path.Combine(text4, "context.bin");
			string path = System.IO.Path.Combine(text4, "crashdump.bin");
			empty = string.Empty;
			string keyName = "Software\\UKRAINEGTA: GLAB3\\Common";
			string valueName = "launcher_id";
			string empty2 = string.Empty;
			try
			{
				empty2 = GetRegistryValue(keyName, valueName);
				if (empty2 == string.Empty || !IsSHA256Hash(empty2))
				{
					SetRegistryValue(keyName, valueName, stringSha256Hash);
					empty2 = stringSha256Hash;
				}
			}
			catch
			{
				empty2 = stringSha256Hash;
			}
			try
			{
				Directory.CreateDirectory(text4);
				if (File.Exists(path))
				{
					text3 = File.ReadAllText(path);
				}
				else
				{
					File.WriteAllText(path, text3);
				}
				text3 = GetStringSha256Hash(text3).Substring(0, 16);
				if (File.Exists(text5) && File.Exists(text6) && IsBinaryFile(text5) && IsBinaryFile(text6))
				{
					byte[] cipherText = File.ReadAllBytes(text5);
					empty = DecryptBytesToText_Aes(text6, cipherText, text3);
					if (empty != empty2)
					{
						empty2 = empty;
						SetRegistryValue(keyName, valueName, empty);
					}
				}
				else
				{
					File.Delete(text5);
					File.Delete(text6);
					empty = empty2;
					byte[] bytes = EncryptStringToBytes_Aes(text6, empty2, text3);
					File.WriteAllBytes(text5, bytes);
				}
				try
				{
					if (empty2 != empty)
					{
						SetRegistryValue(keyName, valueName, empty);
					}
				}
				catch
				{
				}
			}
			catch
			{
				empty = empty2;
				try
				{
					File.Delete(text5);
					File.Delete(text6);
				}
				catch
				{
				}
				if (num == 0)
				{
					num++;
					continue;
				}
			}
			break;
		}
		return empty;
	}

	private static bool IsSHA256Hash(string input)
	{
		Regex regex = new Regex("\\b[A-Fa-f0-9]{64}\\b");
		return regex.IsMatch(input);
	}

	private static void SetRegistryValue(string keyName, string valueName, string value, RegistryHive hive = RegistryHive.CurrentUser)
	{
		RegistryKey registryKey = ((hive != RegistryHive.LocalMachine) ? Registry.CurrentUser : Registry.LocalMachine);
		using RegistryKey registryKey2 = registryKey.CreateSubKey(keyName);
		registryKey2?.SetValue(valueName, value);
	}

	private static string GetRegistryValue(string keyName, string valueName, RegistryHive hive = RegistryHive.CurrentUser)
	{
		RegistryKey registryKey = ((hive != RegistryHive.LocalMachine) ? Registry.CurrentUser : Registry.LocalMachine);
		using (RegistryKey registryKey2 = registryKey.CreateSubKey(keyName))
		{
			if (registryKey2 != null && registryKey2.GetValue(valueName) != null)
			{
				return registryKey2.GetValue(valueName).ToString();
			}
		}
		return string.Empty;
	}

	internal static string GetStringSha256Hash(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		try
		{
			using SHA256Managed sHA256Managed = new SHA256Managed();
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			byte[] array = sHA256Managed.ComputeHash(bytes);
			return BitConverter.ToString(array).Replace("-", string.Empty);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static bool IsBinaryFile(string filePath)
	{
		using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
		{
			int num = 0;
			int num2 = 0;
			byte[] array = new byte[1024];
			while ((num = fileStream.Read(array, 0, array.Length)) > 0)
			{
				for (int i = 0; i < num; i++)
				{
					if (array[i] < 32 && array[i] != 9 && array[i] != 10 && array[i] != 13)
					{
						return true;
					}
				}
				num2 += num;
				if (num2 >= 1024)
				{
					break;
				}
			}
		}
		return false;
	}

	private static byte[] EncryptStringToBytes_Aes(string filePathIV4, string plainText, string key)
	{
		using Aes aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(key);
		aes.GenerateIV();
		ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
		using MemoryStream memoryStream = new MemoryStream();
		using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
		{
			using StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(plainText);
		}
		File.WriteAllBytes(filePathIV4, aes.IV);
		return memoryStream.ToArray();
	}

	private static string DecryptBytesToText_Aes(string filePathIV4, byte[] cipherText, string key)
	{
		using Aes aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(key);
		if (File.Exists(filePathIV4) && IsBinaryFile(filePathIV4))
		{
			aes.IV = File.ReadAllBytes(filePathIV4);
			using MemoryStream stream = new MemoryStream(cipherText);
			using CryptoStream stream2 = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
			using StreamReader streamReader = new StreamReader(stream2);
			return streamReader.ReadToEnd();
		}
		throw new InvalidOperationException("IV4 File corrupted.");
	}

	private static string CalculateMD5(string filePath)
	{
		using MD5 mD = MD5.Create();
		using FileStream inputStream = File.OpenRead(filePath);
		byte[] array = mD.ComputeHash(inputStream);
		return BitConverter.ToString(array).Replace("-", "").ToLower();
	}

	private static string CalculateMD5FromString(string input)
	{
		using MD5 mD = MD5.Create();
		byte[] bytes = Encoding.UTF8.GetBytes(input);
		byte[] array = mD.ComputeHash(bytes);
		return BitConverter.ToString(array).Replace("-", "").ToLower();
	}

	public static string GetClientFriendlyOSName()
	{
		string result = "none";
		try
		{
			ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				result = item["Caption"].ToString();
			}
		}
		catch
		{
		}
		return result;
	}

	private string ReadFromURL(string url)
	{
		try
		{
			if (_WEB_CLIENT.Headers.Get("user-agent") == null || _WEB_CLIENT.Headers.Get("user-agent") != _WEB_CLIENT_USERAGENT)
			{
				_WEB_CLIENT.Headers.Set("user-agent", _WEB_CLIENT_USERAGENT);
			}
		}
		catch
		{
			_WEB_CLIENT.Headers.Set("user-agent", _WEB_CLIENT_USERAGENT);
		}
		string result;
		try
		{
			result = _WEB_CLIENT.DownloadString(url);
		}
		catch (Exception ex)
		{
			result = "error";
			SendErrorReport("read_from_url", "URL=" + url + ";" + ex.Message);
		}
		return result;
	}

	private void SendErrorReport(string _step, string _message)
	{
		if (_message.Length < 6)
		{
			_message = "none";
		}
	}

	private static void CreateDirectoriesIfNotExist(string directoryPath)
	{
		if (!Directory.Exists(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}
	}

	private void DebugLog(string text, int level = 0)
	{
		string log_path = _CLIENT_PATH_LAUNCHER + "\\launcher.log";
		CreateDirectoriesIfNotExist(System.IO.Path.GetDirectoryName(log_path));
		Task.Run(delegate
		{
			while (File.Exists(log_path))
			{
				try
				{
					using (File.Open(log_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
					{
					}
				}
				catch (IOException)
				{
					Thread.Sleep(150);
					continue;
				}
				break;
			}
			try
			{
				if (File.Exists(log_path))
				{
					FileInfo fileInfo = new FileInfo(log_path);
					if (fileInfo.Length > 1048576)
					{
						File.Delete(log_path);
					}
				}
				if (!File.Exists(log_path))
				{
					using (File.Create(log_path))
					{
					}
				}
			}
			catch
			{
			}
			string text2 = "";
			if (1 == 0)
			{
			}
			string text3 = level switch
			{
				1 => "[WARNING]", 
				2 => "[ERROR]", 
				_ => "[INFO]", 
			};
			if (1 == 0)
			{
			}
			text2 = text3;
			try
			{
				text = string.Format("{0} {1} {2}", DateTime.Now.ToString("[dd.MM.yyyy HH:mm:ss.fff]"), text2, text);
				using StreamWriter streamWriter = File.AppendText(log_path);
				streamWriter.WriteLine(text);
			}
			catch
			{
			}
		});
	}

	private void AddToStartup()
	{
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			string value = Assembly.GetEntryAssembly()?.Location;
			if (!string.IsNullOrWhiteSpace(value))
			{
				registryKey.SetValue("KOZAK_RP_Launcher", value);
			}
			registryKey.Close();
		}
		catch
		{
		}
	}

	private void RemoveFromStartup()
	{
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			registryKey.DeleteValue("KOZAK_RP_Launcher", throwOnMissingValue: false);
			registryKey.DeleteValue("UGLauncher", throwOnMissingValue: false);
			registryKey.DeleteValue("UKRAINE GTA [UPDATER]", throwOnMissingValue: false);
			registryKey.Close();
		}
		catch
		{
		}
	}

	private void RemoveLegacyUpdaterStartup()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			registryKey?.DeleteValue("UKRAINE GTA [UPDATER]", throwOnMissingValue: false);
		}
		catch
		{
		}
	}

	private void RunNewsMta(bool forceRun = false)
	{
		if ((forceRun || MTABlock.Visibility != Visibility.Collapsed) && NewsListMta != null && NewsListMta.Count != 0)
		{
			if (currentNewsIndexMta >= NewsListMta.Count)
			{
				currentNewsIndexMta = 0;
			}
			News news = NewsListMta[currentNewsIndexMta];
			try
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				// Try relative path first
				bitmapImage.UriSource = new Uri(news.ImageUrl, UriKind.Relative);
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				DebugLog("Successfully loaded MTA news image: " + news.ImageUrl);
				DoubleAnimation doubleAnimation = new DoubleAnimation(0.0, TimeSpan.FromSeconds(0.3));
				doubleAnimation.Completed += delegate
				{
					NewsImage.Source = bitmapImage;
					DoubleAnimation animation = new DoubleAnimation(1.0, TimeSpan.FromSeconds(0.3));
					NewsImage.BeginAnimation(UIElement.OpacityProperty, animation);
				};
				NewsImage.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
				NewsTitle.Text = news.Title;
				NewsDescription.Text = news.Description;
				NewsDate.Text = news.Date;
				NewsLinkUrl.Tag = news.Link;
			}
			catch (Exception ex)
			{
				DebugLog("Failed to load MTA news image: " + news.ImageUrl + ", Error: " + ex.Message, 2);
				DebugLog("Trying to load with pack:// URI");
				// Try pack:// URI as fallback
				try
				{
					BitmapImage fallbackImage = new BitmapImage();
					fallbackImage.BeginInit();
					fallbackImage.CacheOption = BitmapCacheOption.OnLoad;
					fallbackImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
					fallbackImage.UriSource = new Uri("pack://application:,,,/" + news.ImageUrl);
					fallbackImage.EndInit();
					fallbackImage.Freeze();
					NewsImage.Source = fallbackImage;
					DebugLog("Successfully loaded with pack:// URI");
				}
				catch (Exception ex2)
				{
					DebugLog("Failed to load with pack:// URI too: " + ex2.Message, 2);
					// Try loading from file system as last resort
					try
					{
						string assemblyLocation = Assembly.GetExecutingAssembly().Location;
						string assemblyDirectory = System.IO.Path.GetDirectoryName(assemblyLocation);
						string imagePath = System.IO.Path.Combine(assemblyDirectory, news.ImageUrl);
						if (File.Exists(imagePath))
						{
							BitmapImage fileImage = new BitmapImage();
							fileImage.BeginInit();
							fileImage.CacheOption = BitmapCacheOption.OnLoad;
							fileImage.UriSource = new Uri(imagePath);
							fileImage.EndInit();
							fileImage.Freeze();
							NewsImage.Source = fileImage;
							DebugLog("Successfully loaded from file system: " + imagePath);
						}
						else
						{
							DebugLog("File not found: " + imagePath, 2);
						}
					}
					catch (Exception ex3)
					{
						DebugLog("Failed to load from file system too: " + ex3.Message, 2);
					}
				}
				NewsTitle.Text = news.Title;
				NewsDescription.Text = news.Description;
				NewsDate.Text = news.Date;
				NewsLinkUrl.Tag = news.Link;
			}
			currentNewsIndexMta++;
		}
	}

	private async void newsMtaTimer_Tick(object sender, EventArgs e)
	{
		DebugLog("MTA news timer tick triggered, current index: " + currentNewsIndexMta);
		RunNewsMta();
	}

	private void RunNewsGta5(bool forceRun = false)
	{
		if ((forceRun || Gta5Block.Visibility != Visibility.Collapsed) && NewsListGta5 != null && NewsListGta5.Count != 0)
		{
			if (currentNewsIndexGta5 >= NewsListGta5.Count)
			{
				currentNewsIndexGta5 = 0;
			}
			News news = NewsListGta5[currentNewsIndexGta5];
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(news.ImageUrl);
			bitmapImage.EndInit();
			DoubleAnimation doubleAnimation = new DoubleAnimation(0.0, TimeSpan.FromSeconds(0.3));
			doubleAnimation.Completed += delegate
			{
				NewsImageGta5.Source = bitmapImage;
				DoubleAnimation animation = new DoubleAnimation(1.0, TimeSpan.FromSeconds(0.3));
				NewsImageGta5.BeginAnimation(UIElement.OpacityProperty, animation);
			};
			NewsImageGta5.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
			NewsTitleGta5.Text = news.Title;
			NewsDescriptionGta5.Text = news.Description;
			NewsDateGta5.Text = news.Date;
			NewsLinkUrlGta5.Tag = news.Link;
			currentNewsIndexGta5++;
		}
	}

	private async void newsGta5Timer_Tick(object sender, EventArgs e)
	{
		DebugLog("GTA5 news timer tick triggered, current index: " + currentNewsIndexGta5);
		RunNewsGta5();
	}

	private void InitializeOfflineContent()
	{
		try
		{
				_GAME_URL_DOWNLOAD = _OFFLINE_MTA_ARCHIVE_URL;
			_SETTINGS.LauncherSettings["project"] = "mta";
			_SETTINGS.LauncherSettings["favoriteServerMTA"] = "s1";
			_SETTINGS.LauncherSettings["favoriteServerGTA5"] = "s1";
			_SETTINGS.SaveSettings();
			MTABlock.Visibility = Visibility.Visible;
			ServersBlockMTA.Visibility = Visibility.Visible;
			Gta5Block.Visibility = Visibility.Collapsed;
			ServersBlockGta5.Visibility = Visibility.Collapsed;
			ChangeMainBlockStyle("mta", isIniting: true);
			ServerListMTA.Clear();
			ServerListGTA5.Clear();
			GridServers.Children.Clear();
			GridServers.RowDefinitions.Clear();
			GridServersGta5.Children.Clear();
			GridServersGta5.RowDefinitions.Clear();
			AddServerMTA("s1", "pack://application:,,,/mta-s1.png", "Центральна Україна", "0", "999", "144.76.220.86:30305", "");
			AddServerGTA5("s1", "Server #1", "0", "999", "144.76.220.86:303051", "Сервер тимчасово недоступний");
			_FAVORITE_MTA_SERVER = "s1";
			_FAVORITE_GTA5_SERVER = "s1";
			SelectMTAServerInWindow("s1");
			SelectServerGTA5InWindow("s1");
			TotalOnline.Text = "0";
			TotalOnlineGta5.Text = "0";
			TotalProjectsOnline.Text = "0";
			ProjectMTATotalOnline.Text = "0 гравців";
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			// Initialize monitoring for g1.ketrix.cloud:20013
			_MonitoringTimer.Interval = TimeSpan.FromSeconds(10.0);
			_MonitoringTimer.Tick += MonitoringTimer_Tick;
			_MonitoringTimer.Start();
			NewsListMta.Clear();
			NewsListGta5.Clear();
			string text = DateTime.Now.ToString("dd.MM.yyyy");
			NewsListMta.Add(new News
			{
				ImageUrl = "news.png",
				Date = "13.02.2026",
				Title = "ОНОВЛЕННЯ 3.9 | ФОРМУЛА КОХАННЯ",
				Description = "Хочеш першим дізнатися дату виходу та побачити всі фішки «романтичного» оновлення? Тоді чекаємо тебе на прем'єрі! Ти побачиш: нову систему інкасації, оновлений банкінг, святкові валентинки та ще багато секретних змін, про які ми ще не розповідали.",
				Link = "https://kozakrp.qniks.me/"
			});
			NewsListMta.Add(new News
			{
				ImageUrl = "news2.png",
				Date = "10.02.2026",
				Title = "НОВІ СКИДКИ ТА БОНУСИ",
				Description = "Спеціальні пропозиції для всіх гравців! Знижки на преміум, бонуси за донат та багато інших сюрпризів. Не пропустіть можливість отримати більше!",
				Link = "https://kozakrp.qniks.me/"
			});
			NewsListMta.Add(new News
			{
				ImageUrl = "news3.png",
				Date = "05.02.2026",
				Title = "ОНОВЛЕННЯ КАРТИ ТА ТРАНСПОРТУ",
				Description = "Нові локації, оновлений транспорт та багато інших цікавих змін. Досліджуй світ KOZAK RP з новими можливостями!",
				Link = "https://kozakrp.qniks.me/"
			});
			RunNewsMta(forceRun: true);
			RunNewsGta5(forceRun: true);

			// Start news timers after initialization
			newsMtaTimer.Interval = TimeSpan.FromSeconds(10.0);
			newsMtaTimer.Tick -= newsMtaTimer_Tick; // Remove any existing subscription
			newsMtaTimer.Tick += newsMtaTimer_Tick; // Add fresh subscription
			newsMtaTimer.Start();
			DebugLog("MTA news timer started after initialization, enabled: " + newsMtaTimer.IsEnabled);

			newsGta5Timer.Interval = TimeSpan.FromSeconds(10.0);
			newsGta5Timer.Tick -= newsGta5Timer_Tick; // Remove any existing subscription
			newsGta5Timer.Tick += newsGta5Timer_Tick; // Add fresh subscription
			newsGta5Timer.Start();
			DebugLog("GTA5 news timer started after initialization, enabled: " + newsGta5Timer.IsEnabled);

			MTAProject.version = "3.9";
			GTA5Project.version = "3.9";
			GameVersion.Text = "Версія гри: 3.9";
			if (string.IsNullOrWhiteSpace(_FILES_PATH_AND_HASHES))
			{
				_FILES_PATH_AND_HASHES = "[]";
			}
			InitializeMTAGame(fast_check: false);
			InitializeGTA5Game();
		}
		catch (Exception ex)
		{
			DebugLog("Offline init failed: " + ex.Message, 2);
		}
	}

	private void TryApplyRemoteReinstallRule()
	{
		try
		{
			string remoteValue = ReadFromURL(_REMOTE_REINSTALL_CHECK_URL)?.Trim();
			if (string.IsNullOrWhiteSpace(remoteValue) || string.Equals(remoteValue, "error", StringComparison.OrdinalIgnoreCase))
			{
				DebugLog("Cannot read remote check marker, skip forced reinstall check.", 1);
				return;
			}
			string markerPath = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER, _REMOTE_REINSTALL_CHECK_FILE);
			string localValue = string.Empty;
			try
			{
				if (File.Exists(markerPath))
				{
					localValue = File.ReadAllText(markerPath).Trim();
				}
			}
			catch
			{
				localValue = string.Empty;
			}
			if (string.IsNullOrWhiteSpace(localValue))
			{
				File.WriteAllText(markerPath, remoteValue);
				DebugLog("Created local check marker file.");
				return;
			}
			if (!string.Equals(localValue, remoteValue, StringComparison.Ordinal))
			{
				DebugLog($"Check marker changed: \"{localValue}\" -> \"{remoteValue}\". Starting full reinstall.");
				_REINSTALL_MTA = true;
				_FORCE_DOWNLOAD = true;
				_AUTO_REINSTALL_AFTER_CHECK = true;
				DeleteMtaGameFiels();
				File.WriteAllText(markerPath, remoteValue);
			}
		}
		catch (Exception ex)
		{
			DebugLog("Remote reinstall check failed: " + ex.Message, 1);
		}
	}

	private static void DownloadFile(string url, string savePath)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		HttpClient val = new HttpClient();
		try
		{
			((HttpHeaders)val.DefaultRequestHeaders).Add("User-Agent", "UKRAINE-GTA Launcher client");
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10.0));
			try
			{
				HttpResponseMessage result = val.GetAsync(url, cancellationTokenSource.Token).Result;
				if (result.IsSuccessStatusCode)
				{
					using (Stream stream = result.Content.ReadAsStreamAsync().Result)
					{
						using FileStream destination = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
						stream.CopyTo(destination);
						return;
					}
				}
				throw new Exception($"Failed to download file. Status code: {result.StatusCode}");
			}
			catch (OperationCanceledException)
			{
				throw new Exception("The request timed out after 10 seconds.");
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private bool IsMtaInstalled()
	{
		string gameRootPath = _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME;
		string launcherExePath = gameRootPath + "\\KOZAK RP.exe";
		if (File.Exists(launcherExePath))
		{
			return true;
		}
		string nestedLauncherExePath = System.IO.Path.Combine(gameRootPath, "game", "KOZAK RP.exe");
		if (File.Exists(nestedLauncherExePath))
		{
			NormalizeMtaInstallLayout();
			return File.Exists(launcherExePath);
		}
		return false;
	}

	private void HideSevenZipDirectory()
	{
		try
		{
			string path = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER, "7z");
			if (Directory.Exists(path))
			{
				// Remove System attribute to reduce antivirus suspicion
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				directoryInfo.Attributes = directoryInfo.Attributes | FileAttributes.Hidden;
			}
		}
		catch
		{
		}
	}

	private bool EnsureSevenZipRuntimeFiles()
	{
		try
		{
			RuntimeFile[] sEVEN_ZIP_RUNTIME_FILES = _SEVEN_ZIP_RUNTIME_FILES;
			foreach (RuntimeFile runtimeFile in sEVEN_ZIP_RUNTIME_FILES)
			{
				string text = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER, runtimeFile.file.Replace("/", "\\"));
				CreateDirectoriesIfNotExist(System.IO.Path.GetDirectoryName(text));
				bool flag = !File.Exists(text);
				if (!flag)
				{
					try
					{
						flag = !string.Equals(CalculateMD5(text), runtimeFile.hash, StringComparison.OrdinalIgnoreCase);
					}
					catch
					{
						flag = true;
					}
				}
				if (flag)
				{
					try
					{
						if (File.Exists(text))
						{
							FileInfo fileInfo = new FileInfo(text);
							if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
							{
								fileInfo.Attributes = FileAttributes.Normal;
							}
							File.Delete(text);
						}
					}
					catch
					{
					}
					DownloadFile(runtimeFile.path, text);
				}
				try
				{
					// Remove hidden attribute to reduce antivirus suspicion
					// new FileInfo(text).Attributes |= FileAttributes.Hidden;
				}
				catch
				{
				}
			}
			HideSevenZipDirectory();
			return true;
		}
		catch (Exception ex)
		{
			DebugLog("Cannot prepare 7z runtime. Details: " + ex.Message, 2);
			SendErrorReport("prepare_7z_runtime", ex.Message);
			return false;
		}
	}

	public void SyncUiMtaOnline()
	{
		foreach (GameServer serverListMTum in ServerListMTA)
		{
			((TextBlock)FindElementWithTag("current_online", serverListMTum.element)).Text = serverListMTum.players;
			((TextBlock)FindElementWithTag("max_players", serverListMTum.element)).Text = " / " + serverListMTum.max_players;
			((TextBlock)FindElementWithTag("server_name", serverListMTum.element)).Text = serverListMTum.name;
			((System.Windows.Controls.ProgressBar)FindElementWithTag("progress_bar", serverListMTum.element)).Value = Math.Round(double.Parse(serverListMTum.players) / double.Parse(serverListMTum.max_players) * 100.0);
		}
	}

	public void AddServerMTA(string id, string image_url, string name, string players, string max_players, string address, string closed_text)
	{
		if (ServerListMTA != null)
		{
			GameServer gameServer = ServerListMTA.FirstOrDefault((GameServer server) => server.id == id);
			if (gameServer != null)
			{
				if (_FAVORITE_MTA_SERVER == id)
				{
					FavoriteServerName.Dispatcher.Invoke(delegate
					{
						FavoriteServerMaxPlayerCount.Text = max_players;
						FavoriteServerPlayerCount.Text = players;
						FavoriteServerName.Text = name;
					});
				}
				if (MTABlock.Visibility == base.Visibility)
				{
					((TextBlock)FindElementWithTag("current_online", gameServer.element)).Text = players;
					((TextBlock)FindElementWithTag("max_players", gameServer.element)).Text = " / " + max_players;
					((TextBlock)FindElementWithTag("server_name", gameServer.element)).Text = name;
					((System.Windows.Controls.ProgressBar)FindElementWithTag("progress_bar", gameServer.element)).Value = Math.Round(double.Parse(players) / double.Parse(max_players) * 100.0);
				}
				gameServer.name = name;
				gameServer.players = players;
				gameServer.max_players = max_players;
				gameServer.address = address;
				gameServer.closed_text = closed_text;
				gameServer.image_url = new Uri(image_url);
				return;
			}
		}
		Grid grid = new Grid();
		Grid.SetRow(grid, ServerListMTA.Count);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Vertical;
		stackPanel.Name = "mta_" + id;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = (Style)FindResource("ServerButtonMTAStyle");
		if (ServerListMTA.Count > 0)
		{
			button.Margin = new Thickness(-30.0, 10.0, 0.0, 0.0);
		}
		else
		{
			button.Margin = new Thickness(-30.0, 0.0, 0.0, 0.0);
		}
		button.Click += SelectMTAServer;
		button.Tag = id;
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel2.VerticalAlignment = VerticalAlignment.Center;
		Uri uri = new Uri(image_url);
		System.Windows.Controls.Image image = new System.Windows.Controls.Image();
		image.Source = new BitmapImage(uri);
		image.Width = 30.0;
		image.Height = 30.0;
		image.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
		image.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Vertical;
		TextBlock textBlock = new TextBlock();
		textBlock.Text = name;
		textBlock.Tag = "server_name";
		textBlock.FontFamily = new System.Windows.Media.FontFamily("Montserrat SemiBold");
		textBlock.FontSize = 14.0;
		textBlock.Margin = new Thickness(10.0, 2.0, 0.0, 0.0);
		textBlock.Foreground = System.Windows.Media.Brushes.White;
		textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		StackPanel stackPanel4 = new StackPanel();
		stackPanel4.Orientation = System.Windows.Controls.Orientation.Horizontal;
		double num = double.Parse(players);
		double num2 = double.Parse(max_players);
		System.Windows.Controls.ProgressBar progressBar = new System.Windows.Controls.ProgressBar();
		progressBar.Tag = "progress_bar";
		progressBar.Width = 90.0;
		progressBar.Height = 2.0;
		progressBar.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		progressBar.VerticalAlignment = VerticalAlignment.Top;
		progressBar.Value = Math.Round(num / num2 * 100.0);
		progressBar.Margin = new Thickness(12.0, 7.0, 0.0, 0.0);
		progressBar.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, byte.MaxValue, byte.MaxValue));
		progressBar.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 177, 28));
		Ellipse ellipse = new Ellipse();
		ellipse.Width = 4.0;
		ellipse.Height = 4.0;
		ellipse.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(141, byte.MaxValue, 51));
		ellipse.Margin = new Thickness(10.0, 2.0, 0.0, 0.0);
		TextBlock textBlock2 = new TextBlock();
		textBlock2.Tag = "current_online";
		textBlock2.Text = players;
		textBlock2.FontFamily = new System.Windows.Media.FontFamily("Montserrat Medium");
		textBlock2.FontSize = 10.0;
		textBlock2.Margin = new Thickness(5.0, 2.0, 0.0, 0.0);
		textBlock2.Foreground = System.Windows.Media.Brushes.White;
		textBlock2.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		textBlock2.VerticalAlignment = VerticalAlignment.Top;
		TextBlock textBlock3 = new TextBlock();
		textBlock3.Tag = "max_players";
		textBlock3.Text = " / " + max_players;
		textBlock3.FontFamily = new System.Windows.Media.FontFamily("Montserrat Medium");
		textBlock3.FontSize = 10.0;
		textBlock3.Opacity = 0.5;
		textBlock3.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
		textBlock3.Foreground = System.Windows.Media.Brushes.White;
		textBlock3.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		textBlock3.VerticalAlignment = VerticalAlignment.Top;
		stackPanel4.Children.Add(progressBar);
		stackPanel4.Children.Add(ellipse);
		stackPanel4.Children.Add(textBlock2);
		stackPanel4.Children.Add(textBlock3);
		stackPanel3.Children.Add(textBlock);
		stackPanel3.Children.Add(stackPanel4);
		stackPanel2.Children.Add(image);
		stackPanel2.Children.Add(stackPanel3);
		button.Content = stackPanel2;
		stackPanel.Children.Add(button);
		grid.Children.Add(stackPanel);
		GridServers.Children.Add(grid);
		GridServers.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		GameServer item = new GameServer
		{
			id = id,
			image_url = uri,
			name = name,
			players = players,
			max_players = max_players,
			address = address,
			closed_text = closed_text,
			element = button
		};
		ServerListMTA.Add(item);
	}

	public void SyncUiGta5Online()
	{
		foreach (GameServer item in ServerListGTA5)
		{
			((TextBlock)FindElementWithTag("current_online", item.element)).Text = item.players;
			((TextBlock)FindElementWithTag("max_players", item.element)).Text = " / " + item.max_players;
			((TextBlock)FindElementWithTag("server_name", item.element)).Text = item.name;
			((System.Windows.Controls.ProgressBar)FindElementWithTag("progress_bar", item.element)).Value = Math.Round(double.Parse(item.players) / double.Parse(item.max_players) * 100.0);
		}
	}

	public void AddServerGTA5(string id, string name, string players, string max_players, string address, string closed_text)
	{
		if (ServerListGTA5 != null)
		{
			GameServer gameServer = ServerListGTA5.FirstOrDefault((GameServer server) => server.id == id);
			if (gameServer != null)
			{
				if (_FAVORITE_GTA5_SERVER == id)
				{
					FavoriteServerNameGta5.Dispatcher.Invoke(delegate
					{
						FavoriteServerMaxPlayerCountGta5.Text = max_players;
						FavoriteServerPlayerCountGta5.Text = players;
						FavoriteServerNameGta5.Text = name;
					});
				}
				if (Gta5Block.Visibility == base.Visibility)
				{
					((TextBlock)FindElementWithTag("current_online", gameServer.element)).Text = players;
					((TextBlock)FindElementWithTag("max_players", gameServer.element)).Text = " / " + max_players;
					((TextBlock)FindElementWithTag("server_name", gameServer.element)).Text = name;
					((System.Windows.Controls.ProgressBar)FindElementWithTag("progress_bar", gameServer.element)).Value = Math.Round(double.Parse(players) / double.Parse(max_players) * 100.0);
				}
				gameServer.name = name;
				gameServer.players = players;
				gameServer.max_players = max_players;
				gameServer.address = address;
				gameServer.closed_text = closed_text;
				return;
			}
		}
		Grid grid = new Grid();
		Grid.SetRow(grid, ServerListGTA5.Count);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Vertical;
		stackPanel.Name = "gta5_" + id;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = (Style)FindResource("ServerButtonGta5Style");
		if (ServerListGTA5.Count > 0)
		{
			button.Margin = new Thickness(-30.0, 10.0, 0.0, 0.0);
		}
		else
		{
			button.Margin = new Thickness(-30.0, 0.0, 0.0, 0.0);
		}
		button.Click += SelectGTA5Server;
		button.Tag = id;
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel2.VerticalAlignment = VerticalAlignment.Center;
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			StartPoint = new System.Windows.Point(0.0, 0.5),
			EndPoint = new System.Windows.Point(1.0, 0.5)
		};
		linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(41, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0));
		linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(13, 13, 220, 170), 1.0));
		Border border = new Border();
		border.Width = 21.0;
		border.Height = 21.0;
		border.CornerRadius = new CornerRadius(2.0);
		border.Background = linearGradientBrush;
		border.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
		border.VerticalAlignment = VerticalAlignment.Center;
		border.Child = new TextBlock
		{
			Text = id.TrimStart('s'),
			Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFBEF")),
			FontSize = 12.0,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};
		Border element = border;
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Vertical;
		StackPanel stackPanel4 = new StackPanel();
		stackPanel4.Orientation = System.Windows.Controls.Orientation.Horizontal;
		TextBlock textBlock = new TextBlock();
		textBlock.Text = name;
		textBlock.Tag = "server_name";
		textBlock.FontFamily = new System.Windows.Media.FontFamily("Montserrat SemiBold");
		textBlock.FontSize = 14.0;
		textBlock.Margin = new Thickness(10.0, 2.0, 0.0, 0.0);
		textBlock.Foreground = System.Windows.Media.Brushes.White;
		textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		StackPanel stackPanel5 = new StackPanel();
		stackPanel5.Orientation = System.Windows.Controls.Orientation.Horizontal;
		double num = double.Parse(players);
		double num2 = double.Parse(max_players);
		System.Windows.Controls.ProgressBar progressBar = new System.Windows.Controls.ProgressBar();
		progressBar.Tag = "progress_bar";
		progressBar.Width = 130.0;
		progressBar.Height = 2.0;
		progressBar.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		progressBar.VerticalAlignment = VerticalAlignment.Top;
		progressBar.Value = Math.Round(num / num2 * 100.0);
		progressBar.Margin = new Thickness(0.0, 11.0, 0.0, 0.0);
		progressBar.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, byte.MaxValue, byte.MaxValue));
		progressBar.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 177, 28));
		Ellipse ellipse = new Ellipse();
		ellipse.Width = 4.0;
		ellipse.Height = 4.0;
		ellipse.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(141, byte.MaxValue, 51));
		ellipse.Margin = new Thickness(10.0, 6.0, 0.0, 0.0);
		TextBlock textBlock2 = new TextBlock();
		textBlock2.Tag = "current_online";
		textBlock2.Text = players;
		textBlock2.FontFamily = new System.Windows.Media.FontFamily("Montserrat Medium");
		textBlock2.FontSize = 10.0;
		textBlock2.Margin = new Thickness(5.0, 6.0, 0.0, 0.0);
		textBlock2.Foreground = System.Windows.Media.Brushes.White;
		textBlock2.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		textBlock2.VerticalAlignment = VerticalAlignment.Top;
		TextBlock textBlock3 = new TextBlock();
		textBlock3.Tag = "max_players";
		textBlock3.Text = " / " + max_players;
		textBlock3.FontFamily = new System.Windows.Media.FontFamily("Montserrat Medium");
		textBlock3.FontSize = 10.0;
		textBlock3.Opacity = 0.5;
		textBlock3.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		textBlock3.Foreground = System.Windows.Media.Brushes.White;
		textBlock3.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		textBlock3.VerticalAlignment = VerticalAlignment.Top;
		stackPanel5.Children.Add(progressBar);
		stackPanel5.Children.Add(ellipse);
		stackPanel5.Children.Add(textBlock2);
		stackPanel5.Children.Add(textBlock3);
		stackPanel4.Children.Add(element);
		stackPanel4.Children.Add(textBlock);
		stackPanel3.Children.Add(stackPanel4);
		stackPanel3.Children.Add(stackPanel5);
		stackPanel2.Children.Add(stackPanel3);
		button.Content = stackPanel2;
		stackPanel.Children.Add(button);
		grid.Children.Add(stackPanel);
		GridServersGta5.Children.Add(grid);
		GridServersGta5.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		GameServer item = new GameServer
		{
			id = id,
			name = name,
			players = players,
			max_players = max_players,
			address = address,
			closed_text = closed_text,
			element = button
		};
		ServerListGTA5.Add(item);
	}

	private void TrayShowMainWindow()
	{
		Focus();
		Show();
		base.WindowState = WindowState.Normal;
		Activate();
	}

	private void TrayCloseApplication()
	{
		trayIcon.Dispose();
		System.Windows.Application.Current.Shutdown();
	}

	protected override void OnStateChanged(EventArgs e)
	{
		if (base.WindowState == WindowState.Minimized)
		{
		}
		base.OnStateChanged(e);
	}

	private void InitializeTrayIcon()
	{
		trayIcon = new NotifyIcon();
		trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		trayIcon.Visible = true;
		trayIcon.Text = "KOZAK RP [LAUNCHER]";
		trayIcon.DoubleClick += delegate
		{
			TrayShowMainWindow();
		};
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Items.Add("Відобразити", null, delegate
		{
			TrayShowMainWindow();
		});
		contextMenuStrip.Items.Add("Закрити", null, delegate
		{
			TrayCloseApplication();
		});
		trayIcon.ContextMenuStrip = contextMenuStrip;
	}

	private string BrowsFolderPath(string title)
	{
		folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = title;
		folderBrowserDialog.ShowNewFolderButton = false;
		if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			return folderBrowserDialog.SelectedPath;
		}
		return null;
	}

	public MainWindow()
	{
		// Use the directory where the launcher is installed instead of CommonApplicationData
		string assemblyLocation = Assembly.GetExecutingAssembly().Location;
		string assemblyDirectory = System.IO.Path.GetDirectoryName(assemblyLocation);
		_CLIENT_PATH_LAUNCHER = assemblyDirectory;
		bool flag = false;
		mutex_launcher = new Mutex(initiallyOwned: false, AppMutexName);
		if (!mutex_launcher.WaitOne(0))
		{
			System.Windows.MessageBox.Show(this, "Launcher is already running.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			System.Windows.Application.Current.Shutdown();
			flag = true;
		}
		if (flag)
		{
			return;
		}
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		RemoveLegacyUpdaterStartup();
		_SETTINGS = new SettingsManager(_CLIENT_PATH_LAUNCHER + _FILE_SETTINGS);
		bool flag2 = false;
		if (_SETTINGS.LauncherSettings["favoriteServerMTA"] == null)
		{
			_SETTINGS.LauncherSettings["favoriteServerMTA"] = "s6";
			flag2 = true;
		}
		if (_SETTINGS.LauncherSettings["favoriteServerGTA5"] == null)
		{
			_SETTINGS.LauncherSettings["favoriteServerGTA5"] = "s1";
			flag2 = true;
		}
		if (flag2)
		{
			_SETTINGS.SaveSettings();
		}
			InitializeComponent();
			ApplyBrandingOverrides();
			base.Loaded += delegate
			{
				ApplyBrandingOverrides();
			};
			InitializeTrayIcon();
			Activate();
		if (_SETTINGS.LauncherSettings["project"] == "mta")
		{
			MTABlock.Visibility = Visibility.Visible;
			ServersBlockMTA.Visibility = Visibility.Visible;
		}
		else if (_SETTINGS.LauncherSettings["project"] == "gta5")
		{
			Gta5Block.Visibility = Visibility.Visible;
			ServersBlockGta5.Visibility = Visibility.Visible;
			ChangeMainBlockStyle("gta5", isIniting: true);
		}
		ServerListMTA = new List<GameServer>();
		ServerListGTA5 = new List<GameServer>();
		LeftMenu.IsEnabled = false;
		MTABlock.IsEnabled = false;
		ServersBlockMTA.IsEnabled = false;
		Gta5Block.IsEnabled = false;
		ServersBlockGta5.IsEnabled = false;
		scrollTimer = new DispatcherTimer();
		scrollTimer.Interval = TimeSpan.FromMilliseconds(0.5);
		scrollTimer.Tick += ScrollTimer_Tick;
		NewsListMta = new List<News>();
		NewsListGta5 = new List<News>();
		newsMtaTimer.Interval = TimeSpan.FromSeconds(5.0);
		newsGta5Timer.Interval = TimeSpan.FromSeconds(5.0);
		newsMtaTimer.Tick += newsMtaTimer_Tick;
		newsGta5Timer.Tick += newsGta5Timer_Tick;
		DebugLog("Start launcher...");
		_CLIENT_ID = GetClientID();
		_CLIENT_OS = GetClientFriendlyOSName();
		DebugLog("CLIENT ID = \"" + _CLIENT_ID + "\"; CLIENT OS = \"" + _CLIENT_OS + "\".");
		LauncherIdMTA.Text = _CLIENT_ID;
		LauncherIdGTA5.Text = _CLIENT_ID;
		if (!Directory.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME))
		{
			Directory.CreateDirectory(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
			DebugLog("Create directory \"" + _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\".");
		}
		if (!Directory.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_DOWNLOAD))
		{
			Directory.CreateDirectory(_CLIENT_PATH_LAUNCHER + _LOCATION_DOWNLOAD);
			DebugLog("Create directory \"" + _CLIENT_PATH_LAUNCHER + _LOCATION_DOWNLOAD + "\".");
		}
		GameFolderPath.Text = _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME;
		using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\RAGE-MP"))
		{
			if (registryKey != null)
			{
				object value = registryKey.GetValue("rage_path");
				if (value != null && IsExistRageByPath(value.ToString()))
				{
					RageMpFolderPath.Text = value.ToString();
				}
			}
		}
		string gta5Path = GetGta5Path();
		if (gta5Path != null)
		{
			Gta5FolderPath.Text = gta5Path;
		}
		AutoStart.IsChecked = _SETTINGS.LauncherSettings["autostart"];
		AutoLogin.IsChecked = _SETTINGS.LauncherSettings["autologin"];
		if (_SETTINGS.LauncherSettings["graphic"] == "medium")
		{
			UpdateUiGraphic(GraphicLow, null);
		}
		else if (_SETTINGS.LauncherSettings["graphic"] == "high")
		{
			UpdateUiGraphic(GraphicHigh, null);
		}
		else
		{
			_SETTINGS.LauncherSettings["graphic"] = "high";
			UpdateUiGraphic(GraphicHigh, null);
			}
			_WEB_CLIENT.Headers.Add("user-agent", _WEB_CLIENT_USERAGENT);
			TryApplyRemoteReinstallRule();
			MTABlock.IsEnabled = true;
			ServersBlockMTA.IsEnabled = true;
			Gta5Block.IsEnabled = true;
			ServersBlockGta5.IsEnabled = true;
			LeftMenu.IsEnabled = true;
			InitializeOfflineContent();
			if (_AUTO_REINSTALL_AFTER_CHECK)
			{
				DebugLog("Auto reinstall marker changed, starting automatic client reinstall.");
				StartDownload(null, null);
			}
			_OnlineFetcherTimer.Interval = TimeSpan.FromSeconds(15.0);
		_OnlineFetcherTimer.Tick += FetchOnlineStats;
		_OnlineFetcherTimer.Start();
		FetchOnlineStats(null, null);
		trayIcon.ShowBalloonTip(3, "Лаунчер", "Лаунчер успішно запущено!", ToolTipIcon.Info);
	}

	private void ApplyBrandingOverrides()
	{
		BrandingPatch.Apply(this, _CLIENT_PATH_LAUNCHER);
	}

	private string ResolveAssetUri(string fileName, string fallbackPackUri)
	{
		try
		{
			string filePath = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER, fileName);
			if (File.Exists(filePath))
			{
				return new Uri(filePath).AbsoluteUri;
			}
		}
		catch
		{
		}
		return fallbackPackUri;
	}

	private async void FetchOnlineStats(object sender, EventArgs e)
	{
		try
		{
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(10.0);
				// Use website online.txt file for monitoring
				string onlineUrl = "https://kozakrp.qniks.me/online.txt";
				if (!int.TryParse((await client.GetStringAsync(onlineUrl)).Trim(), out var onlineCount))
				{
					DebugLog("Failed to parse online count from: " + onlineUrl, 1);
					return;
				}
				base.Dispatcher.Invoke(delegate
				{
					TotalOnline.Text = onlineCount.ToString();
					ProjectMTATotalOnline.Text = onlineCount.ToString() + " гравців";
					if (ServerListMTA != null)
					{
						foreach (GameServer serverListMTum in ServerListMTA)
						{
							serverListMTum.players = onlineCount.ToString();
						}
						SyncUiMtaOnline();
					}
					FavoriteServerPlayerCount.Text = onlineCount.ToString();
					// Write online count to file for monitoring
					try
					{
						string onlineLogPath = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER, "online_monitoring.log");
						string logEntry = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " - Online: " + onlineCount + Environment.NewLine;
						File.AppendAllText(onlineLogPath, logEntry);
					}
					catch (Exception logEx)
					{
						DebugLog("Failed to write online monitoring log: " + logEx.Message, 1);
					}
				});
				DebugLog("Successfully fetched online count: " + onlineCount);
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			DebugLog("Error fetching online stats: " + ex2.Message, 2);
		}
	}

	private void DisableActions(string project)
	{
		if (!(project == "mta"))
		{
			if (project == "gta5")
			{
				StartButtonGta5.Dispatcher.Invoke(delegate
				{
					StartButtonGta5.Style = (Style)FindResource("ButtonStartPlayDisabled");
					StartButtonGta5.IsEnabled = false;
					StartDownloadButtonGTA5.Style = (Style)FindResource("ButtonStartPlayDisabled");
					StartDownloadButtonGTA5.IsEnabled = false;
				});
			}
		}
		else
		{
			StartButton.Dispatcher.Invoke(delegate
			{
				StartButton.Style = (Style)FindResource("ButtonStartPlayDisabled");
				StartButton.IsEnabled = false;
				StartDownloadButton.Style = (Style)FindResource("ButtonStartPlayDisabled");
				StartDownloadButton.IsEnabled = false;
			});
		}
	}

	private void EnableActions(string project)
	{
		if (!(project == "mta"))
		{
			if (project == "gta5")
			{
				StartButtonGta5.Dispatcher.Invoke(delegate
				{
					StartButtonGta5.Style = (Style)FindResource("ButtonStartPlay");
					StartButtonGta5.IsEnabled = true;
					StartDownloadButtonGTA5.Style = (Style)FindResource("ButtonStartPlay");
					StartDownloadButtonGTA5.IsEnabled = true;
				});
			}
		}
		else
		{
			StartButton.Dispatcher.Invoke(delegate
			{
				StartButton.Style = (Style)FindResource("ButtonStartPlay");
				StartButton.IsEnabled = true;
				StartDownloadButton.Style = (Style)FindResource("ButtonStartPlay");
				StartDownloadButton.IsEnabled = true;
			});
		}
	}

	private bool InitializeGTA5Game()
	{
		GridAllReadyGTA5.Dispatcher.Invoke(() => GridAllReadyGTA5.Visibility = Visibility.Collapsed);
		GridNeedDownloadGTA5.Dispatcher.Invoke(() => GridNeedDownloadGTA5.Visibility = Visibility.Collapsed);
		GridDownloadGTA5.Dispatcher.Invoke(() => GridDownloadGTA5.Visibility = Visibility.Collapsed);
		ButtonPlayGTA5.Dispatcher.Invoke(() => ButtonPlayGTA5.Visibility = Visibility.Collapsed);
		GridnnNeedBuyGTA5.Dispatcher.Invoke(() => GridnnNeedBuyGTA5.Visibility = Visibility.Collapsed);
		ButtonBuyGTA5.Dispatcher.Invoke(() => ButtonBuyGTA5.Visibility = Visibility.Collapsed);
		DebugLog("Start checking gta5 game.");
		string rageMpPath = GetRageMpPath();
		string gta5Path = GetGta5Path();
		if (gta5Path == null)
		{
			GridnnNeedBuyGTA5.Dispatcher.Invoke(delegate
			{
				GridnnNeedBuyGTA5.Visibility = Visibility.Visible;
				ButtonBuyGTA5.Visibility = Visibility.Visible;
			});
			DebugLog("GTA5 in not installed.");
			return false;
		}
		if (rageMpPath == null)
		{
			GridNeedDownloadGTA5.Dispatcher.Invoke(delegate
			{
				GridNeedDownloadGTA5.Visibility = Visibility.Visible;
				ButtonStartDownloadGTA5.Visibility = Visibility.Visible;
				size_to_download = GTA5Project.archive_size;
				string text = "";
				double num = 0.0;
				if (size_to_download > 1073741824)
				{
					text = "ГБ";
					num = (double)size_to_download / 1024.0 / 1024.0 / 1024.0;
				}
				else if (size_to_download > 1048576)
				{
					text = "МБ";
					num = (double)size_to_download / 1024.0 / 1024.0;
				}
				else
				{
					text = "КБ";
					num = (double)size_to_download / 1024.0;
				}
				NeedDownloadGTA5Size.Text = $"Встановити RAGE ({num:0.00} {text})";
			});
			DebugLog("Rage MP in not installed.");
			return true;
		}
		DebugLog("Gta 5 and Rage MP are installed.");
		GridAllReadyGTA5.Dispatcher.Invoke(delegate
		{
			GridAllReadyGTA5.Visibility = Visibility.Visible;
			ButtonPlayGTA5.Visibility = Visibility.Visible;
		});
		try
		{
			if (File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE))
			{
				File.Delete(_CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE);
			}
		}
		catch
		{
		}
		return true;
	}

	private string GetRageMpPath()
	{
		string name = "Software\\RAGE-MP";
		using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
		{
			if (registryKey != null)
			{
				object value = registryKey.GetValue("rage_path");
				if (value != null && IsExistRageByPath(value.ToString()))
				{
					return value.ToString();
				}
			}
		}
		string path = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP);
		if (IsExistRageByPath(path))
		{
			using (RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey("Software\\RAGE-MP"))
			{
				registryKey2.SetValue("rage_path", path);
			}
			RageMpFolderPath.Dispatcher.Invoke(delegate
			{
				RageMpFolderPath.Text = path;
			});
			return path;
		}
		return null;
	}

	private string GetGta5Path()
	{
		string name = "Software\\RAGE-MP";
		using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
		{
			if (registryKey != null)
			{
				object value = registryKey.GetValue("game_v_path");
				if (value != null)
				{
					string text = value.ToString();
					if (IsExistGTA5ByPath(text))
					{
						return text;
					}
				}
			}
		}
		string[] array = new string[3] { "SOFTWARE\\WOW6432Node\\Rockstar Games\\Grand Theft Auto V", "SOFTWARE\\Rockstar Games\\Grand Theft Auto V", "SOFTWARE\\WOW6432Node\\Rockstar Games\\GTA V Enhanced" };
		string[] array2 = array;
		string[] array3 = array2;
		foreach (string name2 in array3)
		{
			using RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(name2);
			if (registryKey2 == null)
			{
				continue;
			}
			string[] array4 = new string[3] { "InstallFolder", "InstallFolderSteam", "InstallFolderEpic" };
			string[] array5 = array4;
			string[] array6 = array5;
			foreach (string name3 in array6)
			{
				object value2 = registryKey2.GetValue(name3);
				if (value2 != null)
				{
					string text2 = value2.ToString();
					if (IsExistGTA5ByPath(text2))
					{
						return text2;
					}
				}
			}
		}
		return null;
	}

	private bool IsExistGTA5ByPath(string path)
	{
		if (!string.IsNullOrEmpty(path) && (File.Exists(System.IO.Path.Combine(path, "GTA5_Enhanced.exe")) || File.Exists(System.IO.Path.Combine(path, "GTA5.exe")) || File.Exists(System.IO.Path.Combine(path, "GTAV.exe"))))
		{
			return true;
		}
		return false;
	}

	private bool IsExistRageByPath(string path)
	{
		if (!string.IsNullOrEmpty(path) && File.Exists(System.IO.Path.Combine(path, "updater.exe")) && File.Exists(System.IO.Path.Combine(path, "ragemp_v.exe")))
		{
			return true;
		}
		return false;
	}

	private void PlayGameGTA5(object sender, RoutedEventArgs e)
	{
		string ragempExe = "ragemp_v";
		string processName = "GTA5";
		string rageMpPath = GetRageMpPath();
		string gta5Path = GetGta5Path();
		DebugLog("Try start RageMp updater.");
		if (rageMpPath == null)
		{
			DebugLog("Cannt found RageMp path.", 2);
			SendErrorReport("start_ragemp", "Не вдалося відкрити Rage Mp. Шлях до RageMp не знайдено");
			InitializeGTA5Game();
			return;
		}
		if (gta5Path == null)
		{
			DebugLog("Cannt found GTA5 path.", 2);
			SendErrorReport("start_ragemp", "Шлях до GTA 5 не знайдено");
			InitializeGTA5Game();
			return;
		}
		try
		{
			GameServer selectedServer = ServerListGTA5.FirstOrDefault((GameServer server) => server.id == _FAVORITE_GTA5_SERVER);
			if (selectedServer == null)
			{
				return;
			}
			if (selectedServer.closed_text.Length > 0 && !_CLIENT_ADMIN)
			{
				GridAllReady.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, selectedServer.closed_text, "Технічні роботи!"));
				return;
			}
			Process[] processesByName = Process.GetProcessesByName(ragempExe);
			Process[] array = processesByName;
			foreach (Process process in array)
			{
				process.Kill();
			}
			Process[] processesByName2 = Process.GetProcessesByName(processName);
			Process[] array2 = processesByName2;
			foreach (Process process2 in array2)
			{
				process2.Kill();
			}
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\RAGE-MP"))
			{
				if (selectedServer.address != "none")
				{
					registryKey.SetValue("launch.ip", selectedServer.address);
					registryKey.SetValue("launch.port", "22005");
					registryKey.SetValue("launch2.ip", selectedServer.address);
					registryKey.SetValue("launch2.port", "22005");
				}
				registryKey.SetValue("game_v_path", gta5Path);
				registryKey.SetValue("rage_path", rageMpPath);
			}
			Process process3 = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = System.IO.Path.Combine(rageMpPath, "updater.exe"),
					UseShellExecute = true,
					Verb = "runas",
					WorkingDirectory = rageMpPath
				},
				EnableRaisingEvents = true
			};
			process3.Exited += delegate
			{
				DebugLog("Rage Mp updater has been closed.");
				Process rageProcess = Process.GetProcessesByName(ragempExe).FirstOrDefault();
				if (rageProcess == null)
				{
					GTA5Project.is_started = false;
					GridAllReadyGTA5.Dispatcher.Invoke(delegate
					{
						EnableActions("gta5");
						AllReadyStatusGTA5.Text = "Гра готова до запуску!";
						AllReadyAdditionalGTA5.Text = "Розпочніть свою гру прямо зараз";
					});
					DebugLog("Rage Mp was not launched.");
				}
				else
				{
					GridAllReadyGTA5.Dispatcher.Invoke(delegate
					{
						AllReadyStatusGTA5.Text = "Гру успішно запущено!";
						AllReadyAdditionalGTA5.Text = "Зустрінемось у світі UKRAINE GTA 5";
					});
					Task.Run(delegate
					{
						rageProcess.WaitForExit();
						if (Process.GetProcessesByName("EACLauncher").Length != 0)
						{
							GTA5Project.last_exit_proccess = DateTime.Now;
						}
						else
						{
							GTA5Project.is_started = false;
							GridAllReady.Dispatcher.Invoke(delegate
							{
								EnableActions("gta5");
								AllReadyStatusGTA5.Text = "Гра готова до запуску!";
								AllReadyAdditionalGTA5.Text = "Розпочніть свою гру прямо зараз";
								DebugLog("Rage Mp has been closed.");
							});
						}
					});
				}
			};
			GTA5Project.is_started = true;
			DisableActions("gta5");
			GridAllReadyGTA5.Dispatcher.Invoke(delegate
			{
				AllReadyStatusGTA5.Text = "Запускаємо гру...";
				AllReadyAdditionalGTA5.Text = "Зустрінемось у світі UKRAINE GTA 5";
			});
			GTA5Project.last_start_proccess = DateTime.Now;
			process3.Start();
		}
		catch (Exception ex)
		{
			DebugLog("Cannt start Rage MP. Error: " + ex.Message, 2);
			GridAllReady.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Помилка запуску RAGE Multiplayer.\n\nЯкщо ця помилка не зникає - зверніться до підтримки:\nTELEGRAM: @ukrainegta5bot", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
			SendErrorReport("start_ragemp", "Не вдалося відкрити Rage Mp. Помилка: " + ex.Message);
		}
	}

	private void StartDownloadGTA5(object sender, RoutedEventArgs e)
	{
		if (_INIT_DOWNLOAD)
		{
			return;
		}
		Process[] processesByName = Process.GetProcessesByName("ragemp_v");
		if (processesByName.Length != 0)
		{
			GridAllReady.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Ви не можете почати завантаження доки у вас відкрита гра.\n\nБудь ласка, закрийте гру.\n\nЯкщо ця помилка не зникає - зверніться до підтримки:\nTELEGRAM: @ukrainegta5bot", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
		}
		else
		{
			Task.Run(delegate
			{
				DownloadGTA5();
			});
		}
	}

	private void DownloadGTA5()
	{
		string fileName = _CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE;
		if (GTA5Project.archive_path == null || GTA5Project.archive_size <= 0)
		{
			DebugLog("Error download Rage MP, rage mp path or size is empty.", 2);
			SendErrorReport("download_ragemp", "Не вдалося отримати archive_path чи archive_size");
			return;
		}
		_INIT_DOWNLOAD = true;
		DisableActions("mta");
		GridNeedDownloadGTA5.Dispatcher.Invoke(delegate
		{
			GridNeedDownloadGTA5.Visibility = Visibility.Collapsed;
			ButtonStartDownloadGTA5.Visibility = Visibility.Collapsed;
			GridDownloadGTA5.Visibility = Visibility.Visible;
		});
		try
		{
			size_to_download = GTA5Project.archive_size;
			Uri address = new Uri(GTA5Project.archive_path);
			_WEB_CLIENT.CancelAsync();
			_WEB_CLIENT.Dispose();
			_WEB_CLIENT.Headers.Set("user-agent", _WEB_CLIENT_USERAGENT);
			_WEB_CLIENT.DownloadProgressChanged -= DownloadGTA5ProgressChanged;
			_WEB_CLIENT.DownloadProgressChanged += DownloadGTA5ProgressChanged;
			_WEB_CLIENT.DownloadFileCompleted += DownloadGTA5FileCompleted;
			_WEB_CLIENT.DownloadFileAsync(address, fileName);
			if (!_STOP_WATCH.IsRunning)
			{
				_STOP_WATCH.Start();
			}
		}
		catch (Exception)
		{
		}
	}

	private void DownloadGTA5ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
	{
		try
		{
			DownloadGTA5Status.Dispatcher.Invoke(delegate
			{
				if (DownloadGTA5Status.Text != "Завантаження RAGE Multiplayer... [" + (Convert.ToDouble(e.BytesReceived + last_bytes_received) / 1024.0 / 1024.0 / _STOP_WATCH.Elapsed.TotalSeconds).ToString("0.00") + " MB/s]")
				{
					DownloadGTA5Status.Text = "Завантаження RAGE Multiplayer... [" + (Convert.ToDouble(e.BytesReceived + last_bytes_received) / 1024.0 / 1024.0 / _STOP_WATCH.Elapsed.TotalSeconds).ToString("0.00") + " MB/s]";
				}
			});
			int percent = e.ProgressPercentage;
			if (_DOWNLOAD_TYPE)
			{
				percent = (int)((double)(e.BytesReceived + last_bytes_received) / (double)(size_to_download + last_bytes_received) * 100.0);
				if (percent > 100)
				{
					percent = 100;
				}
			}
			DownloadGTA5ProgressValue.Dispatcher.Invoke(delegate
			{
				if (DownloadGTA5ProgressValue.Text != percent + "%")
				{
					DownloadGTA5ProgressValue.Text = percent + "%";
					DownloadGTA5ProgressBar.Value = percent;
				}
			});
		}
		catch
		{
		}
	}

	private void DownloadGTA5FileCompleted(object sender, AsyncCompletedEventArgs e)
	{
		if (!_INIT_DOWNLOAD)
		{
			return;
		}
		if (e.Error != null)
		{
			_INIT_DOWNLOAD = false;
			if (_STOP_WATCH.IsRunning)
			{
				_STOP_WATCH.Stop();
				_STOP_WATCH.Reset();
			}
			size_to_download = 0L;
			last_bytes_received = 0L;
			DebugLog("Error on download file. Details: " + e.Error.ToString(), 2);
			if (e.Error.ToString().Contains("enough space") || e.Error.ToString().Contains("Недостаточно места") || e.Error.ToString().Contains("Недостатньо місця"))
			{
				GridDownloadGTA5.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі завантажити файл з нашого серверу.\n\nНедостатньо місця на диску з лаунчером(необхідно ~12ГБ), будь ласка, звільніть місце на диску та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
				SendErrorReport("downloading", "Недостатньо місця у клієнта (gta5), показуємо повідомлення з проханням звільнити місце");
			}
			EnableActions("mta");
			InitializeGTA5Game();
		}
		else
		{
			count_error_in_download = 0;
			if (_STOP_WATCH.IsRunning)
			{
				_STOP_WATCH.Stop();
				_STOP_WATCH.Reset();
			}
			DebugLog("Finish download Rage MP. Try to install.");
			trayIcon.ShowBalloonTip(10, "", "Завантаження завершено, починаємо розпакування...", ToolTipIcon.Info);
			Task.Run(delegate
			{
				StartUnpackingGTA5();
			});
		}
	}

	private void StartUnpackingGTA5()
	{
		_INIT_DOWNLOAD = false;
		try
		{
			_WEB_CLIENT.Dispose();
		}
		catch
		{
		}
		GridDownloadGTA5.Dispatcher.Invoke(delegate
		{
			DownloadGTA5Status.Text = "Триває розпакування...";
		});
		try
		{
			if (Directory.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP))
			{
				Directory.GetFiles(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP, "*", SearchOption.AllDirectories).ToList().ForEach(delegate(string file)
				{
					FileInfo fileInfo = new FileInfo(file);
					if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
					{
						fileInfo.Attributes = FileAttributes.Normal;
					}
				});
				DirectoryInfo directoryInfo = new DirectoryInfo(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP);
				directoryInfo.Attributes = FileAttributes.Normal;
			}
		}
		catch
		{
			_INIT_UNPACKER = false;
			EnableActions("mta");
			GridDownloadGTA5.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі розпакування архіву з грою! Спробуйте виконати наступне:\n\n1. Закрийте лаунчер.\n2. Видаліть папку 'RAGEMP', яка знаходиться в папці лаунчера.\n3. Відкрийте лаунчер знову та встановіть гру.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
		}
		if (File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE))
		{
			try
			{
				DebugLog("Start unpacking archive with rage mp...");
				_WEB_CLIENT.Dispose();
				_INIT_UNPACKER = true;
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.UseShellExecute = false;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.CreateNoWindow = true;
				processStartInfo.FileName = _CLIENT_PATH_LAUNCHER + "\\7z\\7za.exe";
				processStartInfo.Arguments = "x \"" + _CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE + "\" -y -o\"" + _CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP + "\" -bsp1";
				Process process = Process.Start(processStartInfo);
				while (!process.StandardOutput.EndOfStream)
				{
					string text = process.StandardOutput.ReadLine();
					if (!string.IsNullOrWhiteSpace(text) && text.Contains("%"))
					{
						string[] array = text.Trim().Split('%');
						if (array.Length != 0 && int.TryParse(array[0].Trim(), out var percent))
						{
							DownloadGTA5ProgressValue.Dispatcher.Invoke(delegate
							{
								if (DownloadGTA5ProgressValue.Text != percent + "%")
								{
									DownloadGTA5ProgressValue.Text = percent + "%";
								}
							});
							DownloadGTA5ProgressBar.Dispatcher.Invoke(delegate
							{
								if (DownloadGTA5ProgressBar.Value != (double)percent)
								{
									DownloadGTA5ProgressBar.Value = percent;
								}
							});
						}
					}
				}
				process.WaitForExit();
				process.Close();
				DebugLog("Finish unpacking rage mp archive.");
				_INIT_UNPACKER = false;
				EnableActions("mta");
				InitializeGTA5Game();
				trayIcon.ShowBalloonTip(10, "", "Розпакування завершено, готові до запуску гри!", ToolTipIcon.Info);
				return;
			}
			catch (Exception ex)
			{
				SendErrorReport("start_unpacking_ragemp", ex.Message);
				GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі розпакування архіву з RAGE Multiplayer! Перевстановіть лаунчер та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
				DebugLog("Cannot unpacking rage mp archive. Details: " + ex.Message, 2);
				DebugLog("Exit launcher...\n\n");
				_INIT_UNPACKER = false;
				EnableActions("mta");
				Environment.Exit(0);
				return;
			}
		}
		SendErrorReport("start_unpacking", "Не знайдено архів з Rage MP для розпакування");
		GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі розпакування архіву з RAGE Multiplayer! Перевстановіть лаунчер та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
		DebugLog("Cannot unpacking archive. File not found.", 2);
		DebugLog("Exit launcher...\n\n");
		_INIT_UNPACKER = false;
		EnableActions("mta");
	}

	private bool InitializeMTAGame(bool fast_check = true)
	{
		NormalizeMtaInstallLayout();
		bool flag = false;
		GridAllReady.Dispatcher.Invoke(() => GridAllReady.Visibility = Visibility.Collapsed);
		GridNeedDownload.Dispatcher.Invoke(() => GridNeedDownload.Visibility = Visibility.Collapsed);
		GridDownload.Dispatcher.Invoke(() => GridDownload.Visibility = Visibility.Collapsed);
		GridCheckFiles.Dispatcher.Invoke(() => GridCheckFiles.Visibility = Visibility.Collapsed);
		ButtonPlay.Dispatcher.Invoke(() => ButtonPlay.Visibility = Visibility.Collapsed);
		DebugLog("Start checking all mta game files.");
		GameFiles[] files_to_check = JsonConvert.DeserializeObject<GameFiles[]>(_FILES_PATH_AND_HASHES);
		_FILES_TO_DOWNLOAD.Clear();
		size_to_download = 0L;
		GridCheckFiles.Dispatcher.Invoke(() => GridCheckFiles.Visibility = Visibility.Visible);
		GameFiles gameFiles = files_to_check.FirstOrDefault((GameFiles f) => f.path.EndsWith("gta_low.dat"));
		GameFiles gameFiles2 = files_to_check.FirstOrDefault((GameFiles f) => f.path.EndsWith("gta_hight.dat"));
		int i = 0;
		GameFiles[] array = files_to_check;
		GameFiles[] array2 = array;
		foreach (GameFiles gameFiles3 in array2)
		{
			if (_CLIENT_ADMIN)
			{
				continue;
			}
			i++;
			CheckFilesProgressValue.Dispatcher.Invoke(delegate
			{
				double num2 = (double)i / (double)files_to_check.Length * 100.0;
				CheckFilesProgressValue.Text = $"{(int)num2}%";
				CheckFilesProgressBar.Value = (int)num2;
			});
			string text = _CLIENT_PATH_LAUNCHER + "\\" + gameFiles3.path.Replace("/", "\\");
			if (File.Exists(text))
			{
				if (fast_check)
				{
					continue;
				}
				if (gameFiles3.can_skip == "true")
				{
					FileInfo fileInfo = new FileInfo(text);
					if (fileInfo.Length == gameFiles3.size)
					{
						continue;
					}
				}
				string text2 = CalculateMD5(text);
				if (text2 == gameFiles3.hash)
				{
					continue;
				}
				try
				{
					if (gameFiles3.path.EndsWith("gta.dat") && ((gameFiles != null && text2 == gameFiles.hash) || (gameFiles2 != null && text2 == gameFiles2.hash)))
					{
						continue;
					}
					FileInfo fileInfo2 = new FileInfo(text);
					if (fileInfo2.Attributes.HasFlag(FileAttributes.ReadOnly))
					{
						fileInfo2.Attributes = FileAttributes.Normal;
					}
					File.Delete(text);
					goto IL_031a;
				}
				catch
				{
					goto IL_031a;
				}
			}
			_FILES_TO_DOWNLOAD.Add(gameFiles3);
			size_to_download += gameFiles3.size;
			continue;
			IL_031a:
			_FILES_TO_DOWNLOAD.Add(gameFiles3);
			size_to_download += gameFiles3.size;
		}
		if (size_to_download < MTAProject.archive_size || _FORCE_DOWNLOAD)
		{
			_DOWNLOAD_TYPE = true;
		}
		else
		{
			_DOWNLOAD_TYPE = false;
			size_to_download = MTAProject.archive_size;
		}
		if (!IsMtaInstalled() && !string.IsNullOrWhiteSpace(_GAME_URL_DOWNLOAD))
		{
			_DOWNLOAD_TYPE = false;
			size_to_download = MTAProject.archive_size;
		}
		if (size_to_download > 0)
		{
			flag = true;
			killProccess("CEFLauncher");
			if (_REINSTALL_MTA && ((Is_1_5_9_MtaVersion() && MTAProject.client_version == "1.6") || (!Is_1_5_9_MtaVersion() && MTAProject.client_version == "1.5.9")))
			{
				_DOWNLOAD_TYPE = false;
				size_to_download = MTAProject.archive_size;
				DeleteMtaGameFiels();
			}
		}
		if (flag)
		{
			GridNeedDownload.Dispatcher.Invoke(delegate
			{
				GridNeedDownload.Visibility = Visibility.Visible;
				ButtonStartDownload.Visibility = Visibility.Visible;
				string text3 = "";
				double num2 = 0.0;
				if (size_to_download > 1073741824)
				{
					text3 = "ГБ";
					num2 = (double)size_to_download / 1024.0 / 1024.0 / 1024.0;
				}
				else if (size_to_download > 1048576)
				{
					text3 = "МБ";
					num2 = (double)size_to_download / 1024.0 / 1024.0;
				}
				else
				{
					text3 = "КБ";
					num2 = (double)size_to_download / 1024.0;
				}
				NeedDownloadSize.Text = $"Необхідне оновлення ({num2:0.00} {text3})";
				DebugLog(string.Format("Files to upgrade - {0}; Size to download (MB) - {1}; Download type - {2};", _FILES_TO_DOWNLOAD.Count.ToString(), (float)(size_to_download / 1024 / 1024), _DOWNLOAD_TYPE ? "by one" : "archive"));
			});
		}
		else
		{
			GridAllReady.Dispatcher.Invoke(delegate
			{
				GridAllReady.Visibility = Visibility.Visible;
				ButtonPlay.Visibility = Visibility.Visible;
			});
			try
			{
				if (File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE))
				{
					File.Delete(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE);
				}
			}
			catch
			{
			}
		}
		DebugLog("Finish checking all game files.");
		return flag;
	}

	private void NormalizeMtaInstallLayout()
	{
		try
		{
			string gameRootPath = _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME;
			string nestedGamePath = System.IO.Path.Combine(gameRootPath, "game");
			string rootLauncherExe = System.IO.Path.Combine(gameRootPath, "KOZAK RP.exe");
			string nestedLauncherExe = System.IO.Path.Combine(nestedGamePath, "KOZAK RP.exe");
			if (File.Exists(rootLauncherExe) || !Directory.Exists(nestedGamePath) || !File.Exists(nestedLauncherExe))
			{
				return;
			}
			string[] rootEntries = Directory.GetFileSystemEntries(gameRootPath);
			if (rootEntries.Length != 1 || !string.Equals(System.IO.Path.GetFileName(rootEntries[0]), "game", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			foreach (string file in Directory.GetFiles(nestedGamePath))
			{
				string fileName = System.IO.Path.GetFileName(file);
				string destination = System.IO.Path.Combine(gameRootPath, fileName);
				if (File.Exists(destination))
				{
					FileInfo fileInfo = new FileInfo(destination);
					if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
					{
						fileInfo.Attributes = FileAttributes.Normal;
					}
					File.Delete(destination);
				}
				File.Move(file, destination);
			}
			foreach (string directory in Directory.GetDirectories(nestedGamePath))
			{
				string directoryName = System.IO.Path.GetFileName(directory);
				string destination2 = System.IO.Path.Combine(gameRootPath, directoryName);
				if (Directory.Exists(destination2))
				{
					Directory.Delete(destination2, recursive: true);
				}
				Directory.Move(directory, destination2);
			}
			Directory.Delete(nestedGamePath, recursive: true);
			DebugLog("Normalized extracted MTA layout from nested game directory.");
		}
		catch (Exception ex)
		{
			DebugLog("Cannot normalize extracted MTA layout: " + ex.Message, 2);
		}
	}

	private void PlayGameMTA(object sender, RoutedEventArgs e)
	{
		Task.Run(delegate
		{
			if (!InitializeMTAGame(fast_check: false))
			{
				string text = "";
				DebugLog("Try to get MTA serial number, pre start game.");
				try
				{
					if (MTAProject.client_version == "1.5.9")
					{
						using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\UKRAINEGTA: GLAB3\\1.5\\Settings\\general"))
						{
							if (registryKey != null && registryKey.GetValue("serial") != null)
							{
								text = registryKey.GetValue("serial").ToString();
							}
						}
						using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\WOW6432Node\\UKRAINEGTA: GLAB3\\1.5\\Settings\\general"))
						{
							if (registryKey2 != null && registryKey2.GetValue("serial") != null)
							{
								text = registryKey2.GetValue("serial").ToString();
							}
						}
						if (IsRunningAsAdmin())
						{
							using (RegistryKey registryKey3 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\UKRAINEGTA: GLAB3\\1.5\\Settings\\general"))
							{
								if (registryKey3 != null && registryKey3.GetValue("serial") != null)
								{
									text = registryKey3.GetValue("serial").ToString();
								}
							}
							using RegistryKey registryKey4 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\UKRAINEGTA: GLAB3\\1.5\\Settings\\general");
							if (registryKey4 != null && registryKey4.GetValue("serial") != null)
							{
								text = registryKey4.GetValue("serial").ToString();
							}
						}
					}
					else if (MTAProject.client_version == "1.6")
					{
						try
						{
							killProccess("CEFLauncher");
						}
						catch
						{
						}
						using (RegistryKey registryKey5 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\UKRAINEGTA: GLAB3\\1.6\\Settings\\general"))
						{
							if (registryKey5 != null && registryKey5.GetValue("serial") != null)
							{
								text = registryKey5.GetValue("serial").ToString();
							}
						}
						using (RegistryKey registryKey6 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\WOW6432Node\\UKRAINEGTA: GLAB3\\1.6\\Settings\\general"))
						{
							if (registryKey6 != null && registryKey6.GetValue("serial") != null)
							{
								text = registryKey6.GetValue("serial").ToString();
							}
						}
						if (IsRunningAsAdmin())
						{
							using (RegistryKey registryKey7 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\UKRAINEGTA: GLAB3\\1.6\\Settings\\general"))
							{
								if (registryKey7 != null && registryKey7.GetValue("serial") != null)
								{
									text = registryKey7.GetValue("serial").ToString();
								}
							}
							using RegistryKey registryKey8 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\UKRAINEGTA: GLAB3\\1.6\\Settings\\general");
							if (registryKey8 != null && registryKey8.GetValue("serial") != null)
							{
								text = registryKey8.GetValue("serial").ToString();
							}
						}
					}
				}
				catch
				{
				}
				if (text.Length == 32)
				{
					DebugLog("MTA Serial number received. Current serial: \"" + text + "\".");
				}
				else
				{
					DebugLog("Cannot find MTA Serial number. Maybe it's first run MTA.", 1);
				}
				GameServer gameServer = ServerListMTA.FirstOrDefault((GameServer server) => server.id == _FAVORITE_MTA_SERVER);
				if (gameServer != null)
				{
					MTAProject.is_started = true;
					DisableActions("mta");
					GridAllReady.Dispatcher.Invoke(delegate
					{
						AllReadyStatus.Text = "Запускаємо гру...";
						AllReadyAdditional.Text = "Зустрінемось у світі KOZAK RP";
					});
					try
					{
						if (MTAProject.client_version == "1.5.9")
						{
							SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
							SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch");
							SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
							SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch");
							if (IsRunningAsAdmin())
							{
								SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, RegistryHive.LocalMachine);
								SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch", RegistryHive.LocalMachine);
								SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, RegistryHive.LocalMachine);
								SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch", RegistryHive.LocalMachine);
							}
						}
						else if (MTAProject.client_version == "1.6")
						{
							SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common\\1.6\\Settings\\diagnostics", "preloading-upgrades-lowest-unsafe", "1025");
							SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common\\1.6\\Settings\\watchdog", "L3", "1");
							SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin");
							SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin\\mods\\deathmatch");
							SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin");
							SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin\\mods\\deathmatch");
							if (IsRunningAsAdmin())
							{
								SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\1.6\\Settings\\diagnostics", "preloading-upgrades-lowest-unsafe", "1025", RegistryHive.LocalMachine);
								SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\1.6\\Settings\\watchdog", "L3", "1", RegistryHive.LocalMachine);
								SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin", RegistryHive.LocalMachine);
								SetRegistryValue("Software\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin\\mods\\deathmatch", RegistryHive.LocalMachine);
								SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "GTA:SA Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin", RegistryHive.LocalMachine);
								SetRegistryValue("Software\\WOW6432Node\\UKRAINEGTA: GLAB3\\Common", "File Cache Path", _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\bin\\mods\\deathmatch", RegistryHive.LocalMachine);
							}
							ApplyGraphics(is_run: true);
						}
					}
					catch (Exception ex)
					{
						GridAllReady.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Нам не вдалося здійснити первинні налаштування МТА.\n\nЯкщо під час запуску гра запросить обрати шлях, вкажіть шлях до папки 'game', яка знаходиться в папці з лаунчером:\n\n" + _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
						SendErrorReport("set_mta_path", ex.Message);
					}
					try
					{
						if (File.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mta\\config\\coreconfig.xml"))
						{
							XmlDocument xmlDocument = new XmlDocument();
							xmlDocument.Load(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mta\\config\\coreconfig.xml");
							XmlNode xmlNode = xmlDocument.SelectSingleNode("/mainconfig/settings/nick");
							Random random = new Random();
							xmlNode.InnerText = random.Next(10101010, 99999999).ToString();
							xmlNode = xmlDocument.SelectSingleNode("/mainconfig/settings/qc_host");
							if (xmlNode != null)
							{
								xmlNode.InnerText = "s1.ukraine-gta.com.ua";
							}
							xmlDocument.Save(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mta\\config\\coreconfig.xml");
						}
					}
					catch
					{
					}
					AutoLogin.Dispatcher.Invoke(delegate
					{
						if (AutoLogin.IsChecked == true)
						{
							if (!File.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch\\resources\\ugta_loginpanel\\autologin.launcher"))
							{
								CreateDirectoriesIfNotExist(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch\\resources\\ugta_loginpanel");
								try
								{
									using StreamWriter streamWriter = File.CreateText(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch\\resources\\ugta_loginpanel\\autologin.launcher");
									streamWriter.WriteLine("");
								}
								catch
								{
								}
							}
						}
						else if (File.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch\\resources\\ugta_loginpanel\\autologin.launcher"))
						{
							try
							{
								FileInfo fileInfo = new FileInfo(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch\\resources\\ugta_loginpanel\\autologin.launcher");
								if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
								{
									fileInfo.Attributes = FileAttributes.Normal;
								}
								File.Delete(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\mods\\deathmatch\\resources\\ugta_loginpanel\\autologin.launcher");
							}
							catch
							{
							}
						}
					});
					DebugLog("All prelaunch work has been successfully completed. Start the game...");
					try
					{
						Process.Start(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\\KOZAK RP.exe", "-c " + gameServer.address);
					}
					catch (Exception ex2)
					{
						GridAllReady.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка під час спроби запустити гру!\n\nБудь ласка, перевстановіть гру(через кнопку налаштувань справа-зверху лаунчеру) і спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
						SendErrorReport("start_game", ex2.Message);
						DebugLog("Error while start the game. Details: " + ex2.Message, 2);
					}
				}
			}
		});
	}

	private void StartDownload(object sender, RoutedEventArgs e)
	{
		if (!_INIT_DOWNLOAD)
		{
			if (!_DOWNLOAD_TYPE || _FILES_TO_DOWNLOAD.Count > 0)
			{
			}
			Task.Run(delegate
			{
				StartDownloadFiles();
			});
		}
	}

	private void StartDownloadFiles()
	{
		_INIT_DOWNLOAD = true;
		DisableActions("gta5");
		try
		{
			DriveInfo driveInfo = new DriveInfo(System.IO.Path.GetPathRoot(_CLIENT_PATH_LAUNCHER));
			decimal num = driveInfo.AvailableFreeSpace;
			if ((_DOWNLOAD_TYPE && num < (decimal)size_to_download) || (!_DOWNLOAD_TYPE && num < 12884901888m))
			{
				GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Недостатньо місця на диску з лаунчером, будь ласка, звільніть місце та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
				SendErrorReport("downloading", "Недостатньо місця у клієнта, показуємо повідомлення з проханням звільнити місце");
				_INIT_DOWNLOAD = false;
				EnableActions("gta5");
				DebugLog("Cannot start download. Not enough hard disk space.", 1);
				return;
			}
		}
		catch
		{
		}
		if (!_DOWNLOAD_TYPE && File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE))
		{
			try
			{
				File.Delete(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE);
			}
			catch
			{
				_INIT_DOWNLOAD = false;
				GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Ми не можемо видалити попередні файли гри для того щоб виконати нове завантаження.\n\nУ разі якщо помилка не зникає - перевстановіть лаунчер через офіційний сайт.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
				DebugLog("Error while delete old files before start download.", 2);
				EnableActions("gta5");
				return;
			}
		}
		GridNeedDownload.Dispatcher.Invoke(delegate
		{
			GridNeedDownload.Visibility = Visibility.Collapsed;
			ButtonStartDownload.Visibility = Visibility.Collapsed;
			GridDownload.Visibility = Visibility.Visible;
		});
		GridDownload.Dispatcher.Invoke(delegate
		{
			DownloadStatus.Text = "Завантаження файлів гри...";
		});
		if (!EnsureSevenZipRuntimeFiles())
		{
			DebugLog("Failed to prepare 7z runtime files before download.", 2);
		}
		string text;
		Uri address;
		if (_DOWNLOAD_TYPE && _FILES_TO_DOWNLOAD.Count > 0)
		{
			GameFiles gameFiles = _FILES_TO_DOWNLOAD[0];
			text = _CLIENT_PATH_LAUNCHER + "\\" + gameFiles.path.Replace("/", "\\");
			CreateDirectoriesIfNotExist(System.IO.Path.GetDirectoryName(text));
			address = new Uri(_GAME_FILES_DOWNLOAD + gameFiles.path);
			DebugLog(string.Format("Start download file by one; Remaining files: {0}; File: \"{1}\".", (_FILES_TO_DOWNLOAD.Count - 1).ToString(), gameFiles.path.Replace("game/", "")));
		}
		else
		{
			text = _CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE;
			// Use multiple fallback URLs for better reliability across different PCs
			string[] fallbackUrls = new string[]
			{
				_GAME_URL_DOWNLOAD,
				_OFFLINE_MTA_ARCHIVE_URL,
				"https://kozak-rp.cdn.express/~/share/09d03b7b810f/game.zip",
				"https://cdn.kozak-rp.com/game.zip"
			};

			Uri downloadUri = null;
			foreach (string url in fallbackUrls)
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(url))
					{
						downloadUri = new Uri(url);
						DebugLog("Trying download URL: " + url);
						break;
					}
				}
				catch
				{
					continue;
				}
			}

			if (downloadUri == null)
			{
				downloadUri = new Uri(_GAME_URL_DOWNLOAD);
			}

			address = downloadUri;
			DebugLog("Start download file by archive; URL: " + address.ToString());
		}
		try
		{
			_WEB_CLIENT.CancelAsync();
			_WEB_CLIENT.Dispose();
			_WEB_CLIENT = new WebClient();
			_WEB_CLIENT.Headers.Set("user-agent", _WEB_CLIENT_USERAGENT);
			_WEB_CLIENT.DownloadProgressChanged -= DownloadProgressChanged;
			_WEB_CLIENT.DownloadFileCompleted -= DownloadFileCompleted;
			_WEB_CLIENT.DownloadProgressChanged += DownloadProgressChanged;
			_WEB_CLIENT.DownloadFileCompleted += DownloadFileCompleted;
			_WEB_CLIENT.DownloadFileAsync(address, text);
			if (!_STOP_WATCH.IsRunning)
			{
				_STOP_WATCH.Start();
			}
		}
		catch (Exception ex)
		{
			_INIT_DOWNLOAD = false;
			_WEB_CLIENT.CancelAsync();
			_WEB_CLIENT.Dispose();
			_WEB_CLIENT = new WebClient();
			_WEB_CLIENT.Headers.Set("user-agent", _WEB_CLIENT_USERAGENT);
			InitializeMTAGame(fast_check: false);
			SendErrorReport("download_pre_start", ex.Message);
			DebugLog("Error on pre start download. Details: " + ex.Message, 2);
		}
	}

	private void StartUnpacking()
	{
		_INIT_DOWNLOAD = false;
		try
		{
			_WEB_CLIENT.Dispose();
		}
		catch
		{
		}
		GridDownload.Dispatcher.Invoke(delegate
		{
			DownloadStatus.Text = "Триває розпакування...";
		});
		try
		{
			if (Directory.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME))
			{
				Directory.GetFiles(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, "*", SearchOption.AllDirectories).ToList().ForEach(delegate(string file)
				{
					FileInfo fileInfo = new FileInfo(file);
					if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
					{
						fileInfo.Attributes = FileAttributes.Normal;
					}
				});
				DirectoryInfo directoryInfo = new DirectoryInfo(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
				directoryInfo.Attributes = FileAttributes.Normal;
			}
		}
		catch
		{
			_INIT_UNPACKER = false;
			EnableActions("gta5");
			GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі розпакування архіву з грою! Спробуйте виконати наступне:\n\n1. Закрийте лаунчер.\n2. Видаліть папку 'game', яка знаходиться в папці лаунчера.\n3. Відкрийте лаунчер знову та встановіть гру.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
		}
		if (!EnsureSevenZipRuntimeFiles())
		{
			_INIT_UNPACKER = false;
			EnableActions("gta5");
			GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Не вдалося підготувати модуль розпаковки 7z. Спробуйте ще раз або перевстановіть лаунчер.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
			return;
		}
		if (File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE))
		{
			try
			{
				DebugLog("Start unpacking archive with game...");
				_WEB_CLIENT.Dispose();
				_INIT_UNPACKER = true;
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.UseShellExecute = false;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.CreateNoWindow = true;
				processStartInfo.FileName = System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER, "7z", "7za.exe");
				processStartInfo.Arguments = "x \"" + _CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE + "\" -y -o\"" + _CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME + "\" -bsp1";
				Process process = Process.Start(processStartInfo);
				while (!process.StandardOutput.EndOfStream)
				{
					string text = process.StandardOutput.ReadLine();
					if (!string.IsNullOrWhiteSpace(text) && text.Contains("%"))
					{
						string[] array = text.Trim().Split('%');
						if (array.Length != 0 && int.TryParse(array[0].Trim(), out var percent))
						{
							DownloadProgressValue.Dispatcher.Invoke(delegate
							{
								if (DownloadProgressValue.Text != percent + "%")
								{
									DownloadProgressValue.Text = percent + "%";
								}
							});
							DownloadProgressBar.Dispatcher.Invoke(delegate
							{
								if (DownloadProgressBar.Value != (double)percent)
								{
									DownloadProgressBar.Value = percent;
								}
							});
						}
					}
				}
				process.WaitForExit();
				process.Close();
				DebugLog("Finish unpacking archive.");
				_INIT_UNPACKER = false;
				EnableActions("gta5");
				InitializeMTAGame();
				trayIcon.ShowBalloonTip(10, "", "Розпакування завершено, готові до запуску гри!", ToolTipIcon.Info);
				return;
			}
			catch (Exception ex)
			{
				SendErrorReport("start_unpacking", ex.Message);
				GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі розпакування архіву з грою! Перевстановіть лаунчер та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
				DebugLog("Cannot unpacking archive. Details: " + ex.Message, 2);
				DebugLog("Exit launcher...\n\n");
				_INIT_UNPACKER = false;
				EnableActions("gta5");
				Environment.Exit(0);
				return;
			}
		}
		SendErrorReport("start_unpacking", "Не знайдено архів з грою для розпакування");
		GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі розпакування архіву з грою! Перевстановіть лаунчер та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
		DebugLog("Cannot unpacking archive. File not found.", 2);
		DebugLog("Exit launcher...\n\n");
		_INIT_UNPACKER = false;
		EnableActions("gta5");
	}

	private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
	{
		try
		{
			DownloadStatus.Dispatcher.Invoke(delegate
			{
				if (DownloadStatus.Text != "Завантаження гри... [" + (Convert.ToDouble(e.BytesReceived + last_bytes_received) / 1024.0 / 1024.0 / _STOP_WATCH.Elapsed.TotalSeconds).ToString("0.00") + " MB/s]")
				{
					DownloadStatus.Text = "Завантаження гри... [" + (Convert.ToDouble(e.BytesReceived + last_bytes_received) / 1024.0 / 1024.0 / _STOP_WATCH.Elapsed.TotalSeconds).ToString("0.00") + " MB/s]";
				}
			});
			int percent = e.ProgressPercentage;
			if (_DOWNLOAD_TYPE)
			{
				percent = (int)((double)(e.BytesReceived + last_bytes_received) / (double)(size_to_download + last_bytes_received) * 100.0);
			}
			DownloadProgressValue.Dispatcher.Invoke(delegate
			{
				if (DownloadProgressValue.Text != percent + "%")
				{
					DownloadProgressValue.Text = percent + "%";
					DownloadProgressBar.Value = percent;
				}
			});
		}
		catch
		{
		}
	}

	private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
	{
		if (!_INIT_DOWNLOAD)
		{
			return;
		}
		if (e.Error != null)
		{
			_INIT_DOWNLOAD = false;
			_INIT_UNPACKER = false;
			EnableActions("gta5");
			if (_STOP_WATCH.IsRunning)
			{
				_STOP_WATCH.Stop();
				_STOP_WATCH.Reset();
			}
			size_to_download = 0L;
			last_bytes_received = 0L;
			DebugLog("Error on download file. Details: " + e.Error.ToString(), 2);
			if (e.Error.ToString().Contains("enough space") || e.Error.ToString().Contains("Недостаточно места") || e.Error.ToString().Contains("Недостатньо місця"))
			{
				GridDownload.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "Виникла помилка при спробі завантажити файл з нашого серверу.\n\nНедостатньо місця на диску з лаунчером(необхідно ~12ГБ), будь ласка, звільніть місце на диску та спробуйте ще раз.", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
				SendErrorReport("downloading", "Недостатньо місця у клієнта, показуємо повідомлення з проханням звільнити місце");
				InitializeMTAGame(fast_check: false);
				return;
			}
			if (!_DOWNLOAD_TYPE)
			{
				SendErrorReport("downloading_go_to_reserve", "Помилка завантаження з основного сховища, перекидуємо клієнта на завантаження по одному файлу!");
				_FORCE_DOWNLOAD = true;
				InitializeMTAGame(fast_check: false);
				return;
			}
			count_error_in_download++;
			if (count_error_in_download > 2)
			{
				count_error_in_download = 0;
				InitializeMTAGame(fast_check: false);
			}
			else
			{
				StartDownload(null, null);
			}
			return;
		}
		count_error_in_download = 0;
		if (!_DOWNLOAD_TYPE)
		{
			if (_STOP_WATCH.IsRunning)
			{
				_STOP_WATCH.Stop();
				_STOP_WATCH.Reset();
			}
			DebugLog("Finish download archive. Try to unpack.");
			trayIcon.ShowBalloonTip(10, "", "Завантаження завершено, починаємо розпакування...", ToolTipIcon.Info);
			Task.Run(delegate
			{
				StartUnpacking();
			});
			return;
		}
		last_bytes_received += _FILES_TO_DOWNLOAD[0].size;
		if (_FILES_TO_DOWNLOAD.Count > 0)
		{
			size_to_download -= _FILES_TO_DOWNLOAD[0].size;
			_FILES_TO_DOWNLOAD.RemoveAt(0);
		}
		if (_FILES_TO_DOWNLOAD.Count > 0)
		{
			StartDownloadFiles();
			return;
		}
		if (_STOP_WATCH.IsRunning)
		{
			_STOP_WATCH.Stop();
			_STOP_WATCH.Reset();
		}
		_WEB_CLIENT.CancelAsync();
		_WEB_CLIENT.Dispose();
		_INIT_DOWNLOAD = false;
		EnableActions("gta5");
		size_to_download = 0L;
		last_bytes_received = 0L;
		DebugLog("Finish download files by one.");
		Task.Run(() => InitializeMTAGame());
	}

	private bool Is_1_5_9_MtaVersion()
	{
		if (!Directory.Exists(System.IO.Path.Combine(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, "bin")))
		{
			return true;
		}
		return false;
	}

	private void killProccess(string processName)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName(processName);
			Process[] array = processesByName;
			Process[] array2 = array;
			foreach (Process process in array2)
			{
				try
				{
					process.Kill();
					process.WaitForExit();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private bool IsExistProcess(string processName)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName(processName);
			Process[] array = processesByName;
			int num = 0;
			if (num < array.Length)
			{
				Process process = array[num];
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private void DeleteMtaGameFiels()
	{
		DebugLog("Start deleting game files...");
		if (Directory.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME))
		{
			try
			{
				Directory.GetFiles(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, "*", SearchOption.AllDirectories).ToList().ForEach(delegate(string file)
				{
					try
					{
						FileInfo fileInfo = new FileInfo(file);
						if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
						{
							fileInfo.Attributes = FileAttributes.Normal;
						}
					}
					catch
					{
					}
				});
				DirectoryInfo directoryInfo = new DirectoryInfo(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
				directoryInfo.Attributes = FileAttributes.Normal;
				Directory.Delete(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME, recursive: true);
				Directory.CreateDirectory(_CLIENT_PATH_LAUNCHER + _LOCATION_MTA_GAME);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(this, "Виникла помилка під час спроби очищення папки з грою, будь ласка, перевірьте чи гра була точно закрита та спробуйте ще раз.\n\nЯкщо ця помилка не зникає - зверніться до адміністрації.\n\n[INFO]:\n" + ex.Message, "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
		if (File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE))
		{
			try
			{
				File.Delete(_CLIENT_PATH_LAUNCHER + _FILE_GAME_ARCHIVE);
			}
			catch
			{
			}
		}
		DebugLog("Finish deleting game files...");
	}

	private void ReinstallGame(object sender, EventArgs e)
	{
		MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(this, "Переустановка гри призведе до повного видалення всіх збережених паролів, налаштувань і ігрових скриншотів. Це дія не вплине на ваш ігровий акаунт і всі ігрові цінності залишаться недоторканими.\n\nВи впевнені, що хочете переінсталювати гру?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No, System.Windows.MessageBoxOptions.None);
		if (messageBoxResult == MessageBoxResult.Yes && !_CLIENT_ADMIN)
		{
			if (_INIT_DOWNLOAD || _INIT_UNPACKER)
			{
				System.Windows.MessageBox.Show(this, "Ця дія неможлива доки триває завантаження гри!", "Інформація", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				return;
			}
			DeleteMtaGameFiels();
			OpenMTA(null, null);
			InitializeMTAGame();
		}
	}

	private void ReinstallRageMP(object sender, EventArgs e)
	{
		Process[] processesByName = Process.GetProcessesByName("ragemp_v");
		if (processesByName.Length != 0)
		{
			GridAllReady.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(this, "RAGE Multiplayer вже відкритий, переустановка неможлива.\n\nБудь ласка, завершіть процес гри.\n\nЯкщо ця помилка не зникає - зверніться до підтримки:\nTELEGRAM: @ukrainegta5bot", "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand));
			return;
		}
		MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(this, "Переустановка RAGE Multiplayer призведе до повного видалення всіх збережених паролів, налаштувань та скачаних рестурсів сервера. Це дія не вплине на ваш ігровий акаунт і всі ігрові цінності залишаться недоторканими.\n\nВи впевнені, що хочете переінсталювати гру?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No, System.Windows.MessageBoxOptions.None);
		if (messageBoxResult != MessageBoxResult.Yes || _CLIENT_ADMIN)
		{
			return;
		}
		if (_INIT_DOWNLOAD || _INIT_UNPACKER)
		{
			System.Windows.MessageBox.Show(this, "Ця дія неможлива доки триває завантаження гри!", "Інформація", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		if (Directory.Exists(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP))
		{
			try
			{
				Directory.GetFiles(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP, "*", SearchOption.AllDirectories).ToList().ForEach(delegate(string file)
				{
					try
					{
						FileInfo fileInfo = new FileInfo(file);
						if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
						{
							fileInfo.Attributes = FileAttributes.Normal;
						}
					}
					catch
					{
					}
				});
				DirectoryInfo directoryInfo = new DirectoryInfo(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP);
				directoryInfo.Attributes = FileAttributes.Normal;
				Directory.Delete(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP, recursive: true);
				Directory.CreateDirectory(_CLIENT_PATH_LAUNCHER + _LOCATION_RAGE_MP);
				RageMpFolderPath.Dispatcher.Invoke(delegate
				{
					RageMpFolderPath.Text = "";
				});
				using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\RAGE-MP", writable: true);
				registryKey?.DeleteValue("rage_path", throwOnMissingValue: false);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(this, "Виникла помилка під час спроби очищення папки з RAGE Multiplayer, будь ласка, перевірьте чи гра була точно закрита та спробуйте ще раз.\n\nЯкщо ця помилка не зникає - зверніться до адміністрації.\n\n[INFO]:\n" + ex.Message, "Помилка!", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
		if (File.Exists(_CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE))
		{
			try
			{
				File.Delete(_CLIENT_PATH_LAUNCHER + _FILE_RAGE_MP_ARCHIVE);
			}
			catch
			{
			}
		}
		OpenGTA5(null, null);
		InitializeGTA5Game();
	}

	private string LoadFavoriteGta5Server(string default_server = "s1")
	{
		string text = default_server;
		try
		{
			string favorite_server_mta = _SETTINGS.LauncherSettings["favoriteServerGTA5"];
			if (favorite_server_mta != null)
			{
				GameServer gameServer = ServerListMTA.FirstOrDefault((GameServer item) => item.id == favorite_server_mta);
				if (gameServer == null)
				{
					text = default_server;
					_SETTINGS.LauncherSettings["favoriteServerGTA5"] = text;
					_SETTINGS.SaveSettings();
				}
			}
			else
			{
				_SETTINGS.LauncherSettings["favoriteServerGTA5"] = text;
				_SETTINGS.SaveSettings();
			}
		}
		catch
		{
			text = default_server;
			DebugLog("Error while read gta5 server number from settings.", 2);
		}
		return text;
	}

	private string LoadFavoriteMTAServer(string default_server = "s6")
	{
		string text = default_server;
		try
		{
			string favorite_server_mta = _SETTINGS.LauncherSettings["favoriteServerMTA"];
			if (favorite_server_mta != null)
			{
				GameServer gameServer = ServerListMTA.FirstOrDefault((GameServer item) => item.id == favorite_server_mta);
				if (gameServer != null)
				{
					text = favorite_server_mta;
					_SETTINGS.LauncherSettings["favoriteServerMTA"] = favorite_server_mta;
					_SETTINGS.SaveSettings();
				}
			}
			else
			{
				_SETTINGS.LauncherSettings["favoriteServerMTA"] = text;
				_SETTINGS.SaveSettings();
			}
		}
		catch
		{
			text = default_server;
			DebugLog("Error while read mta server number from settings.", 2);
		}
		return text;
	}

	private void SaveFavoriteMTAServer(string server)
	{
		GameServer gameServer = ServerListMTA.FirstOrDefault((GameServer item) => item.id == server);
		if (gameServer == null)
		{
			return;
		}
		_FAVORITE_MTA_SERVER = server;
		try
		{
			_SETTINGS.LauncherSettings["favoriteServerMTA"] = _FAVORITE_MTA_SERVER;
			_SETTINGS.SaveSettings();
		}
		catch
		{
		}
	}

	private void SaveFavoriteGTA5Server(string server)
	{
		GameServer gameServer = ServerListGTA5.FirstOrDefault((GameServer item) => item.id == server);
		if (gameServer == null)
		{
			return;
		}
		_FAVORITE_GTA5_SERVER = server;
		try
		{
			_SETTINGS.LauncherSettings["favoriteServerGTA5"] = _FAVORITE_GTA5_SERVER;
			_SETTINGS.SaveSettings();
		}
		catch
		{
		}
	}

	private void SelectGTA5Server(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			SelectServerGTA5InWindow(button.Tag.ToString());
		}
	}

	private void SelectServerGTA5InWindow(string server)
	{
		if (server == null)
		{
			return;
		}
		GameServer gameServer = ServerListGTA5.FirstOrDefault((GameServer item) => item.id == server);
		if (gameServer == null || FavoriteServerNameGta5.Text == gameServer.name)
		{
			return;
		}
		foreach (GameServer item in ServerListGTA5)
		{
			item.element.Style = (Style)FindResource("ServerButtonGta5Style");
		}
		gameServer.element.Style = (Style)FindResource("ServerButtonStyleGta5Active");
		FavoriteServerNumberGta5.Text = gameServer.id.TrimStart('s');
		FavoriteServerNameGta5.Text = gameServer.name;
		FavoriteServerPlayerCountGta5.Text = gameServer.players;
		FavoriteServerMaxPlayerCountGta5.Text = gameServer.max_players;
		SaveFavoriteGTA5Server(server);
	}

	private void SelectMTAServer(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			SelectMTAServerInWindow(button.Tag.ToString());
		}
	}

	private void SelectMTAServerInWindow(string server)
	{
		if (server == null)
		{
			return;
		}
		GameServer gameServer = ServerListMTA.FirstOrDefault((GameServer item) => item.id == server);
		if (gameServer == null || FavoriteServerName.Text == gameServer.name)
		{
			return;
		}
		foreach (GameServer serverListMTum in ServerListMTA)
		{
			serverListMTum.element.Style = (Style)FindResource("ServerButtonMTAStyle");
		}
		gameServer.element.Style = (Style)FindResource("ServerButtonMTAStyleActive");
		FavoriteServerName.Text = gameServer.name;
		FavoriteServerPlayerCount.Text = gameServer.players;
		FavoriteServerMaxPlayerCount.Text = gameServer.max_players;
		FavoriteServerIcon.Source = new BitmapImage(gameServer.image_url);
		SaveFavoriteMTAServer(server);
	}

	private void MonitoringTimer_Tick(object sender, EventArgs e)
	{
		Task.Run(delegate
		{
			try
			{
				if (_MonitoringClient == null || !_MonitoringClient.Connected)
				{
					_MonitoringClient?.Close();
					_MonitoringClient?.Dispose();
					_MonitoringClient = new System.Net.Sockets.TcpClient();
					_MonitoringClient.ConnectAsync("g1.ketrix.cloud", 20013).Wait(5000);
				}

				if (_MonitoringClient != null && _MonitoringClient.Connected)
				{
					// Send heartbeat/ping
					byte[] pingData = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
					_MonitoringClient.GetStream().Write(pingData, 0, pingData.Length);
					DebugLog("Monitoring ping sent to g1.ketrix.cloud:20013");
				}
			}
			catch (Exception ex)
			{
				DebugLog("Monitoring connection failed: " + ex.Message, 1);
				_MonitoringClient?.Close();
				_MonitoringClient?.Dispose();
				_MonitoringClient = null;
			}
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.1.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
				string currentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
				Uri resourceLocator = new Uri("/" + currentAssemblyName + ";component/launcher2_ukraine_gta.mainwindow.xaml", UriKind.Relative);
				System.Windows.Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.1.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((MainWindow)target).MouseLeftButtonDown += Window_MouseLeftButtonDown;
			break;
		case 2:
			borderElement = (Border)target;
			break;
		case 3:
			HeaderImage = (System.Windows.Controls.Image)target;
			HeaderImage.MouseDown += OpenLink;
			break;
		case 4:
			CabinetButton = (System.Windows.Controls.Button)target;
			CabinetButton.Click += OpenLink;
			break;
		case 5:
			InstagramButton = (System.Windows.Controls.Image)target;
			InstagramButton.MouseDown += OpenLink;
			break;
		case 6:
			TelegramButton = (System.Windows.Controls.Image)target;
			TelegramButton.MouseDown += OpenLink;
			break;
		case 7:
			DiscordButton = (System.Windows.Controls.Image)target;
			DiscordButton.MouseDown += OpenLink;
			break;
		case 8:
			SettingsButton = (System.Windows.Controls.Button)target;
			SettingsButton.Click += OpenSettings;
			break;
		case 9:
			((System.Windows.Controls.Button)target).Click += MinimizeLauncher;
			break;
		case 10:
			((System.Windows.Controls.Button)target).Click += CloseLauncher;
			break;
		case 11:
			LeftMenu = (Grid)target;
			break;
		case 12:
			LeftMenuBorder = (Border)target;
			break;
		case 13:
			DonateButton = (System.Windows.Controls.Button)target;
			DonateButton.Click += OpenDonate;
			break;
		case 14:
			SiteButton = (System.Windows.Controls.Button)target;
			SiteButton.Click += OpenLink;
			break;
		case 15:
			ForumButton = (System.Windows.Controls.Button)target;
			ForumButton.Click += OpenLink;
			break;
		case 16:
			TotalProjectsOnline = (TextBlock)target;
			break;
		case 17:
			ProjectMTAButton = (System.Windows.Controls.Button)target;
			ProjectMTAButton.Click += OpenMTA;
			break;
		case 18:
			projectEllipseBackgroundMta = (Ellipse)target;
			break;
		case 19:
			ProjectMTATotalOnline = (TextBlock)target;
			break;
		case 20:
			GameVersion = (TextBlock)target;
			break;
		case 21:
			AdminMode = (StackPanel)target;
			break;
		case 22:
			MTABlock = (Grid)target;
			break;
		case 23:
			NewsLinkUrl = (Grid)target;
			NewsLinkUrl.MouseDown += OpenLink;
			break;
		case 24:
			NewsImage = (System.Windows.Controls.Image)target;
			break;
		case 25:
			NewsDate = (TextBlock)target;
			break;
		case 26:
			NewsTitle = (TextBlock)target;
			break;
		case 27:
			NewsDescription = (TextBlock)target;
			break;
		case 28:
			TotalOnline = (TextBlock)target;
			break;
		case 29:
			MtaX2Block = (Grid)target;
			break;
		case 30:
			GridAllReady = (Grid)target;
			break;
		case 31:
			AllReadyStatus = (TextBlock)target;
			break;
		case 32:
			AllReadyAdditional = (TextBlock)target;
			break;
		case 33:
			ButtonPlay = (Grid)target;
			break;
		case 34:
			StartButton = (System.Windows.Controls.Button)target;
			StartButton.Click += PlayGameMTA;
			break;
		case 35:
			GridNeedDownload = (Grid)target;
			break;
		case 36:
			NeedDownloadSize = (TextBlock)target;
			break;
		case 37:
			GridDownload = (Grid)target;
			break;
		case 38:
			DownloadStatus = (TextBlock)target;
			break;
		case 39:
			DownloadProgressValue = (TextBlock)target;
			break;
		case 40:
			DownloadProgressBar = (System.Windows.Controls.ProgressBar)target;
			break;
		case 41:
			ButtonStartDownload = (Grid)target;
			break;
		case 42:
			StartDownloadButton = (System.Windows.Controls.Button)target;
			StartDownloadButton.Click += StartDownload;
			break;
		case 43:
			GridCheckFiles = (Grid)target;
			break;
		case 44:
			CheckFilesProgressValue = (TextBlock)target;
			break;
		case 45:
			CheckFilesProgressBar = (System.Windows.Controls.ProgressBar)target;
			break;
		case 46:
			Gta5Block = (Grid)target;
			break;
		case 47:
			NewsLinkUrlGta5 = (Grid)target;
			NewsLinkUrlGta5.MouseDown += OpenLink;
			break;
		case 48:
			NewsImageGta5 = (System.Windows.Controls.Image)target;
			break;
		case 49:
			NewsDateGta5 = (TextBlock)target;
			break;
		case 50:
			NewsTitleGta5 = (TextBlock)target;
			break;
		case 51:
			NewsDescriptionGta5 = (TextBlock)target;
			break;
		case 52:
			TotalOnlineGta5 = (TextBlock)target;
			break;
		case 53:
			Gta5X2Block = (Grid)target;
			break;
		case 54:
			GridNeedDownloadGTA5 = (Grid)target;
			break;
		case 55:
			NeedDownloadGTA5Size = (TextBlock)target;
			break;
		case 56:
			GridAllReadyGTA5 = (Grid)target;
			break;
		case 57:
			AllReadyStatusGTA5 = (TextBlock)target;
			break;
		case 58:
			AllReadyAdditionalGTA5 = (TextBlock)target;
			break;
		case 59:
			ButtonPlayGTA5 = (Grid)target;
			break;
		case 60:
			StartButtonGta5 = (System.Windows.Controls.Button)target;
			StartButtonGta5.Click += PlayGameGTA5;
			break;
		case 61:
			GridDownloadGTA5 = (Grid)target;
			break;
		case 62:
			DownloadGTA5Status = (TextBlock)target;
			break;
		case 63:
			DownloadGTA5ProgressValue = (TextBlock)target;
			break;
		case 64:
			DownloadGTA5ProgressBar = (System.Windows.Controls.ProgressBar)target;
			break;
		case 65:
			ButtonStartDownloadGTA5 = (Grid)target;
			break;
		case 66:
			StartDownloadButtonGTA5 = (System.Windows.Controls.Button)target;
			StartDownloadButtonGTA5.Click += StartDownloadGTA5;
			break;
		case 67:
			GridnnNeedBuyGTA5 = (Grid)target;
			break;
		case 68:
			ButtonBuyGTA5 = (Grid)target;
			break;
		case 69:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 70:
			DonateMTABlock = (Grid)target;
			break;
		case 71:
			((System.Windows.Controls.Button)target).Click += OpenMTA;
			break;
		case 72:
			((TextBlock)target).MouseDown += OpenMTA;
			break;
		case 73:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 74:
			DonateOffer = (System.Windows.Controls.Image)target;
			break;
		case 75:
			DonateOfferLink = (System.Windows.Controls.Button)target;
			DonateOfferLink.Click += OpenLink;
			break;
		case 76:
			DonateScroll = (ScrollViewer)target;
			DonateScroll.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
			break;
		case 77:
			DonateContent = (Grid)target;
			DonateContent.MouseLeftButtonDown += Window_MouseLeftButtonDown;
			break;
		case 78:
			PremiumButtonsMTA = (StackPanel)target;
			break;
		case 79:
			((System.Windows.Controls.Button)target).Click += PremiumDaysMTA;
			break;
		case 80:
			((System.Windows.Controls.Button)target).Click += PremiumDaysMTA;
			break;
		case 81:
			((System.Windows.Controls.Button)target).Click += PremiumDaysMTA;
			break;
		case 82:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 83:
			PremiumCost = (TextBlock)target;
			break;
		case 84:
			((TextBlock)target).MouseDown += OpenLink;
			break;
		case 85:
			Vehicle1 = (StackPanel)target;
			break;
		case 86:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 87:
			Vehicle1Button = (StackPanel)target;
			break;
		case 88:
			Vehicle2 = (StackPanel)target;
			break;
		case 89:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 90:
			Vehicle2Button = (StackPanel)target;
			break;
		case 91:
			Vehicle3 = (StackPanel)target;
			break;
		case 92:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 93:
			Vehicle3Button = (StackPanel)target;
			break;
		case 94:
			Case1 = (StackPanel)target;
			break;
		case 95:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 96:
			Case1Button = (StackPanel)target;
			break;
		case 97:
			Case2 = (StackPanel)target;
			break;
		case 98:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 99:
			Case2Button = (StackPanel)target;
			break;
		case 100:
			Case3 = (StackPanel)target;
			break;
		case 101:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 102:
			Case3Button = (StackPanel)target;
			break;
		case 103:
			DonateGTA5Block = (Grid)target;
			break;
		case 104:
			((System.Windows.Controls.Button)target).Click += OpenGTA5;
			break;
		case 105:
			((TextBlock)target).MouseDown += OpenGTA5;
			break;
		case 106:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 107:
			DonateGTA5Offer = (System.Windows.Controls.Image)target;
			break;
		case 108:
			DonateGTA5OfferLink = (System.Windows.Controls.Button)target;
			DonateGTA5OfferLink.Click += OpenLink;
			break;
		case 109:
			DonateGTA5Scroll = (ScrollViewer)target;
			DonateGTA5Scroll.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
			break;
		case 110:
			DonateGTA5Content = (Grid)target;
			DonateGTA5Content.MouseLeftButtonDown += Window_MouseLeftButtonDown;
			break;
		case 111:
			PremiumGTA5Buttons = (StackPanel)target;
			break;
		case 112:
			((System.Windows.Controls.Button)target).Click += PremiumDaysGTA5;
			break;
		case 113:
			((System.Windows.Controls.Button)target).Click += PremiumDaysGTA5;
			break;
		case 114:
			((System.Windows.Controls.Button)target).Click += PremiumDaysGTA5;
			break;
		case 115:
			((System.Windows.Controls.Button)target).Click += PremiumDaysGTA5;
			break;
		case 116:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 117:
			PremiumGTA5Cost = (TextBlock)target;
			break;
		case 118:
			((TextBlock)target).MouseDown += OpenLink;
			break;
		case 119:
			Vehicle1GTA5 = (StackPanel)target;
			break;
		case 120:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 121:
			Vehicle1GTA5Button = (StackPanel)target;
			break;
		case 122:
			Vehicle2GTA5 = (StackPanel)target;
			break;
		case 123:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 124:
			Vehicle2GTA5Button = (StackPanel)target;
			break;
		case 125:
			Vehicle3GTA5 = (StackPanel)target;
			break;
		case 126:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 127:
			Vehicle3GTA5Button = (StackPanel)target;
			break;
		case 128:
			Case1GTA5 = (StackPanel)target;
			break;
		case 129:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 130:
			Case1GTA5Button = (StackPanel)target;
			break;
		case 131:
			Case2GTA5 = (StackPanel)target;
			break;
		case 132:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 133:
			Case2GTA5Button = (StackPanel)target;
			break;
		case 134:
			Case3GTA5 = (StackPanel)target;
			break;
		case 135:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 136:
			Case3GTA5Button = (StackPanel)target;
			break;
		case 137:
			SettingsMTABlock = (Grid)target;
			break;
		case 138:
			((System.Windows.Controls.Button)target).Click += OpenMTA;
			break;
		case 139:
			((TextBlock)target).MouseDown += OpenMTA;
			break;
		case 140:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 141:
			GameFolderPath = (TextBlock)target;
			GameFolderPath.MouseDown += OpenGameFolder;
			break;
		case 142:
			((System.Windows.Controls.Button)target).Click += OpenGameFolder;
			break;
		case 143:
			((System.Windows.Controls.Button)target).Click += ReinstallGame;
			break;
		case 144:
			AutoStart = (System.Windows.Controls.CheckBox)target;
			break;
		case 145:
			((TextBlock)target).MouseDown += SetChecked;
			break;
		case 146:
			AutoLogin = (System.Windows.Controls.CheckBox)target;
			break;
		case 147:
			((TextBlock)target).MouseDown += SetChecked;
			break;
		case 148:
			GraphicLow = (StackPanel)target;
			GraphicLow.MouseDown += SelectGraphic;
			break;
		case 149:
			GraphicMedium = (StackPanel)target;
			GraphicMedium.MouseDown += SelectGraphic;
			break;
		case 150:
			GraphicHigh = (StackPanel)target;
			GraphicHigh.MouseDown += SelectGraphic;
			break;
		case 151:
			((StackPanel)target).MouseDown += CopyLauncherIdMTA;
			break;
		case 152:
			LauncherIdMTA = (TextBlock)target;
			break;
		case 153:
			SettingsGTA5Block = (Grid)target;
			break;
		case 154:
			((System.Windows.Controls.Button)target).Click += OpenGTA5;
			break;
		case 155:
			((TextBlock)target).MouseDown += OpenGTA5;
			break;
		case 156:
			((System.Windows.Controls.Button)target).Click += OpenLink;
			break;
		case 157:
			RageMpFolderPath = (TextBlock)target;
			RageMpFolderPath.MouseDown += SetRageMpFolder;
			break;
		case 158:
			((System.Windows.Controls.Button)target).Click += OpenRageMpFolder;
			break;
		case 159:
			((System.Windows.Controls.Button)target).Click += ReinstallRageMP;
			break;
		case 160:
			Gta5FolderPath = (TextBlock)target;
			Gta5FolderPath.MouseDown += SetGta5Folder;
			break;
		case 161:
			((System.Windows.Controls.Button)target).Click += OpeGta5Folder;
			break;
		case 162:
			((StackPanel)target).MouseDown += CopyLauncherIdMTA;
			break;
		case 163:
			LauncherIdGTA5 = (TextBlock)target;
			break;
		case 164:
			ServersBlockMTA = (Grid)target;
			break;
		case 165:
			GridFavoriteServer = (Grid)target;
			break;
		case 166:
			FavoriteServerIcon = (System.Windows.Controls.Image)target;
			break;
		case 167:
			FavoriteServerName = (TextBlock)target;
			break;
		case 168:
			FavoriteServerPlayerCount = (TextBlock)target;
			break;
		case 169:
			FavoriteServerMaxPlayerCount = (TextBlock)target;
			break;
		case 170:
			Scroll = (ScrollViewer)target;
			Scroll.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
			break;
		case 171:
			GridServers = (Grid)target;
			break;
		case 172:
			ServersBlockGta5 = (Grid)target;
			break;
		case 173:
			GridFavoriteServerGta5 = (Grid)target;
			break;
		case 174:
			FavoriteServerNumberGta5 = (TextBlock)target;
			break;
		case 175:
			FavoriteServerNameGta5 = (TextBlock)target;
			break;
		case 176:
			FavoriteServerPlayerCountGta5 = (TextBlock)target;
			break;
		case 177:
			FavoriteServerMaxPlayerCountGta5 = (TextBlock)target;
			break;
		case 178:
			ScrollGta5 = (ScrollViewer)target;
			ScrollGta5.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
			break;
		case 179:
			GridServersGta5 = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}




