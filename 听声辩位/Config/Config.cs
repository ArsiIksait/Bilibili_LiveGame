using Microsoft.VisualBasic;
using System.IO;
using System.Text.Json;
using System.Windows;
using 听声辩位;

namespace Configuration;

class Config
{
    public static Data? ReadConfig(string configFile = @"config.json")
    {
        try
        {
            if (!File.Exists($"Config/{configFile}"))
            {
                SaveConfig(null);
                MainWindow.Instance.Announce($"未发现配置文件 {configFile}，已自动创建");
            }

            string jsonText = File.ReadAllText($"Config/{configFile}", System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<Data>(jsonText);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.Announce($"读取配置文件 {configFile} 时出错: {ex}");
            return null;
        }
    }

    public static bool SaveConfig(Data? data, string configFile = @"config.json")
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            if (data == null)
            {
                Data defaultData = new()
                {
                    ProgramData = new ProgramData(),
                    OpenBLiveData = new OpenBLiveData(),
                    ServerChan = new ServerChan(),
                    PlayersData = new PlayersData()
                };

                MessageBox.Show("由于您第一次使用本程序，即将为您进行配置，请注意，如果输入错误，您需要手动编辑或者删除程序目录下的Config\\config.json进行配置OpenBLiveData里面的内容");

                defaultData.OpenBLiveData.AccessKeyId = Interaction.InputBox("没有检测到AccessKeyId配置文件，请访问网页 https://open-live.bilibili.com/open-manage 申请成为开发者，获取AccessKeyId", "请输入AccessKeyId", "https://open-live.bilibili.com/open-manage", -1, -1);
                defaultData.OpenBLiveData.AccessKeySecret = Interaction.InputBox("没有检测到AccessKeySecret配置文件，请访问网页 https://open-live.bilibili.com/open-manage 申请成为开发者，获取AccessKeySecret", "请输入AccessKeySecret", "https://open-live.bilibili.com/open-manage", -1, -1);
                defaultData.OpenBLiveData.AppId = Interaction.InputBox("没有检测到项目AppId配置文件，请访问网页 https://open-live.bilibili.com/open-manage 申请成为开发者，审核通过后创建一个项目获取项目AppId", "请输入项目AppId", "https://open-live.bilibili.com/open-manage", -1, -1);
                defaultData.OpenBLiveData.AnchorID = Interaction.InputBox("没有检测到主播身份码配置文件，请访问网页 https://play-live.bilibili.com/ 点击网页右下角的身份码图标获取身份码", "请输入主播身份码", "https://play-live.bilibili.com/", -1, -1);
                defaultData.ServerChan.SendKey = Interaction.InputBox("没有检测到Server酱配置文件，如需使用Server酱推送礼物消息提醒到微信，请访问网页 https://sct.ftqq.com/ 注册登录并支付8元购买一个月会员，然后点击网页通道配置，选择方糖服务号（需要关注公众号才能获取到消息），然后点击Key&Api，在App下点击添加按钮，填写应用备注，其他可不填，然后点击生成，复制App SendKey然后粘贴到此", "请输入Server酱消息密钥SendKey\n此项目可以不填写", "https://sct.ftqq.com/", -1, -1);
                defaultData.ServerChan.SendKey = defaultData.ServerChan.SendKey == "https://sct.ftqq.com/" ? string.Empty : defaultData.ServerChan.SendKey;

            inputMaxWaitCount:
                if (int.TryParse(Interaction.InputBox("没有检测到最大派对人数配置文件，请输入一个正整数 比如20", "请输入最大排队人数", "20", -1, -1), out int count))
                {
                    if (count >= 0)
                    {
                        defaultData.ProgramData.MaxWaitCount = count;
                    }
                    else
                    {
                        MessageBox.Show("请输入一个正整数，如0，5，10，20");
                        goto inputMaxWaitCount;
                    }
                }
                else
                {
                    MessageBox.Show("请输入一个正整数，如0，5，10，20");
                    goto inputMaxWaitCount;
                }

            inputTimeout:
                if (int.TryParse(Interaction.InputBox("没有检测到超时秒数配置文件，请输入一个正整数 比如30", "请输入超时秒数", "30", -1, -1), out int time))
                {
                    if (time >= 0)
                    {
                        defaultData.ProgramData.Timeout = time;
                    }
                    else
                    {
                        MessageBox.Show("请输入一个正整数，如0，5，10，30");
                        goto inputTimeout;
                    }
                }
                else
                {
                    MessageBox.Show("请输入一个正整数，如0，5，10，30");
                    goto inputTimeout;
                }

                if (!Directory.Exists("Config"))
                {
                    Directory.CreateDirectory("Config");
                }

                var json = JsonSerializer.Serialize(defaultData, options);
                File.WriteAllText($"Config/{configFile}", json, System.Text.Encoding.UTF8);
            }
            else
            {
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText($"Config/{configFile}", json, System.Text.Encoding.UTF8);
            }

            return true;
        }
        catch (Exception ex)
        {
            MainWindow.Instance.Announce($"保存配置文件 Config/{configFile} 时出错: {ex}");
            return false;
        }
    }
}