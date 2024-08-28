namespace 恶魔轮盘赌弹幕版.Managers
{
    class TipManager
    {
        public static void ShowTip(string text)
        {
            ConfigManager.SaveText("Config/Playing.txt", text);
        }

        public static async Task ShowCommandResult(string text, int wait = 3000)
        {
            ConfigManager.SaveText("Config/CommandResult.txt", text);
            await Task.Delay(wait);
            ConfigManager.SaveText("Config/CommandResult.txt", " ");
        }
    }
}