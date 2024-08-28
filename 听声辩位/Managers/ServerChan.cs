using System.Net.Http;
using System.Net.Http.Json;

namespace 听声辩位.Managers
{
    public class ServerChanHelper
    {
        private static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri(GetBaseUrl()),
        };

        public static async void SendMessage(string title, string content)
        {
            var msg = new ServerChanMessage(title, content);
            var jc = JsonContent.Create(msg);
            _ = await sharedClient.PostAsync(GetBaseUrl(), jc);
        }

        private static string GetBaseUrl()
        {
            return MainWindow.Instance.ConfigData.ServerChan.BaseUrl.Replace("*", MainWindow.Instance.ConfigData.ServerChan.SendKey);
        }
    }

    class ServerChanMessage
    {
        public ServerChanMessage(string title, string desp)
        {
            Title = title;
            Desp = desp;
        }

        public string Title { get; set; } = string.Empty;
        public string Desp { get; set; } = string.Empty;
    }
}
