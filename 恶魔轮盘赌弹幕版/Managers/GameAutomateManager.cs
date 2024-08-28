using Compunet.YoloV8;
using Microsoft.ML.OnnxRuntime;
using 恶魔轮盘赌弹幕版.Utils;

namespace 恶魔轮盘赌弹幕版.Managers
{
    public enum ModelDetectClass
    {
        start_button,
        options_button,
        credits_button,
        exit_button,
        yes_button,
        no_button,
        retry_button,
        pills,
        door,
        heaven_door,
        contractual,
        shotgun,
        dealer,
        you,
        death_text,
        magnifier,
        beer,
        cigarettes,
        knife,
        handcuff,
        empty_item,
        money_box,
        lock_left,
        lock_right,
        item_box,
        tow_item_each,
        four_item_each,
        total_cash,
        press_any_key_to_exit,
        consume_pills,
        double_or_nothing,
        key_A,
        key_B,
        key_C,
        key_D,
        key_E,
        key_F,
        key_G,
        key_H,
        key_I,
        key_J,
        key_K,
        key_L,
        key_M,
        key_N,
        key_O,
        key_P,
        key_Q,
        key_R,
        key_S,
        key_T,
        key_U,
        key_V,
        key_W,
        key_X,
        key_Y,
        key_Z,
        key_Enter,
        key_Backspace
    }

    class GameAutomateManager
    {
        public static double TimeLeft = Program.Timeout;
        public static bool Skip = false;
        private static readonly YoloV8Predictor predictor = YoloV8Predictor.Create(@"Model/best.onnx");

        public static async Task FindContractualAndClick()
        {
            await FindTargetAndClick(ModelDetectClass.contractual, 100, startWait: 3000);
        }

        public static async Task FindDoorAndClick(int wait)
        {
            await FindTargetAndClick(ModelDetectClass.door, 100, startWait: wait, loopWait: 100);
        }

        public static async Task GameTask(Player nowPlay)
        {
            DiaLog.Log($"{nowPlay.Uname} 正在进行游戏");

            await FindTargetAndClick(ModelDetectClass.key_Enter, 100, 0);

            TimeLeft = Program.Timeout;

            int dealerDeathCount = 0;
            int addDealerDeathCount = 0;
            int eachItemCount = 0;
            bool gameStart = false;

            while (Program.Game.Runing)
            {
                var t1 = DateTime.Now;

                var nextPlayName = "无";
                if (Program.Players.Count > 1)
                {
                    var nextPlay = Program.Players[1];
                    nextPlayName = nextPlay.Uname;
                }

                if (Skip)
                {
                    Skip = false;
                    _ = Program.Game.RestartGame();
                    break;
                }

                if (TimeLeft <= 0)
                {
                    DiaLog.Log($"{nowPlay.Uname} 超时，重启游戏");
                    await TipManager.ShowCommandResult($"{nowPlay.Uname} 超时，请下一个玩家准备 玩家列表中还有 {Program.Players.Count - 1} 名玩家");
                    _ = Program.Game.RestartGame();
                    break;
                }

                TipManager.ShowTip($"当前玩家(共{Program.Players.Count}/{Program.MaxWaitCount}人): {nowPlay.Uname}({Math.Round(TimeLeft, 0)}s) 下一个玩家: {nextPlayName}");

                if (WinAPIUtils.ReadModuleBaseAddress(0x0411A0B8, out long moduleBaseAddress))
                {
                    //var myHealAddress = moduleBaseAddress + 0x68 + 0x268 + 0x18 + 0x368 + 0x10 + 0x30 + 0x68 + 0x28 + 0xF8;

                    if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress, [0x68, 0x268, 0x18, 0x368, 0x10, 0x30, 0x68, 0x28, 0xF8], out _, out long myHeal))
                    {
                        //Console.WriteLine($"===ModuleBaseAddress: {moduleBaseAddress:x}");
                        Console.WriteLine($"当前自己的血量为: {myHeal}");

                        if (!gameStart)
                        {
                            if (myHeal <= 0)
                            {
                                continue;
                            }
                            else if (myHeal > 0)
                            {
                                gameStart = true;
                            }
                        }

                        if (myHeal <= 0)
                        {
                            DiaLog.Log($"{nowPlay.Uname} 已死亡，结束游戏");
                            TipManager.ShowTip($"{nowPlay.Uname} 已失败，请下一个玩家准备 当前还有 {Program.Players.Count - 1} 名玩家");
                            await Task.Delay(5000);
                            _ = Program.Game.RestartGame();
                            break;
                        }
                    }
                }

