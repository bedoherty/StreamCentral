using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Twitch.Net.Interfaces;
using Twitch.Net.Factories;
using RestSharp;
using System.Net;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using System.Windows.Threading;
using System.Threading;
using System.Text.RegularExpressions;
using ChatSharp;
using System.Xml;
using System.IO;

namespace StreamCentral
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        #region Global Window Variables

        private readonly SpotifyLocalAPI _spotify = new SpotifyLocalAPI();

        private Queue<string> songQueue;

        private Track _currentTrack;

        IrcClient twitchBot;

        List<String> banWords = new List<String>();

        List<String> timeoutWords = new List<String>();

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            loadSettings();

            loadChatFilters();

            songQueue = new Queue<string>();

            initSpotifyClient();
        }

        #region Twitch Bot
        public void startTwitchBot()
        {
            string botUsername = getSetting("TwitchBotUsername");
            string botOAuthToken = getSetting("TwitchBotOAuthToken");
            string twitchChannel = getSetting("TwitchChannel");

            twitchBot = new IrcClient("irc.twitch.tv", new IrcUser(botUsername, botUsername, "oauth:" + botOAuthToken));

            twitchBot.SendIrcMessage(new IrcMessage(("CAP REQ :twitch.tv/tags")));

            twitchBot.ConnectionComplete += (s, e) => twitchBot.JoinChannel("#" + twitchChannel);

            twitchBot.ChannelMessageRecieved += (s, e) =>
            {
                var channel = twitchBot.Channels[e.PrivateMessage.Source];

                if (hasBanWord(e.PrivateMessage.Message))
                {
                    channel.SendMessage("/ban " + e.PrivateMessage.User.Nick);
                    channel.SendMessage(e.PrivateMessage.User.Nick + " has been banned for extreme language.");
                }

                if (hasTimeoutWord(e.PrivateMessage.Message))
                {
                    channel.SendMessage("/timeout " + e.PrivateMessage.User.Nick);
                    channel.SendMessage(e.PrivateMessage.User.Nick + " has been put in timeout until he can cool off.");
                }

                if (e.PrivateMessage.Message == ".list")
                    channel.SendMessage(string.Join(", ", channel.Users.Select(u => u.Nick)));
                else if (e.PrivateMessage.Message.StartsWith("!np"))
                {
                    channel.SendMessage("Now Playing: " + _currentTrack.TrackResource.Name + " by " + _currentTrack.ArtistResource.Name);
                }
                else if (e.PrivateMessage.Message.StartsWith("!songrequest "))
                {
                    string songId = e.PrivateMessage.Message.Substring(13);
                    songQueue.Enqueue(songId);
                    channel.SendMessage(songId + " has been placed in queue.  Position: " + songQueue.Count.ToString());
                }
                else if(e.PrivateMessage.Message.StartsWith("!help "))
                {
                    string command = e.PrivateMessage.Message.Substring(6);
                    showHelp(command);
                }

                addChatMessage(e.PrivateMessage.User.Nick + ": " + e.PrivateMessage.Message);
            };

            twitchBot.ConnectAsync();
        }
        
        public string showHelp(string input)
        {
            switch (input)
            {
                case "!np":
                    return "!np will show the currently playing song";
                case "!songrequest":
                    return "!songrequest [spotify_song_id] will queue up a song to be played";
                default:
                    return "Commands: !np !songrequest !help  Type !help followed by a command for more information.";
            }
        }

        public bool hasBanWord(string message)
        {
            return banWords.Any(banWord => message.Contains(banWord));
        }

        public bool hasTimeoutWord(string message)
        {
            return timeoutWords.Any(timeoutWord => message.Contains(timeoutWord));
        }

        public void addChatMessage(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                        () => addChatMessageHelper(message), DispatcherPriority.Normal);
            }
            else
            {
                addChatMessageHelper(message);
            }
        }

        public void addChatMessageHelper(string message)
        {
            twitchChatListView.Items.Add(message);
            twitchChatListView.Items.MoveCurrentToLast();
            twitchChatListView.ScrollIntoView(twitchChatListView.Items.CurrentItem);

        }

        private void startTwitchBotButtonClicked(object sender, RoutedEventArgs e)
        {
            startTwitchBotButton.IsEnabled = false;

            startTwitchBot();

            startTwitchBotButton.Content = "Twitch Bot Started";
        }

        public void loadChatFilters()
        {
            if (File.Exists("ban_words.txt"))
            {
                foreach (string banWord in File.ReadLines("ban_words.txt"))
                {
                    if (!String.IsNullOrWhiteSpace(banWord))
                    {
                        banWords.Add(banWord);
                    }
                }
            }
            if (File.Exists("timeout_words.txt"))
            {
                foreach (string timeoutWord in File.ReadLines("timeout_words.txt"))
                {
                    if (!String.IsNullOrWhiteSpace(timeoutWord))
                    {
                        timeoutWords.Add(timeoutWord);
                    }
                }
            }

        }

        #endregion

        #region Window Setup
        private void settingsMenuButtonClicked(object sender, RoutedEventArgs e)
        {
           SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void onMainWindowExit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }
        #endregion

        #region Local Spotify Client
        private void initSpotifyClient()
        {

            _spotify.OnTrackChange += _spotify_OnTrackChange;
            _spotify.OnTrackTimeChange += _spotify_OnTrackTimeChange;

            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                MessageBox.Show(@"Spotify isn't running!");
                return;
            }
            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                MessageBox.Show(@"SpotifyWebHelper isn't running!");
                return;
            }

            bool successful = _spotify.Connect();
            if (successful)
            {
                StatusResponse status = _spotify.GetStatus();
                if (status == null)
                    return;

                if (status.Track != null) //Update track infos
                    updateTrack(status.Track);
                _spotify.ListenForEvents = true;
            }
            else
            {
                MessageBox.Show(@"Couldn't connect to the spotify client.");
            }
        }

        public void updateTrack(Track track)
        {
            if (track.IsAd())
                return; //Don't process further, maybe null values

            setNowPlaying(track);
        }

        private void setNowPlaying(Track currentTrack)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                        () => nowPlayingTextBox.Text = currentTrack.TrackResource.Name + " - " + currentTrack.ArtistResource.Name, DispatcherPriority.Normal);
            }
            else
            {
                nowPlayingTextBox.Text = currentTrack.TrackResource.Name + " - " + currentTrack.ArtistResource.Name;
            }
            _currentTrack = currentTrack;
            System.IO.File.WriteAllText(@"NowPlaying.txt", currentTrack.TrackResource.Name + " - " + currentTrack.ArtistResource.Name);
        }

        public void playNext()
        {
            _spotify.PlayURL(@"spotify:track:" + songQueue.Dequeue());
        }

        public void playDefaultPlaylist()
        {
            string defaultPlaylistURL = getSetting("DefaultPlaylistURL");
            _spotify.PlayURL(defaultPlaylistURL);
        }

        #endregion

        #region Local Spotify Callbacks
        private void _spotify_OnTrackChange(TrackChangeEventArgs e)
        {
            updateTrack(e.NewTrack);          
        }

        private void _spotify_OnTrackTimeChange(TrackTimeChangeEventArgs e)
        {
            //System.IO.File.AppendAllText(@"Timings.txt", ((int)e.TrackTime).ToString() + ":" + _currentTrack.Length + "\r\n");
            if ( (int)e.TrackTime == _currentTrack.Length-1)
            {
                //MessageBox.Show("Track ended!");
                if (songQueue.Count != 0)
                {
                    playNext();
                }
                else
                {
                    playDefaultPlaylist();
                }
            }
        }

        #endregion

        #region Loading Settings
        public void loadSettings()
        {
            XmlDocument settingsDoc = new XmlDocument();

            settingsDoc.Load("StreamSettings.xml");

            XmlElement rootSettingsElement = (XmlElement)settingsDoc.GetElementsByTagName("Settings")[0];

            XmlElement OAuthTokenElement = (XmlElement)rootSettingsElement.GetElementsByTagName("OAuthToken")[0];
            addPropertyHelper("OAuthToken", OAuthTokenElement.InnerText);

            XmlElement twitchChannelElement = (XmlElement)rootSettingsElement.GetElementsByTagName("TwitchChannel")[0];
            addPropertyHelper("TwitchChannel", twitchChannelElement.InnerText);

            XmlElement botUsernameElement = (XmlElement)rootSettingsElement.GetElementsByTagName("TwitchBotUsername")[0];
            addPropertyHelper("TwitchBotUsername", botUsernameElement.InnerText);

            XmlElement botOAuthTokenElement = (XmlElement)rootSettingsElement.GetElementsByTagName("TwitchBotOAuthToken")[0];
            addPropertyHelper("TwitchBotOAuthToken", botOAuthTokenElement.InnerText);

            XmlElement defaultPlaylistElement = (XmlElement)rootSettingsElement.GetElementsByTagName("DefaultPlaylistURL")[0];
            addPropertyHelper("DefaultPlaylistURL", defaultPlaylistElement.InnerText);

        }

        public void addPropertyHelper(string key, string value)
        {
            if (!Application.Current.Properties.Contains(key))
            {
                Application.Current.Properties.Add(key, value);
            }
            else
            {
                Application.Current.Properties[key] = value;
            }
        }

        public string getSetting(string key)
        {
            if (Application.Current.Properties.Contains(key))
            {
                return (string)Application.Current.Properties[key];
            }
            else
            {
                return "";
            }
        }
        #endregion

        #region Twitch Integration
        private void setTwitchStatusButtonClicked(object sender, RoutedEventArgs e)
        {
            string OAuthToken = getSetting("OAuthToken");
            string channel = getSetting("TwitchChannel");
            TwitchAPI.setTwitchTitleAndGame(channel, twitchTitleBox.Text, gameTitleBox.Text, OAuthToken);
        }
        #endregion
    }
}
