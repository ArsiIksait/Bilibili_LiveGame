using Configuration;
using OpenBLive.Client;
using OpenBLive.Client.Data;
using OpenBLive.Runtime;
using OpenBLive.Runtime.Utilities;
using System.Text;
using 听声辨位.Managers;
using static 听声辨位.Managers.GiftManager;

namespace 听声辩位.Managers
{
    internal class GameManager : MainWindow
    {
        public enum QueueStatus { Outside, Queueing }
        public static async void OpenBLiveStart()
        {
        start:
            var config = Config.ReadConfig();

            if (config == null)
            {
                for (int i = 30; i > 0; i--)
                {
                    Instance.Announce($"出现错误，无法读取配置文件\n请检查配置文件是否在被其他程序占用或者尝试以管理员权限运行本程序!\n{i}秒后尝试重连...", 980);
                    await Task.Delay(1000);
                }

                goto start;
            }

            Instance.ConfigData = config;
            Instance.TimeLeft = config.ProgramData.Timeout;
            Player.SyncTopPlayers();

            //是否为测试环境
            BApi.isTestEnv = false;
            //access_key
            SignUtility.accessKeyId = config.OpenBLiveData.AccessKeyId;
            //access_key_secret
            SignUtility.accessKeySecret = config.OpenBLiveData.AccessKeySecret;
            //应用id
            var appId = config.OpenBLiveData.AppId;
            var code = config.OpenBLiveData.AnchorID;

            var startInfo = new AppStartInfo();

            WebSocketBLiveClient m_WebSocketBLiveClient;
            //获取房间信息
            startInfo = await new BApiClient().StartInteractivePlay(code, appId);
            var gameId = startInfo?.GetGameId();
            var anchorOpenId = startInfo?.Data?.AnchorInfo?.OpenId;

            if (startInfo?.Code != 0)
            {
                //MessageBox.Show(startInfo?.Message ?? "发生错误：Code != 0");
                for (int i = 30; i > 0; i--)
                {
                    Instance.Announce($"游戏掉线，原因: {startInfo?.Message}\n{i}秒后尝试重连...", 980);
                    await Task.Delay(1000);
                }

                goto start;
            }

            if (string.IsNullOrEmpty(anchorOpenId))
            {
                Instance.Announce("警告！无法获取到主播OpenId", 10000);
            }
            /*else
            {
                //OwnerOpenID = anchorOpenId;
                //Announcement($"当前主播：{OwnerOpenID}");
            }*/

            if (gameId != null)
            {
                Instance.Announce("成功开启游戏，开始心跳，游戏ID: " + gameId);
                InteractivePlayHeartBeat m_PlayHeartBeat = new(gameId);
                m_PlayHeartBeat.Start();
            }
            else
            {
                for (int i = 30; i > 0; i--)
                {
                    Instance.Announce($"游戏掉线，原因: 开启游戏错误: {startInfo}\n{i}秒后尝试重连...", 980);
                    await Task.Delay(1000);
                }

                goto start;
            }

            m_WebSocketBLiveClient = new WebSocketBLiveClient(startInfo.GetWssLink(), startInfo.GetAuthBody());
            m_WebSocketBLiveClient.OnDanmaku += ChatEventManager.WebSocketBLiveClientOnDanmaku;
            m_WebSocketBLiveClient.OnGift += ChatEventManager.WebSocketBLiveClientOnGiftAsync;
            m_WebSocketBLiveClient.OnGuardBuy += ChatEventManager.WebSocketBLiveClientOnGuardBuy;

            m_WebSocketBLiveClient.Connect(TimeSpan.FromSeconds(30));
        }
        public static async void GameLogic()
        {
            Player? tmpPlayer = null;
            while (true)
            {
                Instance.playerCount.Text = $"{Player.PlayerList.Count}/{Instance.ConfigData.ProgramData.MaxWaitCount}";
                Instance.pass.Text = Instance.ConfigData.ProgramData.Pass.ToString();
                Instance.die.Text = Instance.ConfigData.ProgramData.Die.ToString();

                if (Player.TryGetFirstPlayer(out var player))
                {
                    Instance.nowPlayer.Text = player.Uname;

                    if (tmpPlayer != null && tmpPlayer == player)
                    {
                        if (Instance.PlaySound)
                        {
                            Instance.PlaySound = false;
                            var random = new Random().Next(5, 13);
                            var sound = MediaEx.MediaList[random];
                            Instance.NowSound = MediaEx.GetSoundPos(sound);
                            await Task.Delay(2000);
                            //Announcement($"当前方位是: {NowSound}");
                            Instance.PlayMedia(sound);
                            Instance.PlaySoundCount++;
                        }

                        if (!Instance.mediaElement.IsVisible && Instance.VideoPlayList.Count == 0)
                        {
                            Instance.TimeLeft--;
                        }

                        if (Instance.TimeLeft == 0)
                        {
                            Instance.TimeLeft = Instance.ConfigData.ProgramData.Timeout;
                            Instance.Announce($"{player.Uname}挂机该罚！被踢出了游戏！（挂机惩罚金币-5）");
                            Instance.BronzeManChallenge = 0;
                            Instance.PlaySoundCount = 0;
                            Instance.Health = 3;
                            Instance.UpdateHealthBar(3);
                            player.Afk++;
                            player.Coin -= 5;
                            Player.SyncTopPlayers(player);
                            Player.PlayerList.RemoveAt(0);
                        }
                    }
                    else if (tmpPlayer != null && tmpPlayer != player)
                    {
                        Instance.TimeLeft = Instance.ConfigData.ProgramData.Timeout;
                        tmpPlayer = player;
                        Instance.PlayMedia(MediaEx.MediaList[(int)Medias.开场]);
                        Instance.BronzeManChallenge = 0;
                        Instance.PlaySoundCount = 0;
                        Instance.Health = 3;
                        Instance.UpdateHealthBar(3);
                        Instance.PlaySound = true;
                    }
                    else
                    {
                        Instance.TimeLeft = Instance.ConfigData.ProgramData.Timeout;
                        tmpPlayer = player;
                        Instance.PlayMedia(MediaEx.MediaList[(int)Medias.开场]);
                        Instance.BronzeManChallenge = 0;
                        Instance.PlaySoundCount = 0;
                        Instance.Health = 3;
                        Instance.UpdateHealthBar(3);
                        Instance.PlaySound = true;
                    }
                }
                else
                {
                    Instance.nowPlayer.Text = "无";
                    tmpPlayer = null;
                    Instance.TimeLeft = Instance.ConfigData.ProgramData.Timeout;
                    Instance.BronzeManChallenge = 0;
                    Instance.PlaySoundCount = 0;
                    Instance.Health = 3;
                    Instance.UpdateHealthBar(3);
                }

                Instance.nextPlayer.Text = Player.TryGetNextPlayer(out var p) ? p.Uname : "无";

                Instance.timer.Text = Instance.TimeLeft.ToString();
                await Task.Delay(1000);
            }
        }
        public static async void CheckAndRemoveExpiredGifts()
        {
            List<string> removeKey = [];

            while (true)
            {
                foreach (var item in ReceivedGifts)
                {
                    var giftList = item.Value;

                    var gifts = giftList.Where(s => new TimeSpan(DateTime.Now.Ticks - s.Time.Ticks).TotalSeconds >= 60).ToList();

                    if (!string.IsNullOrWhiteSpace(Instance.ConfigData.ServerChan.SendKey))
                    {
                        if (gifts.Count > 0)
                        {
                            if (ExpiredGifts.TryGetValue(item.Key, out var value))
                            {
                                value.AddRange(gifts);
                            }
                            else
                            {
                                ExpiredGifts.Add(item.Key, gifts);
                            }
                        }
                    }

                    var count = giftList.RemoveAll(s => new TimeSpan(DateTime.Now.Ticks - s.Time.Ticks).TotalSeconds >= 60);

                    if (count > 0)
                    {
                        Instance.Announce($"移除了{count}个过期礼物");
                    }

                    if (item.Value.Count == 0)
                    {
                        removeKey.Add(item.Key);
                    }
                }

                if (removeKey.Count > 0)
                {
                    foreach (var key in removeKey)
                    {
                        ReceivedGifts.Remove(key);
                    }

                    removeKey.Clear();
                }

                if (ReceivedGifts.Count == 0 && ExpiredGifts.Count > 0 && !string.IsNullOrWhiteSpace(Instance.ConfigData.ServerChan.SendKey))
                {
                    var sb = new StringBuilder();
                    double allGiftPrice = 0;
                    long allGiftCount = 0;
                    long allGiftSenderCount = 0;
                    var _giftList = new List<(string UserName, string GiftName, string GiftIcon, long Count, double GiftAllPrice)>();
                    DateTime startTime = DateTime.Now;
                    DateTime endTime = default;

                    foreach (var item in ExpiredGifts)
                    {
                        var giftList = item.Value;

                        foreach (var gift in giftList)
                        {
                            if (gift.Time.Ticks < startTime.Ticks)
                            {
                                startTime = gift.Time;
                            }

                            if (gift.Time.Ticks > endTime.Ticks)
                            {
                                endTime = gift.Time;
                            }

                            var _gift = giftList.Where(v => v.Gift.giftName == gift.Gift.giftName).First().Gift;
                            var count = giftList.Where(v => v.Gift.giftName == gift.Gift.giftName).Sum(s => s.Gift.giftNum);
                            var giftAllPrice = _gift.price * count / 1000.0d;

                            var a = (_gift.userName, _gift.giftName, _gift.giftIcon, count, giftAllPrice);

                            if (!_giftList.Contains(a))
                            {
                                _giftList.Add(a);
                            }
                        }

                        allGiftSenderCount++;
                    }

                    sb.AppendLine("\n---\n");
                    sb.AppendLine($"# *{startTime}* --- *{endTime}* 这段时间您收到的礼物有：\n");
                    sb.AppendLine("| B站昵称 | 礼物名称 | 礼物数量 | 礼物总价值(元) |");
                    sb.AppendLine("|:-:|:-:|:-:|:-:|");

                    foreach (var item in _giftList)
                    {
                        sb.AppendLine($"| {item.UserName} | ![{item.GiftName}]({item.GiftIcon}) {item.GiftName} | {item.Count} | {item.GiftAllPrice} |");
                        allGiftPrice += item.GiftAllPrice;
                        allGiftCount += item.Count;
                    }

                    sb.AppendLine("\n---\n");
                    sb.AppendLine($"# **`总计：{allGiftSenderCount}人送礼，总共送了{allGiftCount}个礼物，全部礼物价值{allGiftPrice}元`** ");

                    ServerChanHelper.SendMessage($"[听声辩位]您收到了{allGiftSenderCount}个人送的{allGiftCount}个礼物", sb.ToString());

                    ExpiredGifts.Clear();
                }

                await Task.Delay(1000);
            }
        }
        public static async void VerifyAnswers(string answers)
        {
            Player.TryGetFirstPlayer(out var nowPlayer);

            if (answers == Instance.NowSound)
            {
                if (Instance.PlaySoundCount >= 5)
                {
                    if (Instance.BronzeManChallenge > 0)
                    {
                        Instance.Announce($"答对了！你过关（来自铜人的奖励金币+{Instance.BronzeManChallenge}）");
                    }
                    else
                    {
                        Instance.Announce("答对了！你过关（过关奖励金币+1）");
                        Instance.BronzeManChallenge = 1;
                    }

                    Instance.PlayMedia(MediaEx.MediaList[(int)Medias.你过关]);
                    Instance.ConfigData.ProgramData.Pass++;
                    nowPlayer!.Pass++;
                    nowPlayer.WinsStreaks++;
                    nowPlayer.Coin += Instance.BronzeManChallenge;
                    Player.SyncTopPlayers(nowPlayer);
                    Player.PlayerList.RemoveAt(0);
                }
                else
                {
                    Instance.Announce("答对了！下一题");
                }
            }
            else
            {
                switch (Instance.Health)
                {
                    case 3:
                        Instance.PlayMedia(MediaEx.MediaList[(int)Medias.该罚]);
                        Instance.Announce("答错了！该罚！");
                        if (Instance.PlaySoundCount >= 5)
                        {
                            Instance.Announce("铜人见你骨骼惊奇连对4题，甚是欣喜\n如果你下次能答对，奖励你3金币");
                            Instance.BronzeManChallenge = 3;
                        }
                        break;
                    case 2:
                        Instance.PlayMedia(MediaEx.MediaList[(int)Medias.惩罚]);
                        Instance.Announce("答错了！再罚！");
                        if (Instance.PlaySoundCount >= 5)
                        {
                            Instance.Announce("铜人想再给你一次机会\n如果你下次能答对，奖励你2金币");
                            Instance.BronzeManChallenge = 2;
                        }
                        break;
                    case 1:
                        Instance.PlayMedia(MediaEx.MediaList[(int)Medias.失败]);
                        Instance.Announce("答错了！你失败了！");

                        if (Instance.PlaySoundCount >= 5)
                        {
                            Instance.Announce("铜人对你很失望");
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.你过关]);
                            Instance.Announce("但是铜人见你闯关努力！算你过关了！（彩蛋过关奖励金币+1）");
                            Instance.ConfigData.ProgramData.Pass++;
                            nowPlayer!.Pass++;
                            nowPlayer.WinsStreaks++;
                            nowPlayer.Coin++;
                            Player.SyncTopPlayers(nowPlayer);
                        }
                        else
                        {
                            Instance.ConfigData.ProgramData.Die++;
                            nowPlayer!.Die++;
                            nowPlayer.WinsStreaks = 0;
                            Player.SyncTopPlayers(nowPlayer);
                        }

                        Player.PlayerList.RemoveAt(0);
                        return;
                }

                Instance.Health--;

            }

            Instance.UpdateHealthBar(Instance.Health);
            await Task.Delay(2000);
            Instance.PlaySound = true;
        }
    }
}