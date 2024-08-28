using System.Text.Json;

namespace Configuration;

class Config
{
    public static Data? ReadConfig(string configFile = @"Config/config.json")
    {
        try
        {
            DiaLog.Log($"正在读取配置文件: {configFile}");

            if (!File.Exists(configFile))
            {
                SaveConfig(null);
                DiaLog.Log($"未发现配置文件 {configFile}，已自动创建");
            }

            string jsonText = File.ReadAllText(configFile, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<Data>(jsonText);
        }
        catch (Exception ex)
        {
            DiaLog.Error($"读取配置文件 {configFile} 时出错: {ex}");
            return null;
        }
    }

    public static bool SaveConfig(Data? data, string configFile = @"Config/config.json")
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            if (data == null)
            {
                Data defaultData = new()
                {
                    ProgramData = new ProgramData(),
                    GameData = new GameData(),
                    OpenBLiveData = new OpenBLiveData()
                };

                var json = JsonSerializer.Serialize(defaultData, options);
                File.WriteAllText(configFile, json, System.Text.Encoding.UTF8);
            }
            else
            {
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(configFile, json, System.Text.Encoding.UTF8);
                DiaLog.Log($"已保存配置文件: {configFile}");
            }

            return true;
        }
        catch (Exception ex)
        {
            DiaLog.Error($"保存配置文件 {configFile} 时出错: {ex}");
            return false;
        }
    }
}