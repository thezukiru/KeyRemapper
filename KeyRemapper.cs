using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

// ПРИМЕЧАНИЕ: Классы KeyBinding, ConfigParser, InstructionExecutor теперь в отдельных файлах
// Models/KeyBinding.cs, Core/ConfigParser.cs, Core/InstructionExecutor.cs, Core/Overlay.cs
// Этот файл содержит только KeyRemapper

// Главный класс для перехвата клавиш и выполнения биндингов
class KeyRemapper
{
    // Константы для Windows API
    private const int WH_KEYBOARD_LL = 13;      // Тип хука - низкоуровневый клавиатурный
    private const int WM_KEYDOWN = 0x0100;      // Сообщение о нажатии клавиши
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
    private static string configFile = "config.txt";
    private static List<KeyBinding> keyBindings = new List<KeyBinding>();
    
    // Отслеживание нажатий Win для экстренного закрытия
    private static List<DateTime> winKeyPresses = new List<DateTime>();
    private static readonly object winKeyLock = new object();
    private const int MAX_KEY_PRESS_HISTORY = 10; // Ограничение размера истории для предотвращения утечки памяти
    
    // Отслеживание нажатий Esc для отмены скрипта (3 раза за секунду)
    private static List<DateTime> escPresses = new List<DateTime>();
    private static readonly object escLock = new object();

    // Импорты Windows API для работы с хуками клавиатуры
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Точка входа в программу
    [STAThread]
    static void Main()
    {
        bool createdNew;
        Mutex singleInstanceMutex = new Mutex(true, "KeyRemapper_SingleInstance", out createdNew);
        if (!createdNew)
        {
            MessageBox.Show("KeyRemapper уже запущен.", "KeyRemapper", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        // Загружаем биндинги из конфигурационного файла
        keyBindings = ConfigParser.LoadConfig(configFile);
        
        // Overlay инициализируется в MainWindow.OnShown() на UI-потоке
        // для гарантии что handle формы создан до первого запуска скрипта
        
        // Устанавливаем глобальный хук клавиатуры
        _hookID = SetHook(_proc);
        
        // Почему MainWindow вместо пустого Application.Run():
        // раньше программа работала "вслепую" через консоль, теперь GUI показывает
        // загруженные биндинги, статус GTA, и позволяет управлять программой из трея
        Application.Run(new MainWindow());

        singleInstanceMutex.ReleaseMutex();
        singleInstanceMutex = null;
        UnhookWindowsHookEx(_hookID);
    }

    // Устанавливает хук клавиатуры
    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // Callback функция, вызываемая при каждом нажатии клавиши
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            // Обработка Win: 3x Win за секунду - экстренное закрытие программы
            if (key == Keys.LWin || key == Keys.RWin)
            {
                lock (winKeyLock)
                {
                    DateTime now = DateTime.Now;
                    // Удаляем старые нажатия (старше 1 секунды)
                    winKeyPresses.RemoveAll(t => (now - t).TotalSeconds > 1.0);
                    // Ограничиваем размер списка для предотвращения утечки памяти
                    if (winKeyPresses.Count >= MAX_KEY_PRESS_HISTORY)
                    {
                        winKeyPresses.RemoveAt(0);
                    }
                    // Добавляем текущее нажатие
                    winKeyPresses.Add(now);
                    
                    // Если 3 нажатия за секунду - экстренное закрытие
                    if (winKeyPresses.Count >= 3)
                    {
                        Console.WriteLine("ЭКСТРЕННОЕ ЗАКРЫТИЕ: обнаружено 3 нажатия Win за секунду!");
                        UnhookWindowsHookEx(_hookID);
                        Environment.Exit(0);
                    }
                }
                
                // Win НЕ отменяет скрипт на паузе, только во время выполнения
                if (InstructionExecutor.IsExecuting() && !InstructionExecutor.IsWaiting())
                {
                    InstructionExecutor.RequestCancel();
                    return (IntPtr)1; // Блокируем нажатие
                }
            }
            
            // Обработка Esc: отмена скрипта только при трёх быстрых нажатиях, иначе Esc передаётся в игру
            if (key == Keys.Escape)
            {
                bool shouldCancel = false;
                lock (escLock)
                {
                    DateTime now = DateTime.Now;
                    // Удаляем нажатия старше 1 секунды
                    escPresses.RemoveAll(t => (now - t).TotalSeconds > 1.0);
                    // Ограничиваем размер списка для предотвращения утечки памяти
                    if (escPresses.Count >= MAX_KEY_PRESS_HISTORY)
                    {
                        escPresses.RemoveAt(0);
                    }
                    // Добавляем текущее нажатие
                    escPresses.Add(now);
                    
                    // Если накоплено 3 нажатия за секунду и скрипт активен, отменяем
                    if (escPresses.Count >= 3 && (InstructionExecutor.IsExecuting() || InstructionExecutor.IsWaiting()))
                    {
                        shouldCancel = true;
                        escPresses.Clear(); // Сбрасываем накопленные нажатия
                    }
                }
                
                if (shouldCancel)
                {
                    Console.WriteLine("Скрипт отменён тремя быстрыми нажатиями Esc.");
                    if (InstructionExecutor.IsWaiting())
                        InstructionExecutor.CancelWaiting();
                    else if (InstructionExecutor.IsExecuting())
                        InstructionExecutor.RequestCancel();
                    return (IntPtr)1; // Блокируем только это (третье) нажатие, чтобы игра не получила лишний Esc
                }
                // Иначе не блокируем - Esc уходит в игру
            }

            // Проверяем нажатие правого Shift для продолжения выполнения
            if (key == Keys.RShiftKey)
            {
                if (InstructionExecutor.CanContinue(null))
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(50);
                        InstructionExecutor.ContinueExecution();
                    });
                    return (IntPtr)1; // Блокируем нажатие
                }
            }

            // Проверяем состояние модификаторов (Ctrl, Shift, Alt)
            bool ctrlPressed = (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
            bool altPressed = (GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0;

            // Проверяем все загруженные биндинги
            foreach (KeyBinding binding in keyBindings)
            {
                if (binding.Matches(key, ctrlPressed, shiftPressed, altPressed))
                {
                    // Найден подходящий биндинг
                    string fileToExecute = binding.InstructionFile;
                    
                    // Если скрипт на паузе и это тот же файл - начинаем сначала
                    if (InstructionExecutor.CanContinue(fileToExecute))
                    {
                        Console.WriteLine("Перезапуск скрипта: " + fileToExecute);
                    }
                    
                    // Проверяем активность GTA перед запуском задачи
                    if (GtaMonitor.IsGtaSaActive())
                    {
                        // Запускаем выполнение инструкций (или перезапускаем)
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(50);
                            InstructionExecutor.ExecuteInstructions(fileToExecute);
                        });
                    }
                    
                    // Блокируем оригинальное нажатие клавиш
                    return (IntPtr)1;
                }
            }
        }
        // Передаем событие дальше по цепочке хуков
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
}
