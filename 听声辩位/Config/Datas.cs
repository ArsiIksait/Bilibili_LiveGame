using 听声辩位.Managers;

namespace Configuration;

public class Data
{
    public ProgramData ProgramData { get; set; } = new();
    public OpenBLiveData OpenBLiveData { get; set; } = new();
    public ServerChan ServerChan { get; set; } = new();
    public PlayersData PlayersData { get; set; } = new();
}

public class ProgramData
{
    public int MaxWaitCount { get; set; } = 20;
    public int Timeout { get; set; } = 30;
    public int Pass { get; set; } = 0;
    public int Die { get; set; } = 0;
}

public class OpenBLiveData
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string AnchorID { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
}

public class ServerChan
{
    public string BaseUrl { get; set; } = "https://sctapi.ftqq.com/*.send";
    public string SendKey { get; set; } = string.Empty;
}

public class PlayersData
{
    public List<Player> Players { get; set; } = new();
}