using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Utils;

namespace 精密切割系统.Helpers
{
    internal class KeyboardSimulator
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        //声明部分
        [DllImport("user32.dll", EntryPoint = "GetKeyboardState")]
        public static extern int GetKeyboardState(byte[] pbKeyState); //自写函数监听键盘按键

        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SHIFT = 0x10; // Shift 键的虚拟键码
        private const byte VK_CONTROL = 0x11; // Ctrl 键的虚拟键码
        private const byte VK_MENU = 0x12; // Alt 键的虚拟键码

        private static readonly Dictionary<string, byte> KeyMap = new Dictionary<string, byte>
    {
        {"a", 0x41}, {"b", 0x42}, {"c", 0x43}, {"d", 0x44}, {"e", 0x45},
        {"f", 0x46}, {"g", 0x47}, {"h", 0x48}, {"i", 0x49}, {"j", 0x4A},
        {"k", 0x4B}, {"l", 0x4C}, {"m", 0x4D}, {"n", 0x4E}, {"o", 0x4F},
        {"p", 0x50}, {"q", 0x51}, {"r", 0x52}, {"s", 0x53}, {"t", 0x54},
        {"u", 0x55}, {"v", 0x56}, {"w", 0x57}, {"x", 0x58}, {"y", 0x59},
        {"z", 0x5A},
        {"0", 0x30}, {"1", 0x31}, {"2", 0x32}, {"3", 0x33}, {"4", 0x34},
        {"5", 0x35}, {"6", 0x36}, {"7", 0x37}, {"8", 0x38}, {"9", 0x39},
        {"tab", 0x09}, {"del", 0x2E}, {"enter", 0x0D}, {"esc", 0x1B},
        {"space", 0x20}, {"up", 0x26}, {"down", 0x28}, {"left", 0x25}, {"right", 0x27},
        {"plus", 0xBB}, // 加号（Shift + =）
        {"minus", 0xBD}, // 减号（-）
        {"dot", 0xBE}, // 点（.）
        {"comma", 0xBC}, // 逗号（,）
        {"semicolon", 0xBA}, // 分号（;）
        {"quote", 0xDE}, // 单引号（'）
        {"slash", 0xBF}, // 斜杠（/）
        {"backslash", 0xDC}, // 反斜杠（\）
        {"leftbracket", 0xDB}, // 左中括号（[）
        {"rightbracket", 0xDD}, // 右中括号（]）
        {"caret", 0xC0}, // 抑扬符（^）
        {"tilde", 0xC0}, // 波浪符（~）
        {"capslock", 0x14}, // 大小写（capslock）
        {"backspace", 0x08}, // 删除（backspace）
        // 添加其他键...
    };

        /// <summary>
        /// 实现部分，如果按键索引码为14，则为大写键，返回为真
        /// </summary>
        public static bool CapsLockStatus
        {
            get
            {
                byte[] bs = new byte[256];
                GetKeyboardState(bs);
                return (bs[0x14] == 1);
            }
        }

        public static void SimulateKeyPress(byte key)
        {
            keybd_event(key, 0, 0, 0); // 按下键
            keybd_event(key, 0, KEYEVENTF_KEYUP, 0); // 释放键
        }

        public static void SimulateKeyPress(string keyCombination)
        {
            string[] keys = keyCombination.ToLower().Split('+');
            bool shiftPressed = false;
            bool ctrlPressed = false;
            // 按下组合键
            foreach (var key in keys)
            {
                if (key.Trim() == "shift")
                {
                    keybd_event(VK_SHIFT, 0, 0, 0); // 按下 Shift 键
                    shiftPressed = true;
                }
                else if (key.Trim() == "ctrl")
                {
                    keybd_event(VK_CONTROL, 0, 0, 0); // 按下 Ctrl 键
                    ctrlPressed = true;
                }
                else if (KeyMap.TryGetValue(key.Trim(), out byte virtualKey))
                {
                    keybd_event(virtualKey, 0, 0, 0); // 按下其他键
                }
                else
                {
                    Tools.LogError("Unsupported key: " + key);
                }
            }
            // 主键的处理
            /*string mainKey = keys[keys.Length - 1].Trim();
            if (KeyMap.TryGetValue(mainKey, out byte mainVirtualKey))
            {
                // 按下主键
                keybd_event(mainVirtualKey, 0, 0, 0);
                // 释放主键
                keybd_event(mainVirtualKey, 0, KEYEVENTF_KEYUP, 0);
            }
            else if (mainKey == "shift")
            {
                // 允许单独释放 Shift 键，不进行主键的处理
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
            }
            else
            {
                throw new ArgumentException("Unsupported key: " + mainKey);
            }*/

            foreach (var key in keys.Select(k => k.Trim()))  // 提前处理 Trim，避免每次循环调用
            {
                // 如果是 Shift 键并且 Shift 被按下
                if (key.Equals("shift", StringComparison.OrdinalIgnoreCase) && shiftPressed)
                {
                    ReleaseKey(VK_SHIFT);
                }
                // 如果是 Ctrl 键并且 Ctrl 被按下
                else if (key.Equals("ctrl", StringComparison.OrdinalIgnoreCase) && ctrlPressed)
                {
                    ReleaseKey(VK_CONTROL);
                }
                // 处理其他键
                else if (KeyMap.TryGetValue(key, out byte virtualKey))
                {
                    ReleaseKey(virtualKey);
                }
            }
        }

        // 释放组合键
        // 辅助函数：释放键盘按键
        private static void ReleaseKey(byte virtualKey)
        {
            keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, 0);
        }
    }
}