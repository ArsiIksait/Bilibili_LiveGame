using 听声辩位;
using 听声辩位.Managers;

namespace 听声辨位.Managers
{
    internal class ItemShopManager
    {
        public enum PurchaseMethod { Coin, Gift }
        public static void AcousticRadar(string openId, string username, PurchaseMethod v)
        {
            if (v == PurchaseMethod.Coin)
            {
                if (Player.TryGetPlayerInfoByOpenID(openId, out var player))
                {
                    if (player!.Coin > 0)
                    {
                        MainWindow.Instance.Announce($"{username}成功使用声波雷达\n当前方位是：{MainWindow.Instance.NowSound}", 5000);
                        player.Coin--;
                        Player.SyncTopPlayers(player);
                    }
                    else
                    {
                        MainWindow.Instance.Announce($"{username}您无法使用声波雷达，金币不足");
                    }
                }
                else
                {
                    MainWindow.Instance.Announce($"{username}您无法使用声波雷达，金币不足");
                }

                return;
            }

            MainWindow.Instance.Announce($"{username}成功使用声波雷达\n当前方位是：{MainWindow.Instance.NowSound}", 5000);
        }
        public static void InsertQueue(string openId, string userName, PurchaseMethod v)
        {
            if (Player.TryGetPlayerByOpenID(openId, out var player, out var index))
            {
                if (v == PurchaseMethod.Coin)
                {
                    if (index != 1 && index != 0)
                    {
                        if (player.Coin >= 10)
                        {
                            player.Coin -= 10;
                            Player.SyncTopPlayers(player);
                            Player.PlayerList.Remove(player);
                            Player.PlayerList.Insert(1, player);
                            MainWindow.Instance.Announce($"{userName}成功使用插队卡，已为您自动上机！");
                        }
                        else
                        {
                            MainWindow.Instance.Announce($"{userName}您无法使用插队卡，金币不足");
                        }
                    }
                    else
                    {
                        MainWindow.Instance.Announce($"{userName}重复使用插队卡无效\n已取消扣除金币");
                    }

                    return;
                }

                if (index != 1 && index != 0)
                {
                    Player.PlayerList.Remove(player);
                    Player.PlayerList.Insert(1, player);
                    MainWindow.Instance.Announce($"{userName}成功使用插队卡");
                }
                else
                {
                    MainWindow.Instance.Announce($"{userName}重复送礼无效");
                }
            }
            else
            {
                Player p = new(userName, openId);
                Player.SyncPlayerInfo(ref p);
                Player.PlayerList.Remove(p);
                Player.PlayerList.Insert(1, p);
                MainWindow.Instance.Announce($"{userName}成功使用插队卡，已为您自动上机！");
            }
        }
    }
}