using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;


    // Класс для эмуляции ввода с клавиатуры и мыши
    public static class InputSimulator
    {
        // Win32 API импорты
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        
        // Структура для координат мыши
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        
        // Константы
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        
        // Словарь специальных клавиш
        private static readonly System.Collections.Generic.Dictionary<string, byte> keyMap = 
            new System.Collections.Generic.Dictionary<string, byte>
        {
            {"F1", 0x70}, {"F2", 0x71}, {"F3", 0x72}, {"F4", 0x73},
            {"F5", 0x74}, {"F6", 0x75}, {"F7", 0x76}, {"F8", 0x77},
            {"F9", 0x78}, {"F10", 0x79}, {"F11", 0x7A}, {"F12", 0x7B},
            {"Enter", 0x0D}, {"Tab", 0x09}, {"Esc", 0x1B}, {"Space", 0x20},
            {"Backspace", 0x08}, {"Delete", 0x2E}, {"Insert", 0x2D},
            {"Home", 0x24}, {"End", 0x23}, {"PageUp", 0x21}, {"PageDown", 0x22},
            {"Left", 0x25}, {"Up", 0x26}, {"Right", 0x27}, {"Down", 0x28},
            {"T", 0x54}, {"t", 0x54}
        };
        
        // Получает текущую раскладку клавиатуры
        private static IntPtr GetCurrentLayout()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint processId;
            uint threadId = GetWindowThreadProcessId(hwnd, out processId);
            return GetKeyboardLayout(threadId);
        }
        
        // Переключает раскладку на русскую
        public static void SwitchToRussian()
        {
            IntPtr hwnd = GetForegroundWindow();
            IntPtr currentLayout = GetCurrentLayout();
            bool isRussian = ((int)currentLayout & 0xFFFF) == 0x0419;
            
            if (!isRussian)
            {
                PostMessage(hwnd, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(100);
            }
        }
        
        // Нажимает специальную клавишу
        public static void PressKey(string keyName)
        {
            if (keyMap.ContainsKey(keyName))
            {
                byte vkCode = keyMap[keyName];
                keybd_event(vkCode, 0, 0, UIntPtr.Zero);
                keybd_event(vkCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            else
            {
                SendKeys.SendWait("{" + keyName + "}");
            }
        }
        
        // Быстрый ввод текста через keybd_event
        public static void TypeTextFast(string text, ref bool cancelRequested)
        {
            SwitchToRussian();
            
            foreach (char c in text)
            {
                if (cancelRequested) return;
                
                short vkAndShift = VkKeyScan(c);
                
                if (vkAndShift == -1)
                {
                    SendKeys.SendWait(EscapeSendKeysText(c.ToString()));
                    continue;
                }
                
                byte vk = (byte)(vkAndShift & 0xFF);
                byte shiftState = (byte)(vkAndShift >> 8);
                
                if ((shiftState & 1) != 0) keybd_event(0x10, 0, 0, UIntPtr.Zero);
                if ((shiftState & 2) != 0) keybd_event(0x11, 0, 0, UIntPtr.Zero);
                if ((shiftState & 4) != 0) keybd_event(0x12, 0, 0, UIntPtr.Zero);
                
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                if ((shiftState & 4) != 0) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if ((shiftState & 2) != 0) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if ((shiftState & 1) != 0) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                Thread.Sleep(1);
            }
        }
        
        // Вставляет текст через буфер обмена
        public static void PasteViaClipboard(string text)
        {
            Thread staThread = new Thread(() =>
            {
                string originalClipboard = "";
                try
                {
                    if (Clipboard.ContainsText())
                        originalClipboard = Clipboard.GetText();
                }
                catch { }
                
                try
                {
                    Clipboard.SetText(text);
                    Thread.Sleep(50);
                    
                    keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl down
                    Thread.Sleep(10);
                    keybd_event(0x56, 0, 0, UIntPtr.Zero); // V down
                    Thread.Sleep(10);
                    keybd_event(0x56, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // V up
                    Thread.Sleep(10);
                    keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Ctrl up
                    
                    Thread.Sleep(100);
                    
                    // Восстановление оригинального содержимого буфера обмена (в отдельном try-catch)
                    if (!string.IsNullOrEmpty(originalClipboard))
                    {
                        try
                        {
                            Clipboard.SetText(originalClipboard);
                        }
                        catch (Exception restoreEx)
                        {
                            Console.WriteLine("Предупреждение: Не удалось восстановить буфер обмена: " + restoreEx.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при работе с буфером обмена: " + ex.Message);
                }
            });
            
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
        
        // Удаляет N символов слева от курсора (Backspace × count)
        public static void SendBackspaces(int count)
        {
            for (int i = 0; i < count; i++)
            {
                keybd_event(0x08, 0, 0, UIntPtr.Zero);        // Backspace down
                keybd_event(0x08, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Backspace up
                Thread.Sleep(10);
            }
        }
        
        // Экранирует специальные символы для SendKeys
        private static string EscapeSendKeysText(string text)
        {
            text = text.Replace("+", "{+}");
            text = text.Replace("^", "{^}");
            text = text.Replace("%", "{%}");
            text = text.Replace("~", "{~}");
            text = text.Replace("(", "{(}");
            text = text.Replace(")", "{)}");
            text = text.Replace("[", "{[}");
            text = text.Replace("]", "{]}");
            text = text.Replace("{", "{{}");
            text = text.Replace("}", "{}}");
            return text;
        }
        
        // Получает координаты мыши
        public static void GetMousePosition()
        {
            POINT point;
            if (GetCursorPos(out point))
                Console.WriteLine("Координаты мыши: X=" + point.X + ", Y=" + point.Y);
            else
                Console.WriteLine("Ошибка получения координат мыши");
        }
        
        // Перемещает курсор мыши
        public static void MoveMouse(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(10);
        }
        
        // Выполняет клик мышью
        public static void ClickMouse(string button)
        {
            uint downFlag = 0;
            uint upFlag = 0;
            
            if (button.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
            {
                downFlag = MOUSEEVENTF_LEFTDOWN;
                upFlag = MOUSEEVENTF_LEFTUP;
            }
            else if (button.Equals("RIGHT", StringComparison.OrdinalIgnoreCase))
            {
                downFlag = MOUSEEVENTF_RIGHTDOWN;
                upFlag = MOUSEEVENTF_RIGHTUP;
            }
            else if (button.Equals("MIDDLE", StringComparison.OrdinalIgnoreCase))
            {
                downFlag = MOUSEEVENTF_MIDDLEDOWN;
                upFlag = MOUSEEVENTF_MIDDLEUP;
            }
            
            if (downFlag != 0)
            {
                mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
            }
        }
        
        // Выполняет клик с перемещением и возвратом курсора
        public static void ClickMouseAt(int x, int y, string button)
        {
            POINT originalPos;
            GetCursorPos(out originalPos);
            
            MoveMouse(x, y);
            Thread.Sleep(50);
            ClickMouse(button);
            MoveMouse(originalPos.X, originalPos.Y);
        }
    }
