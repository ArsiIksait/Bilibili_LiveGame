using System.Buffers;
using System.Drawing;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WindowsInput.Native;

namespace 恶魔轮盘赌弹幕版.Utils
{
    class WinAPIUtils
    {
        public static Size GetScreensSize()
        {
            var size = new Size();
            using var hdc = User32.GetDC(nint.Zero);
            size.Width = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.DESKTOPHORZRES);
            size.Height = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.DESKTOPVERTRES);
            User32.ReleaseDC(nint.Zero, hdc);
            return size;
        }

        public static async Task MouseLifeClick(int x, int y)
        {
            await MoveMouse(x, y);
            Program.Game.InputSimulator.Mouse.LeftButtonClick();
        }

        public static async Task MoveMouse(int xpos, int ypos)
        {
            var size = GetScreensSize();
            int screenWidth = size.Width;
            int screenHeight = size.Height;

            int X = xpos * 65535 / screenWidth;
            int Y = ypos * 65535 / screenHeight;

            Program.Game.InputSimulator.Mouse.MoveMouseTo(X, Y);
            await Task.Delay(100);
        }

        public static uint SendKey(VirtualKeyCode keyCode)
        {
            User32.INPUT[] inputs = {
                new() {
                    type = User32.INPUTTYPE.INPUT_KEYBOARD,
                    ki = new User32.KEYBDINPUT()
                    {
                        wVk = (ushort)keyCode,
                        dwFlags = 0,
                        wScan = (ushort)User32.MapVirtualKey((uint)keyCode, 0),
                        dwExtraInfo = IntPtr.Zero,
                        time = 0
                    }
                },
                new() {
                    type = User32.INPUTTYPE.INPUT_KEYBOARD,
                    ki = new User32.KEYBDINPUT()
                    {
                        wVk = (ushort)keyCode,
                        dwFlags = User32.KEYEVENTF.KEYEVENTF_KEYUP,
                        wScan = (ushort)User32.MapVirtualKey((uint)keyCode, 0),
                        dwExtraInfo = IntPtr.Zero,
                        time = 0
                    }
                }
            };

            var result = User32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(User32.INPUT)));
            return result;
        }

        /*public static Bitmap Screenshot()
        {
            var size = GetScreensSize();
            using (Bitmap bitmap = new(size.Width, size.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new Point(0, 0), new Point(0, 0), size);
                }
                //bitmap.Save("Screenshot.png");
                return bitmap;
            }
        }*/

        /*public static byte[] BitmapToByte(Bitmap bitmap)
        {
            // 1.先将BitMap转成内存流
            using MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            // 2.再将内存流转成byte[]并返回
            byte[] bytes = new byte[ms.Length];
            ms.Read(bytes, 0, bytes.Length);
            ms.Dispose();
            return bytes;
        }*/

        private static int screenshotToStreamOutputMaxSize = 2 * 1024 * 1024; // 确保bitmap.Save不会超过这个大小

        public class BytesDisposable : IDisposable
        {
            public byte[] Bytes { get; set; }
            public bool IsRequiredReturn { get; set; }
            public BytesDisposable(byte[] bytes, bool isRequiredReturn)
            {
                Bytes = bytes;
            }
            public void Dispose()
            {
                if (IsRequiredReturn && Bytes != null)
                {
                    ArrayPool<byte>.Shared.Return(Bytes);
                }
            }
        }

        //private static readonly RecyclableMemoryStreamManager manager = new();

        public static BytesDisposable ScreenshotToStream() //ilyfairy: 使用非托管内存ArrayPool，减少GC使用，把图片等比缩放到640大小
        {
            var size = GetScreensSize();
            using Bitmap bitmap = new(size.Width, size.Height);
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), size);

            BytesDisposable buffer = new BytesDisposable(ArrayPool<byte>.Shared.Rent(screenshotToStreamOutputMaxSize), true);
            using MemoryStream ms = new MemoryStream(buffer.Bytes, 0, buffer.Bytes.Length, true, true);
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            //return ms.ToArray();

            if (ms.GetBuffer() == buffer.Bytes)
            {
                //Console.WriteLine($"已缓存 Length:{buffer.Bytes.Length}");
                return buffer;
            }
            else
            {
                screenshotToStreamOutputMaxSize = (int)ms.Length;
                return new BytesDisposable(ms.ToArray(), false);
            }
        }

        public static void SetGameWindowActive()
        {
            // 设置活动窗口
            if (FindGameWindow(out var hWND))
            {
                User32.SetForegroundWindow(hWND);
            }
            else
            {
                Console.WriteLine("未能成功设置游戏活动窗口，原因是未找到游戏窗口");
            }
        }

        public static bool FindGameWindow(out HWND hWND)
        {
            // 根据窗口标题找到对应的窗口句柄
            hWND = User32.FindWindow(null, "Buckshot Roulette");

            // 判断窗口是否存在
            if (hWND != nint.Zero)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ReadModuleBaseAddress(IntPtr offsetAddress, out long baseAddress)
        {
            baseAddress = IntPtr.Zero;
            var process = Program.Game.Process;

            if (process == null)
                return false;
            if (process.MainModule == null)
                return false;

            if (ReadMemory(process.MainModule.BaseAddress + offsetAddress, out long address))
            {
                //Console.WriteLine($"######Process.MainModule.BaseAddress: {process.MainModule.BaseAddress:x} OffsetAddress: {offsetAddress:x} ReadAddress: {process.MainModule.BaseAddress + offsetAddress:x} OutBaseAddress: {address:x}");
                baseAddress = address;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ReadMultilevelPointerAddress(long baseAddress, IntPtr[] offsets, out long address, out long value)
        {
            address = 0;
            value = 0;
            var tmpAddress = baseAddress;

            //Console.WriteLine($"*******BaseAddress: {baseAddress:x} Offsets: {offsets} {offsets.Length}");

            for (int i = 0; i < offsets.Length; i++)
            {
                if (ReadMemory(tmpAddress + offsets[i], out long nextAddress))
                {
                    //Console.WriteLine($"tmpAddress + offsets[{i}]: {tmpAddress:x} + {offsets[i]:x}，NextAddress: {nextAddress:x}");
                    address = tmpAddress + offsets[i];
                    tmpAddress = nextAddress;
                }
                else
                {
                    return false;
                }
            }

            value = tmpAddress;
            return true;
        }

        public static bool ReadMemory(long baseAddress, out long memoryValue)
        {
            memoryValue = 0;
            var process = Program.Game.Process;

            if (process == null)
                return false;

            //Console.WriteLine($"#ReadAddress: {baseAddress:x}");
            //Console.WriteLine($"BaseAddress: {process.MainModule?.BaseAddress:x}");

            byte[] buffer = new byte[4];
            try
            {
                //获取缓冲区地址 :固定数组元素的不安全地址
                IntPtr byteAddress = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
                //获取进程的最高权限
                Kernel32.SafeHPROCESS hProcess = Kernel32.OpenProcess(0x1F0FFF, false, uint.Parse(process.Id.ToString()));
                //将指定内存中的值读入缓冲区 
                Kernel32.ReadProcessMemory(hProcess, (nint)baseAddress, byteAddress, 8, out _);
                //Kernel32.CloseHandle(hProcess);
                hProcess.Close();
                memoryValue = Marshal.ReadInt64(byteAddress);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取内存出错:{ex.Message}");
                return false;
            }
        }

        public static bool WriteMemory(long baseAddress, long memoryValue)
        {
            var process = Program.Game.Process;

            if (process == null)
                return false;

            //Console.WriteLine($"#ReadAddress: {baseAddress:x}");
            //Console.WriteLine($"BaseAddress: {process.MainModule?.BaseAddress:x}");

            try
            {
                Kernel32.SafeHPROCESS hProcess = Kernel32.OpenProcess(0x1F0FFF, false, uint.Parse(process.Id.ToString())); //0x1F0FFF 最高权限 
                Kernel32.WriteProcessMemory(hProcess, (nint)baseAddress, (nint)memoryValue, 8, out _);
                hProcess.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入内存出错:{ex.Message}");
                return false;
            }
        }
    }
}