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
using System.Windows.Shapes;
using System.Xml;

namespace StreamCentral
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            if (Application.Current.Properties.Contains("OAuthToken"))
            {
                oauthTextBox.Text = (String)Application.Current.Properties["OAuthToken"];
            }
            if (Application.Current.Properties.Contains("TwitchChannel"))
            {
                twitchChannelTextBox.Text = (String)Application.Current.Properties["TwitchChannel"];
            }
            if (Application.Current.Properties.Contains("TwitchBotUsername"))
            {
                botUsernameTextBox.Text = (String)Application.Current.Properties["TwitchBotUsername"];
            }
            if (Application.Current.Properties.Contains("TwitchBotOAuthToken"))
            {
                botOauthTextBox.Text = (String)Application.Current.Properties["TwitchBotOAuthToken"];
            }
            if (Application.Current.Properties.Contains("DefaultPlaylistURL"))
            {
                defaultPlaylistTextBox.Text = (String)Application.Current.Properties["DefaultPlaylistURL"];
            }
        }

        private void saveSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            XmlDocument settingsDoc = new XmlDocument();

            XmlElement settingsRootElement = settingsDoc.CreateElement("Settings");

            XmlElement OAuthTokenNode = settingsDoc.CreateElement("OAuthToken");
            OAuthTokenNode.InnerText = oauthTextBox.Text;
            settingsRootElement.AppendChild(OAuthTokenNode);
            addPropertyHelper("OAuthToken", oauthTextBox.Text);

            XmlElement twitchChannelNode = settingsDoc.CreateElement("TwitchChannel");
            twitchChannelNode.InnerText = twitchChannelTextBox.Text;
            settingsRootElement.AppendChild(twitchChannelNode);
            addPropertyHelper("TwitchChannel", twitchChannelTextBox.Text);

            XmlElement botUsernameNode = settingsDoc.CreateElement("TwitchBotUsername");
            botUsernameNode.InnerText = botUsernameTextBox.Text;
            settingsRootElement.AppendChild(botUsernameNode);
            addPropertyHelper("TwitchBotUsername", botUsernameTextBox.Text);

            XmlElement botOAuthTokenNode = settingsDoc.CreateElement("TwitchBotOAuthToken");
            botOAuthTokenNode.InnerText = botOauthTextBox.Text;
            settingsRootElement.AppendChild(botOAuthTokenNode);
            addPropertyHelper("TwitchBotOAuthToken", botOauthTextBox.Text);

            XmlElement defaultPlaylistNode = settingsDoc.CreateElement("DefaultPlaylistURL");
            defaultPlaylistNode.InnerText = defaultPlaylistTextBox.Text;
            settingsRootElement.AppendChild(defaultPlaylistNode);
            addPropertyHelper("DefaultPlaylistURL", defaultPlaylistTextBox.Text);

            settingsDoc.AppendChild(settingsRootElement);
            settingsDoc.Save("StreamSettings.xml");

            this.Close();
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
    }
}
