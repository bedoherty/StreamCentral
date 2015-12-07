using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StreamCentral
{
    public static class TwitchAPI
    {
        public static void setTwitchTitleAndGame(string channel, string twitchTitle, string twitchGame, string OAuthToken)
        {
            WebClient wc = new WebClient();

            wc.Headers.Set("Authorization", "OAuth " + OAuthToken);
            wc.Headers.Set("Accept", "application/vnd.twitchtv.v3+json");
            wc.Headers.Set("content-type", "application/json");
            wc.UploadDataAsync(new Uri("https://api.twitch.tv/kraken/channels/" + channel), "PUT", Encoding.Default.GetBytes("{ \"channel\": { \"status\": \"" + twitchTitle + "\", \"game\": \"" + twitchGame + "\" } }"));
        }
    }
}