                if (WinAPIUtils.ReadModuleBaseAddress(0x0411A028, out long moduleBaseAddress2))
                {
                    if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress2, [0x68, 0x268, 0x18, 0x368, 0x10, 0x30, 0x68, 0x28, 0x110], out _, out long dealerHeal))
                    {
                        //Console.WriteLine($"===ModuleBaseAddress: {moduleBaseAddress:x}");
                        Console.WriteLine($"当前大哥的血量为: {dealerHeal}");

                        if (dealerHeal > 0)
                        {
                            addDealerDeathCount = 1;
                        }
                        else
                        {
                            if (addDealerDeathCount > 0)
                            {
                                dealerDeathCount++;
                                addDealerDeathCount--;
                            }
                        }

                        if (dealerDeathCount == 1 && eachItemCount < 2)
                            eachItemCount = 2;

                        if (dealerDeathCount == 2 && eachItemCount < 4)
                            eachItemCount = 4;

                        if (dealerDeathCount == 1)
                        {
                            if (eachItemCount != 2)
                            {
                                eachItemCount = 2;
                                DiaLog.Log("已设置每回合拿道具数量为2个");
                            }
                        }
                        else if (dealerDeathCount == 2)
                        {
                            if (eachItemCount != 4)
                            {
                                eachItemCount = 4;
                                DiaLog.Log("已设置每回合拿道具数量为4个");
                            }
                        }
                        else if (dealerDeathCount >= 3)
                        {
                            Console.WriteLine($"已成功打败大哥3次");
                            if (FindTarget(ModelDetectClass.money_box, out int x7, out int y7, out float c7))
                            {
                                if (c7 < 0.8f)
                                    continue;
                                DiaLog.Log($"目标图像{ModelDetectClass.money_box}在 x={x7}, y={y7}");
                                DiaLog.Log($"正在为 {nowPlay.Uname} 打开钱箱");

                                await FindTargetAndClick(ModelDetectClass.lock_left, 100, 0, setWindowActive: false, findOnce: true);
                                await FindTargetAndClick(ModelDetectClass.lock_right, 100, 0, setWindowActive: false, findOnce: true);
                                await FindTargetAndClick(ModelDetectClass.money_box, 100, 0, setWindowActive: false, findOnce: true);

                                continue;
                            }

                            if (FindTarget(ModelDetectClass.press_any_key_to_exit, out int _, out int _, out float c8))
                            {
                                if (c8 < 0.6f)
                                    continue;

                                DiaLog.Log($"{nowPlay.Uname} 已成功借款，结束游戏");

                                long money = 0;

                                if (WinAPIUtils.ReadModuleBaseAddress(0x0411A028, out long moduleBaseAddress4))
                                {
                                    if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress4, [0x68, 0x138, 0x18, 0x368, 0x10, 0x30, 0x68, 0x28, 0x1E8], out _, out long drinkBeer))
                                    {
                                        if (WinAPIUtils.ReadModuleBaseAddress(0x0411A028, out long moduleBaseAddress5))
                                        {
                                            if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress5, [0x68, 0x138, 0x18, 0x368, 0x10, 0x30, 0x68, 0x28, 0x1D0], out _, out long cigaretteSmoking))
                                            {
                                                money = (long)(70000 - drinkBeer * 1.5 - cigaretteSmoking * 220);
                                            }
                                        }
                                    }
                                }

                                var p = FindPlayerDataByOpenId(nowPlay.OpenId, out int n);

                                if (p == null)
                                {
                                    Program.PlayersData.Add(new Player(nowPlay.Uname, nowPlay.OpenId, money));
                                }
                                else
                                {
                                    p.AllMoney += money;
                                    Program.PlayersData[n] = p;
                                }

                                ConfigManager.SavePlayerData();

                                TipManager.ShowTip($"恭喜B站用户: {nowPlay.Uname} 成功借款 ${money}! 请下一位玩家准备 玩家列表中还有 {Program.Players.Count - 1} 名玩家");
                                await Task.Delay(5000);
                                _ = Program.Game.RestartGame();
                                break;
                            }
                        }
                    }
                }


                if (WinAPIUtils.ReadModuleBaseAddress(0x042BFCD8, out long moduleBaseAddress3))
                {
                    if (WinAPIUtils.ReadMultilevelPointerAddress(moduleBaseAddress3, [0x80, 0x1B8, 0x0, 0x60, 0x30, 0x68, 0x28, 0xA0, 0x68, 0x28, 0x38], out _, out long takeItem))
                    {
                        //Console.WriteLine($"===ModuleBaseAddress: {moduleBaseAddress:x}");
                        Console.WriteLine($"是否是拿道具时间: {takeItem}");

                        if (takeItem == 1)
                        {
                            if (FindTarget(ModelDetectClass.item_box, out int x6, out int y6, out float c6))
                            {
                                if (c6 < 0.8f)
                                    continue;
                                DiaLog.Log($"目标图像{ModelDetectClass.item_box}在 x={x6}, y={y6}");
                                DiaLog.Log($"放置{eachItemCount}个道具");
                                for (int i = 1; i <= eachItemCount; i++)
                                {
                                    await FindTargetAndClick(ModelDetectClass.item_box, 100, 0, setWindowActive: false, retryNum: 5);
                                    await FindTargetAndClick(ModelDetectClass.empty_item, 100, 100, setWindowActive: false, retryNum: 5);
                                    await Task.Delay(100);
                                }

                                continue;
                            }
                        }
                    }
                }

                await Task.Delay(1000);
                var t2 = DateTime.Now - t1;
                TimeLeft -= t2.Seconds + t2.Milliseconds * 0.001;
            }
        }

        public static async Task FindKeyboardAndInputName(string name)
        {
            WinAPIUtils.SetGameWindowActive();

            foreach (char c in name.ToUpper())
            {
                for (int i = 0; i < 20; i++)
                {
                    var result = c switch
                    {
                        //'0' => await FindTargetAndClick(ModelDetectClass.key_Backspace, 0, 0, setWindowActive: false, findOnce: true),
                        //'1' => await FindTargetAndClick(ModelDetectClass.key_Enter, 0, 0, setWindowActive: false, findOnce: true),
                        'A' => await FindTargetAndClick(ModelDetectClass.key_A, 0, 0, setWindowActive: false, findOnce: true),
                        'B' => await FindTargetAndClick(ModelDetectClass.key_B, 0, 0, setWindowActive: false, findOnce: true),
                        'C' => await FindTargetAndClick(ModelDetectClass.key_C, 0, 0, setWindowActive: false, findOnce: true),
                        'D' => await FindTargetAndClick(ModelDetectClass.key_D, 0, 0, setWindowActive: false, findOnce: true),
                        'E' => await FindTargetAndClick(ModelDetectClass.key_E, 0, 0, setWindowActive: false, findOnce: true),
                        'F' => await FindTargetAndClick(ModelDetectClass.key_F, 0, 0, setWindowActive: false, findOnce: true),
                        'G' => await FindTargetAndClick(ModelDetectClass.key_G, 0, 0, setWindowActive: false, findOnce: true),
                        'H' => await FindTargetAndClick(ModelDetectClass.key_H, 0, 0, setWindowActive: false, findOnce: true),
                        'I' => await FindTargetAndClick(ModelDetectClass.key_I, 0, 0, setWindowActive: false, findOnce: true),
                        'J' => await FindTargetAndClick(ModelDetectClass.key_J, 0, 0, setWindowActive: false, findOnce: true),
                        'K' => await FindTargetAndClick(ModelDetectClass.key_K, 0, 0, setWindowActive: false, findOnce: true),
                        'L' => await FindTargetAndClick(ModelDetectClass.key_L, 0, 0, setWindowActive: false, findOnce: true),
                        'M' => await FindTargetAndClick(ModelDetectClass.key_M, 0, 0, setWindowActive: false, findOnce: true),
                        'N' => await FindTargetAndClick(ModelDetectClass.key_N, 0, 0, setWindowActive: false, findOnce: true),
                        'O' => await FindTargetAndClick(ModelDetectClass.key_O, 0, 0, setWindowActive: false, findOnce: true),
                        'P' => await FindTargetAndClick(ModelDetectClass.key_P, 0, 0, setWindowActive: false, findOnce: true),
                        'Q' => await FindTargetAndClick(ModelDetectClass.key_Q, 0, 0, setWindowActive: false, findOnce: true),
                        'R' => await FindTargetAndClick(ModelDetectClass.key_R, 0, 0, setWindowActive: false, findOnce: true),
                        'S' => await FindTargetAndClick(ModelDetectClass.key_S, 0, 0, setWindowActive: false, findOnce: true),
                        'T' => await FindTargetAndClick(ModelDetectClass.key_T, 0, 0, setWindowActive: false, findOnce: true),
                        'U' => await FindTargetAndClick(ModelDetectClass.key_U, 0, 0, setWindowActive: false, findOnce: true),
                        'V' => await FindTargetAndClick(ModelDetectClass.key_V, 0, 0, setWindowActive: false, findOnce: true),
                        'W' => await FindTargetAndClick(ModelDetectClass.key_W, 0, 0, setWindowActive: false, findOnce: true),
                        'X' => await FindTargetAndClick(ModelDetectClass.key_X, 0, 0, setWindowActive: false, findOnce: true),
                        'Y' => await FindTargetAndClick(ModelDetectClass.key_Y, 0, 0, setWindowActive: false, findOnce: true),
                        'Z' => await FindTargetAndClick(ModelDetectClass.key_Z, 0, 0, setWindowActive: false, findOnce: true),
                        _ => true
                    };

                    if (result)
                        break;
                }
            }
        }

        public static async Task FindStartDoorContractualAndClick()
        {
            while (true)
            {
                await FindTargetAndClick(ModelDetectClass.start_button, 100, 0, findOnce: true, setWindowActive: false);

                await FindTargetAndClick(ModelDetectClass.door, 100, 0, findOnce: true, setWindowActive: false);

                if (await FindTargetAndClick(ModelDetectClass.heaven_door, 100, 0, findOnce: true, setWindowActive: false))
                {
                    await TipManager.ShowCommandResult("到达世界最高城天塘！太美丽啦天塘，哎呀这不丁真吗？再看看远处的雪山吧家人们。", 10000);
                    await Task.Delay(3000);
                }

                if (FindTarget(ModelDetectClass.retry_button, out int _, out int _, out float _))
                {
                    while (!FindTarget(ModelDetectClass.door, out int _, out int _, out float _))
                    {
                        await FindTargetAndClick(ModelDetectClass.retry_button, 100, 0, findOnce: true, setWindowActive: false);
                        await Task.Delay(3000);
                        WinAPIUtils.SendKey(WindowsInput.Native.VirtualKeyCode.VK_R);
                    }
                }

                if (FindTarget(ModelDetectClass.contractual, out int _, out int _, out float _))
                {
                    while (!FindTarget(ModelDetectClass.key_Enter, out int _, out int _, out float _))
                    {
                        await FindTargetAndClick(ModelDetectClass.contractual, 100, 0, findOnce: true, setWindowActive: false);
                    }

                    break;
                }
            }
        }

        public static Player? FindPlayerByOpenId(string openId, out int waitingIndex)
        {
            waitingIndex = 1;

            foreach (var player in Program.Players)
            {
                if (player.OpenId == openId)
                {
                    return player;
                }

                waitingIndex++;
            }

            waitingIndex = 0;
            return null;
        }

        public static Player? FindPlayerDataByOpenId(string openId, out int index)
        {
            index = 0;

            foreach (var player in Program.PlayersData)
            {
                if (player.OpenId == openId)
                {
                    return player;
                }

                index++;
            }

            return null;
        }

        public static Player? FindPlayerDataByName(string name, out int index)
        {
            index = 0;

            foreach (var player in Program.PlayersData)
            {
                if (player.Uname == name)
                {
                    return player;
                }

                index++;
            }

            return null;
        }

        public static async Task FindShotgunAndShotDealer()
        {
            await FindTargetAndClick(ModelDetectClass.shotgun, 100, 100, setWindowActive: false);
            await FindTargetAndClick(ModelDetectClass.dealer, 100, 100, setWindowActive: false);
        }

        public static async Task FindShotgunAndShotSelf()
        {
            await FindTargetAndClick(ModelDetectClass.shotgun, 100, 100, setWindowActive: false);
            await FindTargetAndClick(ModelDetectClass.you, 100, 100, setWindowActive: false);
        }

        public static bool FindTarget(ModelDetectClass target, out int x, out int y, out float confidence)
        {
            try
            {
                var bitimg = WinAPIUtils.ScreenshotToStream();

                var imgSelector = new ImageSelector(bitimg.Bytes);

                var result = predictor.Detect(imgSelector);

                bitimg.Dispose(); // 运行试试看

                if (result != null)
                {
                    if (result.Boxes.Length > 0)
                    {
                        foreach (var item in result.Boxes)
                        {
                            if (item == null)
                                continue;

                            //Console.WriteLine($"{item.Class}: {item.Confidence} {item.Bounds} Center Point: X={item.Bounds.X + item.Bounds.Width / 2}, Y={item.Bounds.Y + item.Bounds.Height / 2}");

                            if (item.Class.Id == ((int)target))
                            {
                                x = item.Bounds.X + item.Bounds.Width / 2;
                                y = item.Bounds.Y + item.Bounds.Height / 2;
                                confidence = item.Confidence;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            x = 0;
            y = 0;
            confidence = 0;
            return false;
        }

        public static async Task<bool> FindTargetAndClick(ModelDetectClass target, int clickWait, int startWait = 1000, int loopWait = 500, bool setWindowActive = true, bool findOnce = false, bool dialog = true, int retryNum = 20)
        {
            await Task.Delay(startWait);

            if (setWindowActive)
            {
                WinAPIUtils.SetGameWindowActive();
                await Task.Delay(1000);
            }

            for (int i = 0; i < retryNum; i++)
            {
                // 找不到游戏窗口就返回
                if (!WinAPIUtils.FindGameWindow(out _))
                {
                    if (dialog)
                        DiaLog.Error($"未能成功找到目标图像 {target}，原因是未找到游戏窗口");
                    return false;
                }

                if (FindTarget(target, out int x, out int y, out float _))
                {
                    if (dialog)
                        DiaLog.Log($"目标图像{target}在 x={x}, y={y}");
                    await WinAPIUtils.MouseLifeClick(x, y);
                    return true;
                }
                else
                {
                    if (dialog)
                        DiaLog.Warning($"未找到目标图像 {target}");
                }


                if (findOnce)
                    return false;

                await Task.Delay(loopWait);
            }

            return false;
        }
    }
}