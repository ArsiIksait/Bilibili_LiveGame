using OpenBLive.Client;
using OpenBLive.Client.Data;
using OpenBLive.Runtime;
using System.Runtime.Versioning;
using 恶魔轮盘赌弹幕版.Managers;
using 恶魔轮盘赌弹幕版.Utils;

[assembly: SupportedOSPlatform("windows")]

namespace 恶魔轮盘赌弹幕版;

class Program : WinAPIUtils
{
    public static readonly BuckshotRoulette Game = new();
    public static List<Player> Players = new();
    public static List<Player> PlayersData = new();
    public static Dictionary<Player, string> PlayerName = new();
    public static double Timeout = 120d;
    public static int ReadyTime = 20;
    public static int MaxWaitCount = 10;
    public static string OwnerOpenID = string.Empty;
    public static List<string> AdminOpenId = new();

    public static async Task Main()
    {
        try
        {
            //是否为测试环境
            BApi.isTestEnv = false;
            //access_key

            //SignUtility.accessKeyId = "xdBGmmiGG8DnrI2vLWXzYMy6";
            //access_key_secret
            //SignUtility.accessKeySecret = "RH0vqyQaAYkK5TuqKhRpMOKLfw3Um8";
            //应用id
            //var appId = "1706535988894";
            //var code = "BOA91YHS8IKG2";

            var appId = string.Empty;
            //主播身份码
            var code = string.Empty;

            (appId, code) = ConfigManager.ReadEnv();

            var startInfo = new AppStartInfo();

            WebSocketBLiveClient m_WebSocketBLiveClient;
            //获取房间信息
            startInfo = await new BApiClient().StartInteractivePlay(code, appId);
            var gameId = startInfo?.GetGameId();
            var anchorOpenId = startInfo?.Data?.AnchorInfo?.Open_id;

            if (startInfo?.Code != 0)
            {
                DiaLog.Error(startInfo?.Message ?? string.Empty);
                return;
            }

            if (string.IsNullOrEmpty(anchorOpenId))
            {
                DiaLog.Error("无法获取到主播OpenID");
            }
            else
            {
                OwnerOpenID = anchorOpenId;
            }

            if (gameId != null)
            {
                DiaLog.Info("成功开启，开始心跳，游戏ID: " + gameId);
                InteractivePlayHeartBeat m_PlayHeartBeat = new(gameId);
                m_PlayHeartBeat.HeartBeatError += ChatEventManager.PlayHeartBeat_HeartBeatError;
                m_PlayHeartBeat.HeartBeatSucceed += ChatEventManager.PlayHeartBeat_HeartBeatSucceed;
                m_PlayHeartBeat.Start();
            }
            else
            {
                DiaLog.Error("开启游戏错误: " + startInfo.ToString());
            }

            ConfigManager.SetEnv(appId, code);

            //程序退出事件
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Game.StopGame();
                var ret = new BApiClient().EndInteractivePlay(appId, gameId ?? "0");
                DiaLog.Log($"关闭游戏: AppId: {appId}, GameId: {gameId} Code: {ret.Result.Code}, Message: {ret.Result.Message}");
            };

            m_WebSocketBLiveClient = new WebSocketBLiveClient(startInfo.GetWssLink(), startInfo.GetAuthBody());
            m_WebSocketBLiveClient.OnDanmaku += ChatEventManager.WebSocketBLiveClientOnDanmaku;
            m_WebSocketBLiveClient.OnGift += ChatEventManager.WebSocketBLiveClientOnGiftAsync;
            m_WebSocketBLiveClient.OnGuardBuy += ChatEventManager.WebSocketBLiveClientOnGuardBuy;
            m_WebSocketBLiveClient.OnSuperChat += ChatEventManager.WebSocketBLiveClientOnSuperChat;

            m_WebSocketBLiveClient.Connect(TimeSpan.FromSeconds(30));

            DiaLog.Log("正在启动游戏");
            Game.StartGame();
            Thread.Sleep(-1);
        }
        catch (Exception ex)
        {
            DiaLog.Error($"程序出现错误: {ex}");
        }
    }
}