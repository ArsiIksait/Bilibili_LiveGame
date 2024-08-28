using OpenBLive.Runtime.Data;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using 恶魔轮盘赌弹幕版.Utils;

namespace 恶魔轮盘赌弹幕版.Managers
{
    class ChatCommandManager
    {
        public static void NextPlayer()
        {
            if (Program.Players.Count == 0)
                return;
            var nowPlay = Program.Players[0];
            //Program.PlayerName.Remove(nowPlay);
            GameAutomateManager.Skip = true;
            DiaLog.Log($"已跳过 {nowPlay.Uname}，让下一个玩家游戏");
            _ = TipManager.ShowCommandResult($"主播已使用命令跳过 {nowPlay.Uname}，让下一个玩家游戏");
        }

        public static async Task ShowPlayerList(Dm dm)
        {
            DiaLog.Log($"当前玩家列表有(Players: {Program.Players.Count} PlayerName:{Program.PlayerName.Count}):");
            var sb = new StringBuilder($"{dm.userName} ({dm.open_id}) 查询了玩家列表:\n");
            int count = 1;
            foreach (var item in Program.Players)
            {
                try
                {
                    var name = Program.PlayerName[item];
                    var p = GameAutomateManager.FindPlayerByOpenId(item.OpenId, out int _);
                    if (p == null)
                    {
                        sb.AppendLine($"{count}.[萌新] {item.Uname}{(name == "Sakura" ? string.Empty : " 游戏昵称: " + name)}");
                    }
                    else
                    {
                        var time = p.GuardExpirationTime - DateTime.Now;
                        sb.AppendLine($"{count}.{(time.TotalSeconds > 0 ? "[大航海成员]" : string.Empty)}{item.Uname}{(name == "Sakura" ? string.Empty : " 游戏昵称: " + name)} 拥有${p.AllMoney}积分");
                    }

                    DiaLog.Log($"{item.Uname}: {name} {item.OpenId}");
                }
                catch (Exception ex)
                {
                    DiaLog.Error($"在读取玩家列表时出错: {ex}");
                }
                count++;
            }

            await TipManager.ShowCommandResult(sb.ToString(), 5000);
        }

        public static async Task ShowPlayeToprList(Dm dm)
        {
            DiaLog.Log($"当前玩家排行榜列表有(Players: {Program.Players.Count} PlayerName:{Program.PlayerName.Count}):");
            var sb = new StringBuilder($"{dm.userName} ({dm.open_id}) 查询了积分排行榜:\n");
            int count = 1;
            var playerTop = Program.PlayersData.OrderByDescending(p => p.AllMoney).ToList();

            if (playerTop == null || playerTop.Count == 0)
            {
                sb.AppendLine("1.虚位以待");
                sb.AppendLine("2.虚位以待");
                sb.AppendLine("3.虚位以待");
            }
            else
            {
                foreach (var item in playerTop)
                {
                    try
                    {
                        var time = item.GuardExpirationTime - DateTime.Now;
                        sb.AppendLine($"{count}.{(time.TotalSeconds > 0 ? "[大航海成员]" : string.Empty)}{item.Uname} ${item.AllMoney} 积分");

                        DiaLog.Log($"{item.Uname}: {item.OpenId}");
                    }
                    catch (Exception ex)
                    {
                        DiaLog.Error($"在读取玩家数据列表时出错: {ex}");
                    }
                    count++;
                }
            }

            await TipManager.ShowCommandResult(sb.ToString(), 5000);
        }

        public static async Task TryAddPlayer(Dm dm)
        {
            if (Program.Players.Count >= Program.MaxWaitCount)
            {
                await Console.Out.WriteLineAsync($"{dm.userName} 预约上机失败，等待列表已满");
                await TipManager.ShowCommandResult($"{dm.userName} 预约上机失败，等待列表已满");
                return;
            }

            var p = GameAutomateManager.FindPlayerByOpenId(dm.open_id, out int n);

            if (p != null)
            {
                await Console.Out.WriteLineAsync($"{dm.userName} 预约上机失败，已经预约过了: Name {p.Uname} OpenID {p.OpenId}");
                await TipManager.ShowCommandResult($"{dm.userName} 预约上机失败，已经预约过了，您当前正在第 {n} 个");
                return;
            }

            var tmp_player = new Player(dm.userName, dm.open_id);
            var name = dm.msg.Replace("上机", "").Replace(" ", "");

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Sakura";
            }
            else if (name.ToUpper() == "GOD")
            {
                await TipManager.ShowCommandResult($"{dm.userName}设置的名字{name}无法使用，原因: 这个名字是上帝的名字，上帝已经嗝屁了");
                return;
            }
            else if (name.ToUpper() == "DEALER")
            {
                await TipManager.ShowCommandResult($"{dm.userName}设置的名字{name}无法使用，原因: 这个名字是大哥的名字，你不能用");
                return;
            }
            else if (!Regex.IsMatch(name, @"^[a-zA-Z]+$"))
            {
                await TipManager.ShowCommandResult($"{dm.userName}设置的名字{name}无法使用，原因: 名字必须是纯英文字母组成的");
                return;
            }

