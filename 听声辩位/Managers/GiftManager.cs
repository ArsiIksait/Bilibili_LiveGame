using OpenBLive.Runtime.Data;

namespace 听声辨位.Managers
{
    internal class GiftManager
    {
        public static Dictionary<string, List<(SendGift Gift, DateTime Time)>> ExpiredGifts { get; private set; } = [];
        public static Dictionary<string, List<(SendGift Gift, DateTime Time)>> ReceivedGifts { get; private set; } = [];

        public static (SendGift Gift, long Count) GetSenderSimilarGift(string openId, string giftName)
        {
            if (ReceivedGifts.TryGetValue(openId, out var value))
            {
                var giftList = value;

                var gift = giftList.Where(v => v.Gift.giftName == giftName).First().Gift;
                var count = giftList.Where(v => v.Gift.giftName == giftName).Sum(s => s.Gift.giftNum);

                return (gift, count);
            }
            else
            {
                return (new SendGift(), 0);
            }
        }
    }
}