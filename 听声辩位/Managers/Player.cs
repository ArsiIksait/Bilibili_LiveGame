using Configuration;
using System.Diagnostics.CodeAnalysis;
using 听声辩位;

namespace 听声辩位.Managers;

public class Player
{
    public static List<Player> PlayerList = new();
    public string Uname { get; set; }
    public string OpenId { get; set; }
    public int WinsStreaks { get; set; }
    public int Pass { get; set; }
    public int Die { get; set; }
    public int Afk { get; set; }
    public int Coin { get; set; }

    public Player(string uname, string openId, int winsStreaks = 0, int pass = 0, int die = 0, int afk = 0, int coin = 0)
    {
        Uname = uname;
        OpenId = openId;
        WinsStreaks = winsStreaks;
        Pass = pass;
        Die = die;
        Afk = afk;
        Coin = coin;
    }

    public static bool TryGetPlayerInfoByOpenID(string openId, [NotNullWhen(true)] out Player? player)
    {
        var topPlayers = MainWindow.Instance.ConfigData.PlayersData.Players;

        player = topPlayers.Find(s => s.OpenId.Equals(openId));

        if (player == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public static void SyncTopPlayers(Player player)
    {
        var topPlayers = MainWindow.Instance.ConfigData.PlayersData.Players;

        if (TryGetPlayerInfoByOpenID(player.OpenId, out var p))
        {
            topPlayers.Remove(p);
            topPlayers.Add(player);
        }
        else
        {
            topPlayers.Add(player);
        }

        var sorted = topPlayers.OrderByDescending(t => t.WinsStreaks).ToList();

        var topThreePlayer = sorted.Take(3).ToList();

        MainWindow.Instance.Dispatcher.Invoke(() =>
        {
            if (topThreePlayer.Count > 0)
            {
                MainWindow.Instance.topOneName.Text = topThreePlayer[0].Uname;
                MainWindow.Instance.topOneWinsStreaks.Text = topThreePlayer[0].WinsStreaks.ToString();
            }
            else
            {
                MainWindow.Instance.topOneName.Text = "虚位以待";
                MainWindow.Instance.topOneWinsStreaks.Text = "0";
            }

            if (topThreePlayer.Count > 1)
            {
                MainWindow.Instance.topTowName.Text = topThreePlayer[1].Uname;
                MainWindow.Instance.topTowWinsStreaks.Text = topThreePlayer[1].WinsStreaks.ToString();
            }
            else
            {
                MainWindow.Instance.topTowName.Text = "虚位以待";
                MainWindow.Instance.topTowWinsStreaks.Text = "0";
            }

            if (topThreePlayer.Count > 2)
            {
                MainWindow.Instance.topThreeName.Text = topThreePlayer[2].Uname;
                MainWindow.Instance.topThreeWinsStreaks.Text = topThreePlayer[2].WinsStreaks.ToString();
            }
            else
            {
                MainWindow.Instance.topThreeName.Text = "虚位以待";
                MainWindow.Instance.topThreeWinsStreaks.Text = "0";
            }
        });

        MainWindow.Instance.ConfigData.PlayersData.Players = sorted;
        Config.SaveConfig(MainWindow.Instance.ConfigData);
    }

    public static void SyncTopPlayers()
    {
        var topPlayers = MainWindow.Instance.ConfigData.PlayersData.Players;

        var sorted = topPlayers.OrderByDescending(t => t.WinsStreaks).ToList();

        var topThreePlayer = sorted.Take(3).ToList();

        MainWindow.Instance.Dispatcher.Invoke(() =>
        {
            if (topThreePlayer.Count > 0)
            {
                MainWindow.Instance.topOneName.Text = topThreePlayer[0].Uname;
                MainWindow.Instance.topOneWinsStreaks.Text = topThreePlayer[0].WinsStreaks.ToString();
            }
            else
            {
                MainWindow.Instance.topOneName.Text = "虚位以待";
                MainWindow.Instance.topOneWinsStreaks.Text = "0";
            }

            if (topThreePlayer.Count > 1)
            {
                MainWindow.Instance.topTowName.Text = topThreePlayer[1].Uname;
                MainWindow.Instance.topTowWinsStreaks.Text = topThreePlayer[1].WinsStreaks.ToString();
            }
            else
            {
                MainWindow.Instance.topTowName.Text = "虚位以待";
                MainWindow.Instance.topTowWinsStreaks.Text = "0";
            }

            if (topThreePlayer.Count > 2)
            {
                MainWindow.Instance.topThreeName.Text = topThreePlayer[2].Uname;
                MainWindow.Instance.topThreeWinsStreaks.Text = topThreePlayer[2].WinsStreaks.ToString();
            }
            else
            {
                MainWindow.Instance.topThreeName.Text = "虚位以待";
                MainWindow.Instance.topThreeWinsStreaks.Text = "0";
            }
        });
    }

    public static void SyncPlayerInfo(ref Player player)
    {
        if (TryGetPlayerInfoByOpenID(player.OpenId, out var p))
        {
            player.WinsStreaks = p.WinsStreaks;
            player.Pass = p.Pass;
            player.Die = p.Die;
            player.Afk = p.Afk;
            player.Coin = p.Coin;
        }
    }

    public static bool TryGetFirstPlayer([NotNullWhen(true)] out Player? player)
    {
        player = null;
        if (PlayerList.Count > 0)
        {
            player = PlayerList[0];
            return true;
        }

        return false;
    }

    public static bool TryGetNextPlayer([NotNullWhen(true)] out Player? player)
    {
        player = null;
        if (PlayerList.Count >= 2)
        {
            player = PlayerList[1];
            return true;
        }

        return false;
    }

    public static bool TryGetPlayerByOpenID(string openId, [NotNullWhen(true)] out Player? player, out int index)
    {
        player = null;
        index = 0;

        foreach (var _player in PlayerList)
        {
            if (_player.OpenId == openId)
            {
                player = _player;
                return true;
            }

            index++;
        }

        return false;
    }
}
