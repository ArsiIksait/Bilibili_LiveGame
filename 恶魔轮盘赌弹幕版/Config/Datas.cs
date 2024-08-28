using 恶魔轮盘赌弹幕版;

namespace Configuration;

public class Data
{
    public ProgramData ProgramData { get; set; } = new();
    public GameData GameData { get; set; } = new();
    public OpenBLiveData OpenBLiveData { get; set; } = new();
    public PlayersData PlayersData { get; set; } = new();
}

public class ProgramData
{
    public List<string> AdminOpenId { get; set; } = new();
    public int MaxWaitCount { get; set; } = 20;
}

public class GameData
{
    public string GamePath { get; set; } = string.Empty;
    public bool UseOpenGL3 { get; set; } = false;
}

public class OpenBLiveData
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string AnchorID { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
}

public class PlayersData
{
    public List<Player> Players { get; set; } = new();
}