using Newtonsoft.Json;
using OpenBLive.Client.Data;
using OpenBLive.Runtime.Data;
using OpenBLive.Runtime.Utilities;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace 恶魔轮盘赌弹幕版.Managers
{
    class ChatEventManager
    {
        public static void PlayHeartBeat_HeartBeatError(string json)
        {
            JsonConvert.DeserializeObject<EmptyInfo>(json);
            Logger.Log("心跳失败" + json);
        }

        public static void PlayHeartBeat_HeartBeatSucceed()
        {
            Logger.Log("心跳成功");
        }

        public static async void WebSocketBLiveClientOnDanmaku(Dm dm)
        {
            StringBuilder sb = new("收到弹幕!");
            sb.AppendLine();
            sb.Append("用户：");
            sb.AppendLine(dm.userName);
            sb.Append("弹幕内容：");
            sb.Append(dm.msg);
            Logger.Log(sb.ToString());

            var p = GameAutomateManager.FindPlayerDataByName(dm.userName, out int i);
            if (p != null)
            {
                Program.PlayersData[i].OpenId = dm.open_id;
                ConfigManager.SavePlayerData();
            }

            if ((dm.open_id == Program.OwnerOpenID || Program.AdminOpenId.Contains(dm.open_id)) && dm.msg == "强制关机")
            {
                DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: 强制关机");
                var game = Process.GetProcessesByName("Buckshot Roulette");

                foreach (var process in game)
                {
                    process.Kill();
                }

                Environment.Exit(0);
                return;
            }

            if (Program.Game.Runing)
            {
                DiaLog.Log("游戏正在运行");

                if ((dm.open_id == Program.OwnerOpenID || Program.AdminOpenId.Contains(dm.open_id)) && dm.msg == "重启")
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: 重启");
                    await Program.Game.RestartGame();
                    return;
                }

                if ((dm.open_id == Program.OwnerOpenID || Program.AdminOpenId.Contains(dm.open_id)) && dm.msg == "关机")
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: 关机");
                    Program.Game.StopGame();
                    return;
                }

                if ((dm.open_id == Program.OwnerOpenID || Program.AdminOpenId.Contains(dm.open_id)) && dm.msg == "下一位玩家")
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: 下一位玩家");
                    ChatCommandManager.NextPlayer();
                    return;
                }

                if ((dm.open_id == Program.OwnerOpenID || Program.AdminOpenId.Contains(dm.open_id)) && Regex.IsMatch(dm.msg, @"设置等待人数"))
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: {dm.msg}");
                    await ChatCommandManager.TrySetMaxWaitCount(dm);
                    return;
                }

                if (dm.open_id == Program.OwnerOpenID && Regex.IsMatch(dm.msg, @"添加管理"))
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: {dm.msg}");
                    await ChatCommandManager.TryAddAdmin(dm);
                    return;
                }

                if (dm.open_id == Program.OwnerOpenID && Regex.IsMatch(dm.msg, @"删除管理"))
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: {dm.msg}");
                    await ChatCommandManager.TryRemoveAdmin(dm);
                    return;
                }

                if (dm.msg == "玩家列表")
                {
                    DiaLog.Info($"玩家: {dm.userName} OpenID: {dm.open_id} 使用了命令: 玩家列表");
                    await ChatCommandManager.ShowPlayerList(dm);
                    return;
                }
                
                if (dm.msg == "积分排行")
                {
                    DiaLog.Info($"玩家: {dm.userName} OpenID: {dm.open_id} 使用了命令: 积分排行");
                    await ChatCommandManager.ShowPlayeToprList(dm);
                    return;
                }

                if (Regex.IsMatch(dm.msg, @"上机"))
                {
                    DiaLog.Info($"玩家: {dm.userName} OpenID: {dm.open_id} 使用了命令: {dm.msg}");
                    await ChatCommandManager.TryAddPlayer(dm);
                    return;
                }

                if (dm.msg == "下机")
                {
                    DiaLog.Info($"玩家: {dm.userName} OpenID: {dm.open_id} 使用了命令: 下机");
                    ChatCommandManager.TryNextPlayer(dm);
                    return;
                }

                if (dm.msg == "准备")
                {
                    DiaLog.Info($"玩家: {dm.userName} OpenID: {dm.open_id} 使用了命令: 准备");
                    ChatCommandManager.TryReady(dm);
                    return;
                }

                ChatCommandManager.TryResetActionTime(dm);
                await ChatCommandManager.TryOperatingGame(dm);
            }
            else
            {
                DiaLog.Warning("游戏已关闭");
                if (dm.open_id == Program.OwnerOpenID && dm.msg == "开机")
                {
                    DiaLog.Info($"管理员: {dm.userName} OpenID: {dm.open_id} 使用了命令: 开机");
                    Program.Game.StartGame();
                }
            }
        }

        public static async void WebSocketBLiveClientOnGiftAsync(SendGift sendGift)
        {
            StringBuilder sb = new("收到礼物!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(sendGift.userName);
            sb.Append("赠送了");
            sb.Append($"{sendGift.giftNum} ({sendGift.giftId})");
            sb.Append('个');
            sb.Append(sendGift.giftName);
            Logger.Log(sb.ToString());

            if (sendGift.paid)
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的付费礼物{sendGift.giftNum}个{sendGift.giftName}\n(ID={sendGift.giftId},Price={sendGift.price}, Paid={sendGift.paid})");
            }
            else
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的免费礼物{sendGift.giftNum}个{sendGift.giftName}\n(ID={sendGift.giftId},Price={sendGift.price}, Paid={sendGift.paid})");
                //return;
            }

            if (Program.Game.Runing)
            {
                await ChatCommandManager.GiftQueueJumping(sendGift); //礼物插队
                await ChatCommandManager.GiftShotDealer(sendGift); //修改扣除大哥1滴血
                await ChatCommandManager.GiftCigarettes(sendGift); //修改增加自己1滴血
                await ChatCommandManager.GiftHandcuff(sendGift); //修改手铐铐住大哥
                await ChatCommandManager.GiftKnife(sendGift); //修改当前散弹枪伤害*2
            }
        }

        public static async void WebSocketBLiveClientOnGuardBuy(Guard guard)
        {
            StringBuilder sb = new("收到大航海!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(guard.userInfo.userName);
            sb.Append("赠送了");
            sb.Append(guard.guardUnit);
            Logger.Log(sb.ToString());

            var p = GameAutomateManager.FindPlayerDataByOpenId(guard.userInfo.open_id, out int n);

            long money = guard.guardLevel == 1 ? 1000000000 : guard.guardLevel == 2 ? 100000000 : guard.guardLevel == 3 ? 1000000 : 1000000;

            if (p == null)
            {
                Program.PlayersData.Add(new Player(guard.userInfo.userName, guard.userInfo.open_id, money, DateTime.Now.AddMonths((int)guard.guardNum)));
            }
            else
            {
                p.AllMoney += money;
                p.GuardExpirationTime = DateTime.Now.AddMonths((int)guard.guardNum);
                Program.PlayersData[n] = p;
            }

            var guardGift = new SendGift
            {
                open_id = guard.userInfo.open_id,
                userName = guard.userInfo.userName,
                giftId = 0,
                giftName = $"大航海{(guard.guardLevel == 1 ? "总督" : guard.guardLevel == 2 ? "提督" : guard.guardLevel == 3 ? "舰长" : string.Empty)} {guard.guardNum}个月",
                giftNum = 1,
                price = money
            };

            await TipManager.ShowCommandResult($"感谢{guard.userInfo.userName} 哥上舰大航海！\n享受特殊权益: 积分增加{(guard.guardLevel == 1 ? "10亿" : guard.guardLevel == 2 ? "1亿" : guard.guardLevel == 3 ? "100万" : string.Empty)}，上机优先排队");
            await Task.Delay(4000);
            await ChatCommandManager.GiftQueueJumping(guardGift); //礼物插队
            ConfigManager.SavePlayerData();
        }

        public static void WebSocketBLiveClientOnSuperChat(SuperChat superChat)
        {
            StringBuilder sb = new("收到SC!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(superChat.userName);
            sb.Append("留言内容：");
            sb.AppendLine(superChat.message);
            sb.Append("金额：");
            sb.Append(superChat.rmb);
            sb.Append('元');
            Logger.Log(sb.ToString());
        }
    }
}