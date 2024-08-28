namespace 恶魔轮盘赌弹幕版;

public class Player
{
    public string Uname { get; set; }
    public string OpenId { get; set; }
    public long AllMoney { get; set; }
    public DateTime GuardExpirationTime { get; set; }

    public Player(string uname, string openId, long allMoney = 0, DateTime guardExpirationTime = default)
    {
        Uname = uname;
        OpenId = openId;
        AllMoney = allMoney;
        GuardExpirationTime = guardExpirationTime;
    }
}
