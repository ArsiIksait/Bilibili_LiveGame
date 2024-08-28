using System.Diagnostics;
using WindowsInput;
using WindowsInput.Native;
using 恶魔轮盘赌弹幕版.Managers;
using 恶魔轮盘赌弹幕版.Utils;

namespace 恶魔轮盘赌弹幕版
{
    class BuckshotRoulette
    {
        private bool Start = false;
        private bool Stop = false;
        private static int StartCount = 0;

        private readonly Process process = new();
        public static string GamePath = string.Empty;
        public static bool UseOpenGL = false;
        public static bool IsReady = false;
        public static int ReadyTime = Program.ReadyTime;
        public static bool GameReady = false;
        public InputSimulator InputSimulator = new();

        public void StartGame()
        {
            StartCount++;

            if (StartCount > 1)
            {
                StartCount--;
                return;
            }

            Task.Run(async () =>
            {
                await TipManager.ShowCommandResult(string.Empty);

                while (true)
                {
                    TipManager.ShowTip("正在重启游戏，如果卡在这里请联系我重启游戏");
                    process.StartInfo.FileName = GamePath;
                    if (UseOpenGL)
                        process.StartInfo.Arguments = "--rendering-driver opengl3";
                    process.Start();
                    Start = true;

                    await GameAutomateManager.FindStartDoorContractualAndClick();
                    GameReady = true;
                    //await GameAutomateManager.FindKeyboardAndInputName("Sakura");

                    // 启动一个线程循环判断当前玩家列表是否有人，有人就按下回车键开始游戏，没有人就继续等待
                    _ = Task.Run(async () =>
                    {
                        while (Runing)
                        {
                            //判断是否有玩家上机，有的话就开始游戏
                            if (Program.Players.Count > 0)
                            {
                                var nowPlay = Program.Players[0];
                                ReadyTime = Program.ReadyTime;

                                while (true)
                                {
                                    var p1 = GameAutomateManager.FindPlayerByOpenId(nowPlay.OpenId, out int n1);

                                    if (p1 == null || n1 != 1 || GameAutomateManager.Skip)
                                        break;

                                    if (!GameReady)
                                    {
                                        TipManager.ShowTip($"正在重启游戏，下一位玩家: {nowPlay.Uname}，如果卡在这里请联系我重启游戏");
                                        await Task.Delay(100);
                                        continue;
                                    }

                                    if (ReadyTime > 0)
                                    {
                                        if (IsReady)
                                            break;
                                        TipManager.ShowTip($"请玩家 {nowPlay.Uname} 发送准备来开始游戏 ({ReadyTime}s)");
                                        await Task.Delay(1000);
                                        ReadyTime--;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                var p2 = GameAutomateManager.FindPlayerByOpenId(nowPlay.OpenId, out int n2);

                                if (p2 == null || n2 != 1 || GameAutomateManager.Skip)
                                {
                                    TipManager.ShowTip($"玩家列表中找不到{nowPlay.Uname}或者该玩家已下机 已取消准备，请下一位玩家准备");
                                    await Task.Delay(3000);
                                    IsReady = false;

                                    if (GameAutomateManager.Skip)
                                    {
                                        Program.Players.Remove(nowPlay);
                                        Program.PlayerName.Remove(nowPlay);
                                        GameAutomateManager.Skip = false;
                                    }
                                    continue;
                                }

                                if (IsReady)
                                {
                                    var name = Program.PlayerName[nowPlay];
                                    await GameAutomateManager.FindKeyboardAndInputName(name);
                                    await GameAutomateManager.GameTask(nowPlay);
                                    Program.Players.Remove(nowPlay);
                                    Program.PlayerName.Remove(nowPlay);
                                    IsReady = false;
                                }
                                else
                                {
                                    TipManager.ShowTip($"玩家 {nowPlay.Uname} 超时，未发送准备");
                                    await Task.Delay(3000);
                                    Program.Players.Remove(nowPlay);
                                    Program.PlayerName.Remove(nowPlay);
                                    continue;
                                }
                            }

                            //无人游玩进入循环等待
                            TipManager.ShowTip("当前无人游玩，请发送弹幕 上机 开始游戏");
                            await Task.Delay(100);
                        }
                    });

                    // 等待程序执行完成
                    process.WaitForExit();
                    Start = false;

                    if (!Stop)
                    {
                        Console.WriteLine("游戏进程已意外结束，正在尝试重启");
                        continue;
                    }

                    Console.WriteLine("游戏进程已结束");
                    Stop = false;
                    break;
                }
            });
        }

        public void StopGame()
        {
            GameReady = false;
            StartCount--;
            Stop = true;
            process.Kill();
        }

        public async Task RestartGame(int waitTime = 1000)
        {
            GameReady = false;
            WinAPIUtils.SetGameWindowActive();
            await Task.Delay(waitTime);
            WinAPIUtils.SendKey(VirtualKeyCode.VK_R);
            await GameAutomateManager.FindStartDoorContractualAndClick();
            GameReady = true;
        }

        public bool Runing => Start;
        public Process Process => process;
    }
}
