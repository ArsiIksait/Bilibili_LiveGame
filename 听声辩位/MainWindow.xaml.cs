using Configuration;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using 听声辩位.Managers;

namespace 听声辩位
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; } = null!;
        public string? CurrentDirectory { get; private set; } = string.Empty;
        public List<string> VideoPlayList { get; private set; } = [];
        public List<string> AnnouncementList { get; private set; } = [];
        public int TimeLeft = 30;
        public bool PlaySound = true;
        public bool ExitQueue = false;
        public string NowSound = string.Empty;
        public int PlaySoundCount = 0;
        public int CorrectAnswered = 0;
        public int IncorrectAnswered = 0;
        public int Health = 3;
        public int BronzeManChallenge = 0;
        public Data ConfigData = new();

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            var currentAssembly = Assembly.GetEntryAssembly()!;
            CurrentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;

            if (CurrentDirectory == null)
                return;

            mediaElement.MediaEnded += StoppedEvent;
            //mediaElement.Unloaded += StoppedEvent;
            mediaElement.LoadedBehavior = MediaState.Manual;
            //mediaElement.UnloadedBehavior = MediaState.Manual;

            mediaElement.Visibility = Visibility.Collapsed;
            announcement.Visibility = Visibility.Collapsed;
        }

        public void PlayMedia(string mediaName)
        {
            Dispatcher.InvokeAsync(() =>
            {
                VideoPlayList.Add(mediaName);

                if (!mediaElement.IsVisible)
                {
                    mediaElement.SpeedRatio = VideoPlayList[0] == MediaEx.MediaList[(int)Medias.开场] ? 2.0d : 1.0d;
                    mediaElement.Visibility = Visibility.Visible;
                    mediaElement.Source = new Uri(@$"{CurrentDirectory}\Resources\{VideoPlayList[0]}");
                    mediaElement.Play();
                }
            });
        }

        public void StopMedia()
        {
            Dispatcher.InvokeAsync(() =>
            {
                mediaElement.Visibility = Visibility.Collapsed;
                mediaElement.Stop();
                mediaElement.Source = null;
            });
        }

        private void StoppedEvent(object? sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                mediaElement.Visibility = Visibility.Collapsed;

                if (VideoPlayList.Count > 0)
                {
                    VideoPlayList.RemoveAt(0);

                    if (VideoPlayList.Count > 0 && MediaEx.GetSoundPos(VideoPlayList[0]) != "ERROR" && ExitQueue)
                    {
                        ExitQueue = false;
                        VideoPlayList.RemoveAt(0);
                    }
                }

                if (VideoPlayList.Count > 0)
                {
                    mediaElement.Visibility = Visibility.Visible;
                    mediaElement.SpeedRatio = VideoPlayList[0] == MediaEx.MediaList[(int)Medias.开场] ? 2.0d : 1.0d;
                    //await Task.Delay(2000);
                    mediaElement.Source = new Uri(@$"{CurrentDirectory}\Resources\{VideoPlayList[0]}");
                    mediaElement.Play();
                    //实在不行可以用Task.Run()来防止它卡住
                }
            });
        }

        public void Announce(string message, int delay = 3000)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                if (announcement.Visibility == Visibility.Visible)
                {
                    AnnouncementList.Add(message);
                    return;
                }
                else if (AnnouncementList.Count == 0)
                {
                    AnnouncementList.Add(message);
                }

                while (AnnouncementList.Count > 0)
                {
                    if (announcement.Visibility == Visibility.Collapsed)
                    {
                        announcement.Visibility = Visibility.Visible;
                        announcement.Text = AnnouncementList[0];
                        await Task.Delay(delay);
                        AnnouncementList.RemoveAt(0);
                        announcement.Text = string.Empty;
                        announcement.Visibility = Visibility.Collapsed;
                    }

                    await Task.Delay(1000);
                }
            });
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                GameManager.OpenBLiveStart();
                GameManager.GameLogic();
                GameManager.CheckAndRemoveExpiredGifts();
            });
        }

        public void UpdateHealthBar(int health)
        {
            Dispatcher.Invoke(() =>
            {
                healthBar.Children.Clear();

                for (int i = 0; i < health; i++)
                {
                    healthBar.Children.Add(new Image());
                }
            });
        }
    }

    public enum Medias
    {
        开场,
        该罚,
        惩罚,
        失败,
        你过关,
        东方,
        南方,
        西方,
        北方,
        东北方,
        东南方,
        西北方,
        西南方,
        人声东方,
        人声南方,
        人声西方,
        人声北方,
        人声东北方,
        人声东南方,
        人声西北方,
        人声西南方,
        听不清楚
    }

    public class MediaEx
    {
        public static readonly List<string> MediaList =
        [
            "1.mp4",
            "2.mp4",
            "3.mp4",
            "4.mp4",
            "5.mp4",
            "e.mp3",
            "s.mp3",
            "w.mp3",
            "n.mp3",
            "ne.mp3",
            "se.mp3",
            "nw.mp3",
            "sw.mp3",
            "e_.mp3",
            "s_.mp3",
            "w_.mp3",
            "n_.mp3",
            "ne_.mp3",
            "se_.mp3",
            "nw_.mp3",
            "sw_.mp3",
            "x.mp3"
        ];

        public static string GetSoundPos(string sound)
        {
            return sound switch
            {
                "e.mp3" => "东方",
                "s.mp3" => "南方",
                "w.mp3" => "西方",
                "n.mp3" => "北方",
                "ne.mp3" => "东北方",
                "se.mp3" => "东南方",
                "nw.mp3" => "西北方",
                "sw.mp3" => "西南方",
                _ => "ERROR",
            };
        }
    }
}