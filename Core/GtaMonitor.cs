using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


    // Класс для мониторинга активности GTA SA
    public static class GtaMonitor
    {
        // Win32 API импорты
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        // Кэширование для оптимизации
        private static Process cachedGtaProcess = null;
        private static DateTime lastCheckTime = DateTime.MinValue;
        private static bool lastCheckResult = false;
        private const int CACHE_MS = 100; // Кэшировать результат на 100мс
        
        // Проверяет, запущен ли вообще процесс gta_sa.exe (без проверки активности окна)
        public static bool IsGtaSaRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("gta_sa");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }
        
        // Проверяет, является ли активное окно процессом gta_sa.exe (с кэшированием)
        public static bool IsGtaSaActive()
        {
            try
            {
                // Проверяем кэш - если прошло меньше CACHE_MS, возвращаем кэшированный результат
                DateTime now = DateTime.Now;
                if ((now - lastCheckTime).TotalMilliseconds < CACHE_MS)
                {
                    return lastCheckResult;
                }
                
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    lastCheckTime = now;
                    lastCheckResult = false;
                    cachedGtaProcess = null;
                    return false;
                }
                
                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);
                
                // Проверяем кэшированный процесс
                if (cachedGtaProcess != null && !cachedGtaProcess.HasExited && cachedGtaProcess.Id == processId)
                {
                    lastCheckTime = now;
                    lastCheckResult = true;
                    return true;
                }
                
                // Получаем новый процесс
                Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName.ToLower();
                bool isGta = processName == "gta_sa";
                
                // Кэшируем результат
                if (isGta)
                {
                    cachedGtaProcess = process;
                }
                else
                {
                    cachedGtaProcess = null;
                }
                
                lastCheckTime = now;
                lastCheckResult = isGta;
                return isGta;
            }
            catch
            {
                lastCheckTime = DateTime.Now;
                lastCheckResult = false;
                cachedGtaProcess = null;
                return false;
            }
        }
        
        // Сбрасывает кэш (полезно при перезапуске проверок)
        public static void ResetCache()
        {
            cachedGtaProcess = null;
            lastCheckTime = DateTime.MinValue;
            lastCheckResult = false;
        }
    }