            Program.PlayerName.Add(tmp_player, name);
            Program.Players.Add(tmp_player);

            await Console.Out.WriteLineAsync($"{tmp_player.Uname} 预约上机成功，当前是第 {Program.Players.Count} 个");
            await TipManager.ShowCommandResult($"{tmp_player.Uname}({(name == "Sakura" ? "默认昵称" : name)}) 预约普通模式上机成功，当前是第 {Program.Players.Count} 个");
        }

        public static void TryNextPlayer(Dm dm)
        {
            if (Program.Players.Count == 0)
                return;

            var p = GameAutomateManager.FindPlayerByOpenId(dm.open_id, out int n);

            if (p == null)
                return;

            if (n == 1)
            {
                GameAutomateManager.Skip = true;
            }
            else if (n > 1)
            {
                Program.Players.Remove(p);
                Program.PlayerName.Remove(p);
            }

            DiaLog.Log($"{p.Uname}已主动下机，请下一个玩家准备");
            _ = TipManager.ShowCommandResult($"{p.Uname}已主动下机，请下一个玩家准备");
        }

        public static void TryReady(Dm dm)
        {
            if (Program.Players.Count == 0)
                return;

            var nowPlay = Program.Players[0];

            if (nowPlay.OpenId != dm.open_id)
                return;

            if (BuckshotRoulette.ReadyTime <= 0)
            {
                _ = TipManager.ShowCommandResult($"对不起{nowPlay.Uname}，您已超时，直播间有延迟，请提前5秒发送准备");
                return;
            }

            BuckshotRoulette.IsReady = true;
            DiaLog.Log($"{nowPlay.Uname}已准备");
            _ = TipManager.ShowCommandResult($"{nowPlay.Uname}已准备开始游戏");
        }

        public static void TryResetActionTime(Dm dm)
        {
            var player = GameAutomateManager.FindPlayerByOpenId(dm.open_id, out _);
            if (player != null)
            {
                if (Program.Players[0].OpenId == player.OpenId)
                {
                    GameAutomateManager.TimeLeft = Program.Timeout;
                    DiaLog.Log($"已为 {dm.userName} 重置了时间，剩余时间: {GameAutomateManager.TimeLeft}");
                }
            }
        }

