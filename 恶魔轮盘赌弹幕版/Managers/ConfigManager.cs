using Configuration;
using OpenBLive.Runtime.Utilities;
using System.Text;

namespace 恶魔轮盘赌弹幕版.Managers
{
    internal static class ConfigManager
    {
        public static Data? ConfigData;

        public static void SaveText(string file, string text)
        {
            try
            {
                File.WriteAllBytes(file, Encoding.UTF8.GetBytes(text));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存提示出错: {ex}");
            }
        }

        public static (string appId, string code) ReadEnv()
        {
            var appId = string.Empty;
            var code = string.Empty;

            ConfigData = Config.ReadConfig();

            if (ConfigData == null)
            {
                DiaLog.Error("配置文件读取失败，请删除配置文件或者打开文本编辑器尝试修复! 请按任意键退出程序. . .");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            if (string.IsNullOrWhiteSpace(ConfigData.OpenBLiveData.AccessKeyId))
            {
                while (true)
                {
                    DiaLog.Error("没有检测到AccessKeyId配置文件，您可能是第一次使用本程序，请访问网页 https://open-live.bilibili.com/open-manage 申请成为开发者，获取AccessKeyId\n请输入AccessKeyId:");
                    SignUtility.accessKeyId = Console.ReadLine() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(SignUtility.accessKeyId))
                    {
                        DiaLog.Error("您必须输入AccessKeyId才能使用本程序");
                        continue;
                    }

                    break;
                }
            }
            else
            {
                SignUtility.accessKeyId = ConfigData.OpenBLiveData.AccessKeyId;
            }

            if (string.IsNullOrWhiteSpace(ConfigData.OpenBLiveData.AccessKeySecret))
            {
                while (true)
                {
                    DiaLog.Error("没有检测到AccessKeySecret配置文件，您可能是第一次使用本程序，请访问网页 https://open-live.bilibili.com/open-manage 申请成为开发者，获取AccessKeySecret\n请输入AccessKeySecret:");
                    SignUtility.accessKeySecret = Console.ReadLine() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(SignUtility.accessKeySecret))
                    {
                        DiaLog.Error("您必须输入AccessKeySecret才能使用本程序");
                        continue;
                    }

                    break;
                }
            }
            else
            {
                SignUtility.accessKeySecret = ConfigData.OpenBLiveData.AccessKeySecret;
            }

            if (string.IsNullOrWhiteSpace(ConfigData.OpenBLiveData.AppId))
            {
                while (true)
                {
                    DiaLog.Error("没有检测到项目AppId配置文件，您可能是第一次使用本程序，请访问网页 https://open-live.bilibili.com/open-manage 申请成为开发者，审核通过后创建一个项目获取项目AppId\n请输入AppId:");
                    appId = Console.ReadLine() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(appId))
                    {
                        DiaLog.Error("您必须输入AppId才能使用本程序");
                        continue;
                    }

                    break;
                }
            }
            else
            {
                appId = ConfigData.OpenBLiveData.AppId;
            }

            if (string.IsNullOrWhiteSpace(ConfigData.OpenBLiveData.AnchorID))
            {
                while (true)
                {
                    DiaLog.Error("没有检测到主播身份码配置文件，您可能是第一次使用本程序，请访问网页 https://play-live.bilibili.com/ 点击网页右下角的身份码图标获取身份码\n请输入主播身份码:");
                    code = Console.ReadLine() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        DiaLog.Error("您必须输入身份码才能使用本程序");
                        continue;
                    }

                    break;
                }
            }
            else
            {
                code = ConfigData.OpenBLiveData.AnchorID;
            }

            if (string.IsNullOrWhiteSpace(ConfigData.GameData.GamePath))
            {
                while (true)
                {
                    DiaLog.Error("没有检测到游戏路径配置文件，您可能是第一次使用本程序，请复制游戏完整路径粘贴到这里然后回车来设置路径\n请输入游戏路径:");
                    BuckshotRoulette.GamePath = Console.ReadLine() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(BuckshotRoulette.GamePath))
                    {
                        DiaLog.Error("您必须设置游戏路径才能启动游戏!");
                    }

                    BuckshotRoulette.GamePath = BuckshotRoulette.GamePath.Replace("\"", "");

                    if (File.Exists(BuckshotRoulette.GamePath) && Path.GetExtension(BuckshotRoulette.GamePath) == ".exe")
                    {
                        DiaLog.Log("设置游戏路径成功!");
                        ConfigData.GameData.GamePath = BuckshotRoulette.GamePath;
                        Config.SaveConfig(ConfigData);
                        break;
                    }
                    else
                    {
                        DiaLog.Error("您设置的游戏路径没有找到游戏可执行文件(.exe)文件!");
                    }
                }
            }
            else
            {
                BuckshotRoulette.GamePath = ConfigData.GameData.GamePath;
            }

            BuckshotRoulette.UseOpenGL = ConfigData.GameData.UseOpenGL3;
            Program.MaxWaitCount = ConfigData.ProgramData.MaxWaitCount;
            DiaLog.Log($"最大等待人数已设置为{Program.MaxWaitCount}人");
            Program.AdminOpenId = ConfigData.ProgramData.AdminOpenId;
            Program.PlayersData = ConfigData.PlayersData.Players;

            return (appId, code);
        }

        public static void SetEnv(string appId, string code)
        {
            ConfigData!.OpenBLiveData.AccessKeyId = SignUtility.accessKeyId;
            DiaLog.Log("已保存AccessKeyId");

            ConfigData!.OpenBLiveData.AccessKeySecret = SignUtility.accessKeySecret;
            DiaLog.Log("已保存AccessKeySecret");

            ConfigData!.OpenBLiveData.AppId = appId;
            DiaLog.Log("已保存AppId");

            ConfigData!.OpenBLiveData.AnchorID = code;
            DiaLog.Log("已保存主播身份码");

            Config.SaveConfig(ConfigData);
        }

        public static void SaveAdminOpenId()
        {
            ConfigData!.ProgramData.AdminOpenId = Program.AdminOpenId;
            Config.SaveConfig(ConfigData);
            DiaLog.Log($"已保存管理员OpenId");
        }

        public static void SaveMaxWaitCount(int waitCount)
        {
            ConfigData!.ProgramData.MaxWaitCount = waitCount;
            Config.SaveConfig(ConfigData);
            DiaLog.Log($"已保存最大等待人数{waitCount}人");
        }

        public static void SavePlayerData()
        {
            ConfigData!.PlayersData.Players = Program.PlayersData;
            Config.SaveConfig(ConfigData);
            DiaLog.Log($"已保存玩家数据");
        }
    }
}