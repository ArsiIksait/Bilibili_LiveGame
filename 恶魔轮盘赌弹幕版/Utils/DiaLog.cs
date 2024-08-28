class DiaLog
{
    public static bool saveLog = true;
    static readonly string logFile = @"Config/恶魔轮盘赌弹幕版.log";

    public static void Log(string text)
    {
        Console.ForegroundColor = ConsoleColor.White;
        text = $"[{DateTime.Now}] [Log]: {text}";
        Console.WriteLine(text);
        SaveLog(text);
    }

    public static void Info(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        text = $"[{DateTime.Now}] [Info]: {text}";
        Console.WriteLine(text);
        Console.ForegroundColor = ConsoleColor.White;
        SaveLog(text);
    }

    public static void Warning(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        text = $"[{DateTime.Now}] [Warning]: {text}";
        Console.WriteLine(text);
        Console.ForegroundColor = ConsoleColor.White;
        SaveLog(text);
    }

    public static void Error(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        text = $"[{DateTime.Now}] [Error]: {text}";
        Console.WriteLine(text);
        Console.ForegroundColor = ConsoleColor.White;
        SaveLog(text);
    }
    static void SaveLog(string text)
    {
        try
        {
            if (saveLog)
            {
                File.AppendAllText(logFile, text + "\r\n");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now}] [Error]: 在保存日志时发生了错误: {ex}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}