        public static async Task TryOperatingGame(Dm dm)
        {
            if (Program.Players.Count == 0)
                return;

            if (GameAutomateManager.FindPlayerByOpenId(dm.open_id, out _) == null)
                return;

            if (Program.Players[0].OpenId == dm.open_id)
            {
                switch (dm.msg)
                {
                    case "打自己" or "0" or "赞":
                        await GameAutomateManager.FindShotgunAndShotSelf();
                        break;
                    case "打大哥" or "打恶魔" or "9" or "妙啊":
                        await GameAutomateManager.FindShotgunAndShotDealer();
                        break;
                    case "啤酒" or "1" or "干杯":
                        await GameAutomateManager.FindTargetAndClick(ModelDetectClass.beer, 300, 0, findOnce: true);
                        break;
                    case "香烟" or "2" or "2333":
                        await GameAutomateManager.FindTargetAndClick(ModelDetectClass.cigarettes, 300, 0, findOnce: true);
                        break;
                    case "放大镜" or "3" or "打call":
                        await GameAutomateManager.FindTargetAndClick(ModelDetectClass.magnifier, 300, 0, findOnce: true);
                        break;
                    case "刀子" or "4" or "多谢款待":
                        await GameAutomateManager.FindTargetAndClick(ModelDetectClass.knife, 300, 0, findOnce: true);
                        break;
                    case "手铐" or "5" or "awsl":
                        await GameAutomateManager.FindTargetAndClick(ModelDetectClass.handcuff, 300, 0, findOnce: true);

                        if (WinAPIUtils.ReadModuleBaseAddress(0x0411A028, out long moduleBaseAddress))
                        {
                            if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress, [0x68, 0x268, 0x18, 0x3B0, 0x368, 0x10, 0x30, 0x68, 0x28, 0x488], out _, out long value))
                            {
                                if (value == 1)
                                    await TipManager.ShowCommandResult("提示: 大哥已经被手铐铐住啦，不能再使用手铐了");
                            }
                        }
                        break;
                }
            }
        }

        public static async Task TryAddAdmin(Dm dm)
        {
            var openId = dm.msg.Replace("添加管理", "").Replace(" ", "");

            if (!string.IsNullOrWhiteSpace(openId))
            {
                if (Program.AdminOpenId.Contains(openId))
                {
                    await TipManager.ShowCommandResult($"用户{openId}已经是管理员了");
                }
                else
                {
                    Program.AdminOpenId.Add(openId);
                    ConfigManager.SaveAdminOpenId();
                    await TipManager.ShowCommandResult($"成功添加管理员{openId}");
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"无法添加管理员{openId}，请检查是否输入错误，例: 添加管理+OpenId");
            }
        }

        public static async Task TryRemoveAdmin(Dm dm)
        {
            var openId = dm.msg.Replace("删除管理", "").Replace(" ", "");

            if (!string.IsNullOrWhiteSpace(openId))
            {
                if (Program.AdminOpenId.Contains(openId))
                {
                    await TipManager.ShowCommandResult($"用户{openId}已经不是管理员了");
                }
                else
                {
                    Program.AdminOpenId.Remove(openId);
                    ConfigManager.SaveAdminOpenId();
                    await TipManager.ShowCommandResult($"成功删除管理员{openId}");
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"无法删除管理员{openId}，请检查是否输入错误，例: 删除管理+UID");
            }
        }

        public static async Task TrySetMaxWaitCount(Dm dm)
        {
            var count = dm.msg.Replace("设置等待人数", "").Replace(" ", "");
            if (int.TryParse(count, out int waitCount))
            {
                if (waitCount > 0)
                {
                    await TipManager.ShowCommandResult($"成功设置最大等待人数为{waitCount}");
                    ConfigManager.SaveMaxWaitCount(waitCount);
                    Program.MaxWaitCount = waitCount;
                }
                else
                {
                    await TipManager.ShowCommandResult($"无法设置最大等待人数{waitCount}，请检查是否输入错误，例: 设置等待人数+数字");
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"无法设置最大等待人数{waitCount}，请检查是否输入错误，例: 设置等待人数+数字");
            }
        }

        public static async Task GiftQueueJumping(SendGift sendGift)
        {
            //赠送的是否是粉丝牌灯牌，或者礼物价值是否小于1元
            if (sendGift.giftId != 31164 && sendGift.giftNum * sendGift.price < 1000)
                return;

            var player = GameAutomateManager.FindPlayerByOpenId(sendGift.open_id, out int index);
            if (player != null)
            {
                try
                {
                    if (index > 2)
                    {
                        await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n已安排您下一个游玩");
                        Program.Players.Remove(player);
                        Program.Players.Insert(1, player);
                    }
                    else if (index == 2)
                    {
                        await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n您已经优先游玩了,重复送礼无效");
                    }
                }
                catch (Exception ex)
                {
                    await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，插队失败，请联系主播\n原因: {ex.Message}");
                    DiaLog.Error($"在送礼插队时出错: {ex}");
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n您需要先发送弹幕上机才能使用送礼插队功能哦");
            }
        }

        public static async Task GiftShotDealer(SendGift sendGift)
        {
            //赠送的是否是5个小花花
            if (sendGift.giftId != 31036 && sendGift.giftNum < 5)
                return;

            var player = GameAutomateManager.FindPlayerByOpenId(sendGift.open_id, out int index);
            if (player != null)
            {
                if (index == 1)
                {
                    var useCount = sendGift.giftNum / 5;
                    try
                    {
                        if (WinAPIUtils.ReadModuleBaseAddress(0x0411A028, out long moduleBaseAddress))
                        {
                            if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress, [0x68, 0x268, 0x18, 0x368, 0x10, 0x30, 0x68, 0x28, 0x110], out long address, out long dealerHeal))
                            {
                                if (WinAPIUtils.WriteMemory(address, dealerHeal - useCount))
                                {
                                    await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n扣血道具*{useCount}使用成功！当前大哥血量: {dealerHeal - useCount}");
                                    return;
                                }
                            }
                        }

                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用扣血道具失败，请联系主播\n原因: 内存读写失败");
                    }
                    catch (Exception ex)
                    {
                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用扣血道具失败，请联系主播\n原因: {ex.Message}");
                        DiaLog.Error($"在使用扣血道具时出错: {ex}");
                    }
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n您需要先发送弹幕上机才能使用扣血道具功能哦");
            }
        }

        public static async Task GiftCigarettes(SendGift sendGift)
        {
            //赠送的是否是5个牛蛙牛蛙
            if (sendGift.giftId != 31039 && sendGift.giftNum < 5)
                return;

            var player = GameAutomateManager.FindPlayerByOpenId(sendGift.open_id, out int index);
            if (player != null)
            {
                if (index == 1)
                {
                    var useCount = sendGift.giftNum / 5;
                    try
                    {
                        if (WinAPIUtils.ReadModuleBaseAddress(0x0411A0B8, out long moduleBaseAddress))
                        {
                            if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress, [0x68, 0x268, 0x18, 0x368, 0x10, 0x30, 0x68, 0x28, 0xF8], out long address, out long myHeal))
                            {
                                if (WinAPIUtils.WriteMemory(address, myHeal + useCount))
                                {
                                    await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n加血道具*{useCount}使用成功！您当前的血量: {myHeal + useCount}");
                                    return;
                                }
                            }
                        }

                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用加血道具失败，请联系主播\n原因: 内存读写失败");
                    }
                    catch (Exception ex)
                    {
                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用加血道具失败，请联系主播\n原因: {ex.Message}");
                        DiaLog.Error($"在使用加血道具时出错: {ex}");
                    }
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n您需要先发送弹幕上机才能使用加血道具功能哦");
            }
        }

        public static async Task GiftHandcuff(SendGift sendGift)
        {
            //赠送的是否是打call
            if (sendGift.giftId != 31037)
                return;

            var player = GameAutomateManager.FindPlayerByOpenId(sendGift.open_id, out int index);
            if (player != null)
            {
                if (index == 1)
                {
                    try
                    {
                        if (WinAPIUtils.ReadModuleBaseAddress(0x0411A028, out long moduleBaseAddress))
                        {
                            if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress, [0x68, 0x268, 0x18, 0x3B0, 0x368, 0x10, 0x30, 0x68, 0x28, 0x488], out long address, out _))
                            {
                                if (WinAPIUtils.WriteMemory(address, 1))
                                {
                                    await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n手铐道具使用成功！限制大哥行动2回合\n重复使用无效，如果使用失败，请轮到自己的回合再使用");
                                    return;
                                }
                            }
                        }

                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用手铐道具失败，请联系主播\n原因: 内存读写失败");
                    }
                    catch (Exception ex)
                    {
                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用手铐道具失败，请联系主播\n原因: {ex.Message}");
                        DiaLog.Error($"在使用手铐道具时出错: {ex}");
                    }
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n您需要先发送弹幕上机才能使用手铐功能哦");
            }
        }

        public static async Task GiftKnife(SendGift sendGift)
        {
            //赠送的是否是这个好诶
            if (sendGift.giftId != 30758)
                return;

            var player = GameAutomateManager.FindPlayerByOpenId(sendGift.open_id, out int index);
            if (player != null)
            {
                if (index == 1)
                {
                    var useCount = sendGift.giftNum / 5;
                    try
                    {
                        if (WinAPIUtils.ReadModuleBaseAddress(0x04133260, out long moduleBaseAddress))
                        {
                            if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress, [0x378, 0x178, 0x0, 0x178, 0x20, 0x68, 0x28, 0x440], out long address, out long damage))
                            {
                                if (WinAPIUtils.WriteMemory(address, damage * useCount * 2))
                                {
                                    await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n超级加倍道具*{useCount}使用成功！当前散弹枪伤害: {damage * useCount * 2}\n提示：最好一击干掉大哥，否则大哥也会用这把{damage * useCount * 2}伤害的枪打你");
                                    return;
                                }
                            }
                        }

                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用超级加倍道具失败，请联系主播\n原因: 内存读写失败");
                    }
                    catch (Exception ex)
                    {
                        await TipManager.ShowCommandResult($"抱歉{sendGift.userName}，出了一点小问题，使用超级加倍道具失败，请联系主播\n原因: {ex.Message}");
                        DiaLog.Error($"在使用超级加倍道具时出错: {ex}");
                    }
                }
            }
            else
            {
                await TipManager.ShowCommandResult($"感谢{sendGift.userName}赠送的{sendGift.giftNum}个{sendGift.giftName}\n您需要先发送弹幕上机才能使用超级加倍道具功能哦");
            }
        }
    }
}