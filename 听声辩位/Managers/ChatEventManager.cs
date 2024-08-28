using OpenBLive.Runtime.Data;
using 听声辩位;
using 听声辩位.Managers;
using static 听声辨位.Managers.ItemShopManager;
using static 听声辩位.MainWindow;
using static 听声辩位.Managers.ChatManager;
using static 听声辩位.Managers.GameManager;

namespace 听声辨位.Managers
{
    internal class ChatEventManager : GiftManager
    {
        public static void WebSocketBLiveClientOnDanmaku(Dm dm)
        {
            /*StringBuilder sb = new("收到弹幕!");
            sb.AppendLine();
            sb.Append("用户：");
            sb.AppendLine(dm.userName);
            sb.Append("弹幕内容：");
            sb.Append(dm.msg);
            MessageBox.Show(sb.ToString());*/

            //MainWindow.Instance.Announcement($"{dm.userName}说：{dm.msg}");

            if (dm.msg == "道具商店")
            {
                string itemShop = $"""
                    {dm.userName}当前道具商店中有:
                    [名称] [介绍] [金币价格/指定礼物]
                    声波雷达 查看答案一次 1金币/1人气票
                    插队卡 插队到第2名，人满也可以 10金币/1粉丝牌灯牌
                    发送对应道具名称即可用金币兑换使用
                    如果您有好想法可以私信告诉我，采纳者会收到50金币
                    """;
                Instance.Announce(itemShop, 5000);

                return;
            }

            if (Player.TryGetPlayerByOpenID(dm.open_id, out _, out int index))
            {
                //当前用户已经在等待列表中了
                switch (dm.msg)
                {
                    case "上机":
                        Queuing(dm, QueueStatus.Queueing);
                        return;
                    case "下机":
                        ExitQueue(dm, index, QueueStatus.Queueing);
                        return;
                    case "啥时到我":
                        QueryQueueStatus(dm, index, QueueStatus.Queueing);
                        return;
                    case "我的信息":
                        GetDmUserInfo(dm);
                        return;
                    case "插队卡":
                        InsertQueue(dm.open_id, dm.userName, PurchaseMethod.Coin);
                        return;
                }

                if (index == 0)
                {
                    Instance.TimeLeft = Instance.ConfigData.ProgramData.Timeout;

                    if (Instance.PlaySound)
                        return;
                    if (Instance.mediaElement.IsVisible)
                        return;

                    //当前用户正在游戏中
                    switch (dm.msg)
                    {
                        case "声波雷达":
                            AcousticRadar(dm.open_id, dm.userName, PurchaseMethod.Coin);
                            return;
                        case "听不清楚":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.听不清楚]);
                            Instance.Announce($"{dm.userName}听不清楚，铜人很生气！该罚！");
                            VerifyAnswers("听不清楚");
                            return;
                        case "东" or "东方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声东方]);
                            VerifyAnswers("东方");
                            return;
                        case "南" or "南方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声南方]);
                            VerifyAnswers("南方");
                            return;
                        case "西" or "西方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声西方]);
                            VerifyAnswers("西方");
                            return;
                        case "北" or "北方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声北方]);
                            VerifyAnswers("北方");
                            return;
                        case "东北" or "东北方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声东北方]);
                            VerifyAnswers("东北方");
                            return;
                        case "西北" or "西北方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声西北方]);
                            VerifyAnswers("西北方");
                            return;
                        case "东南" or "东南方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声东南方]);
                            VerifyAnswers("东南方");
                            return;
                        case "西南" or "西南方":
                            Instance.PlayMedia(MediaEx.MediaList[(int)Medias.人声西南方]);
                            VerifyAnswers("西南方");
                            return;
                    }
                }
            }
            else
            {
                //当前用户没有在等待列表中
                switch (dm.msg)
                {
                    case "上机":
                        Queuing(dm, QueueStatus.Outside);
                        return;
                    case "下机":
                        ExitQueue(dm, 0, QueueStatus.Outside);
                        return;
                    case "啥时到我":
                        QueryQueueStatus(dm, 0, QueueStatus.Outside);
                        return;
                    case "我的信息":
                        GetDmUserInfo(dm);
                        return;
                    case "插队卡":
                        InsertQueue(dm.open_id, dm.userName, PurchaseMethod.Coin);
                        return;
                }
            }
        }
        public static void WebSocketBLiveClientOnGiftAsync(SendGift sendGift)
        {
            /*StringBuilder sb = new("收到礼物!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(sendGift.userName);
            sb.Append("赠送了");
            sb.Append($"{sendGift.giftNum} ({sendGift.giftId})");
            sb.Append('个');
            sb.Append(sendGift.giftName);*/

            if (ReceivedGifts.TryGetValue(sendGift.open_id, out var values))
            {
                values.Add((sendGift, DateTime.Now));
            }
            else
            {
                ReceivedGifts.Add(sendGift.open_id, [(sendGift, DateTime.Now)]);
            }

            var gift = GetSenderSimilarGift(sendGift.open_id, sendGift.giftName);

            Instance.Announce($"感谢{sendGift.userName}赠送的{gift.Count}个{gift.Gift.giftName}！", 980);

            switch (sendGift.giftName)
            {
                case "人气票":
                    AcousticRadar(sendGift.open_id, sendGift.userName, PurchaseMethod.Gift);
                    break;
                case "粉丝牌灯牌":
                    InsertQueue(sendGift.open_id, sendGift.userName, PurchaseMethod.Gift);
                    break;
            }
        }
        public static void WebSocketBLiveClientOnGuardBuy(Guard guard)
        {
            /*StringBuilder sb = new("收到大航海!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(guard.userInfo.userName);
            sb.Append("赠送了");
            sb.Append(guard.guardUnit);
            MessageBox.Show(sb.ToString());*/

            Instance.Announce($"感谢{guard.userInfo.userName}赠送的{guard.guardNum}个月{(guard.guardLevel == 1 ? "总督" : guard.guardLevel == 2 ? "提督" : guard.guardLevel == 3 ? "舰长" : "大航海")}！");
        }
    }
}