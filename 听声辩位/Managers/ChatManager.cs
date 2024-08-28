using OpenBLive.Runtime.Data;
using static 听声辩位.MainWindow;
using static 听声辩位.Managers.GameManager;

namespace 听声辩位.Managers
{
    internal class ChatManager
    {

        public static void Queuing(Dm dm, QueueStatus v)
        {
            if (v == QueueStatus.Queueing)
            {
                Instance.Announce($"{dm.userName}你已经上机了！");
                return;
            }

            if (Player.PlayerList.Count == Instance.ConfigData.ProgramData.MaxWaitCount)
            {
                Instance.Announce($"{dm.userName}上机失败！当前用户等待列表已满！");
            }
            else
            {
                Player player = new(dm.userName, dm.open_id);
                Player.SyncPlayerInfo(ref player);
                Player.PlayerList.Add(player);
                Instance.Announce($"{dm.userName}上机成功！你当前在队列中第{Player.PlayerList.Count}个！");
            }
        }
        public static void ExitQueue(Dm dm, int index, QueueStatus v)
        {
            if (v == QueueStatus.Outside)
            {
                Instance.Announce($"{dm.userName}你已经下机了！");
                return;
            }

            if (index == 0)
            {
                Instance.ExitQueue = true;
                Instance.StopMedia();
            }

            Player.PlayerList.RemoveAt(index);
            Instance.Announce($"{dm.userName}已下机！");
        }
        public static void QueryQueueStatus(Dm dm, int index, QueueStatus v)
        {
            if (v == QueueStatus.Outside)
            {
                Instance.Announce($"{dm.userName}你还没有上机呢！");
                return;
            }

            Instance.Announce($"{dm.userName}当前你正在等待列表中第{index + 1}个");
        }
        public static void GetDmUserInfo(Dm dm)
        {
            string playerInfo = Player.TryGetPlayerInfoByOpenID(dm.open_id, out var p)
                ? $"""
                            {dm.userName}您查询的信息如下:
                            我的金币数：{p.Coin}枚
                            我的连胜次数：{p.WinsStreaks}次
                            我的通关次数：{p.Pass}次
                            我的失败次数：{p.Die}次
                            我的挂机次数：{p.Afk}次
                            金币后续可兑换付费游戏道具使用
                            日常通关即可获得，开发中敬请期待
                            """
                : $"""
                            {dm.userName}
                            抱歉没有查询到您的信息
                            请先上机游玩一次
                            您的个人信息可能如下:
                            我的金币数：0枚
                            我的连胜次数：0次
                            我的通关次数：0次
                            我的失败次数：0次
                            我的挂机次数：0次
                            金币后续可兑换付费游戏道具使用
                            日常通关即可获得，开发中敬请期待
                            """;

            Instance.Announce(playerInfo, 5000);
        }
    }
}