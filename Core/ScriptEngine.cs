using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;


    // Главный движок выполнения скриптов
    public class ScriptEngine
    {
        // Состояние выполнения
        private bool isWaiting = false;
        private string currentFile = null;
        private int currentLineIndex = 0;
        private bool useClipboardMode = false;
        private string[] currentLines = null;
        private readonly object executionLock = new object();
        
        // Флаги (volatile для потокобезопасности)
        private volatile bool cancelRequested = false;
        private volatile bool isExecuting = false;
        
        // Компоненты
        private BranchingEngine branchingEngine = new BranchingEngine();
        private OverlayForm overlayForm = null; // OverlayForm для визуализации
        
        // Счетчик для проверки GTA SA
        private int gtaCheckCounter = 0;
        private const int GTA_CHECK_INTERVAL = 3; // Проверять каждые 3 строки (безопасность!)
        
        // Публичные методы для управления
        public void RequestCancel()
        {
            lock (executionLock)
            {
                if (isExecuting)
                {
                    cancelRequested = true;
                    branchingEngine.RequestCancel();
                    Console.WriteLine("Запрошена отмена выполнения скрипта!");
                }
            }
        }
        
        public void CancelWaiting()
        {
            lock (executionLock)
            {
                if (isWaiting)
                {
                    isWaiting = false;
                    isExecuting = false;
                    currentFile = null;
                    currentLines = null;
                    cancelRequested = false;
                    Console.WriteLine("Скрипт отменен на паузе.");
                }
            }
        }
        
        public bool IsWaiting()
        {
            return isWaiting;
        }
        
        public bool IsExecuting()
        {
            return isExecuting;
        }
        
        public bool CanContinue(string filePath)
        {
            lock (executionLock)
            {
                if (filePath == null)
                    return isWaiting;
                return isWaiting && currentFile == filePath;
            }
        }
        
        public void SetOverlayForm(OverlayForm overlay)
        {
            overlayForm = overlay;
        }
        
        // Обновляет overlay с текущим состоянием скрипта
        private void UpdateOverlay(string[] lines, int currentIndex)
        {
            if (overlayForm == null) return;
            
            try
            {
                string breadcrumbs = string.Join(" > ", branchingEngine.Breadcrumbs);
                overlayForm.UpdateText(lines, currentIndex, breadcrumbs);
            }
            catch
            {
                // Игнорируем ошибки обновления overlay
            }
        }
        
        // Скрывает overlay
        private void HideOverlay()
        {
            if (overlayForm == null) return;
            
            try
            {
                overlayForm.HideText();
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
        
        // Главный метод выполнения скрипта
        public void ExecuteInstructions(string filePath)
        {
            if (!GtaMonitor.IsGtaSaActive())
            {
                Console.WriteLine("GTA SA не является активным окном. Скрипт не запущен.");
                return;
            }
            
            lock (executionLock)
            {
                if (isWaiting && currentFile != filePath)
                {
                    Console.WriteLine("Уже выполняется другой скрипт, ожидание завершения...");
                    return;
                }
                
                isWaiting = false;
                isExecuting = true;
                cancelRequested = false;
                currentFile = filePath;
                currentLineIndex = 0;
                gtaCheckCounter = 0;
                branchingEngine.ResetCancel();
            }
            
            SendKeys.Flush();
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Файл " + filePath + " не найден!");
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            
            lock (executionLock)
            {
                currentLines = lines;
            }
            
            // Парсим метки и ветки
            branchingEngine.ParseLabelsAndBranches(lines);
            
            // Проверяем режим буфера обмена
            bool useClipboard = false;
            int startLine = 0;
            
            if (lines.Length > 0 && lines[0].Trim() == "!")
            {
                useClipboard = true;
                startLine = 1;
                Console.WriteLine("Режим буфера обмена активирован для " + filePath);
            }
            
            lock (executionLock)
            {
                useClipboardMode = useClipboard;
                currentLineIndex = startLine;
            }
            
            // Показываем overlay через InstructionExecutor (с учётом autoMode)
            InstructionExecutor.ShowOverlayForScript();
            UpdateOverlay(lines, startLine);
            
            // Выполняем скрипт
            ExecuteLines(lines, startLine, useClipboard);
        }
        
        // Продолжает выполнение после паузы
        public void ContinueExecution()
        {
            lock (executionLock)
            {
                if (!isWaiting) return;
                
                isWaiting = false;
                cancelRequested = false;
                gtaCheckCounter = 0;
                branchingEngine.ResetCancel();
                Console.WriteLine("Продолжаем выполнение скрипта: " + currentFile);
            }
            
            ExecuteLines(currentLines, currentLineIndex, useClipboardMode);
        }
        
        // Выполняет строки скрипта
        private void ExecuteLines(string[] lines, int startLine, bool useClipboard)
        {
            int i = startLine;
            while (i < lines.Length)
            {
                // Проверка на остановку
                if (ShouldStopExecution())
                {
                    string reason = cancelRequested ? 
                        "Выполнение скрипта отменено пользователем." : 
                        "GTA SA больше не активна. Скрипт остановлен.";
                    StopScript(reason);
                    return;
                }
                
                string line = lines[i];
                
                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("//"))
                {
                    i++;
                    continue;
                }
                
                // Обработка команд ветвления
                bool shouldStop = false;
                int nextLine = branchingEngine.ProcessBranchingCommands(line, i, lines, ref shouldStop);
                if (shouldStop)
                {
                    StopScript("Скрипт остановлен из-за ошибки ветвления.");
                    return;
                }
                if (nextLine != i)
                {
                    i = nextLine;
                    continue;
                }
                
                // Обработка строки
                bool continueExecution;
                if (useClipboard)
                    continueExecution = ProcessLineForClipboard(line);
                else
                    continueExecution = ProcessLine(line);
                
                if (!continueExecution)
                {
                    // Встретили {WAIT}
                    lock (executionLock)
                    {
                        currentLineIndex = i + 1;
                        isWaiting = true;
                        Console.WriteLine("Пауза {WAIT} - нажмите биндинг снова для продолжения");
                    }
                    // Обновляем overlay перед паузой
                    UpdateOverlay(lines, i);
                    return;
                }
                
                // Обновляем overlay после каждой строки
                UpdateOverlay(lines, i);
                
                i++;
            }
            
            // Скрипт завершен
            lock (executionLock)
            {
                isWaiting = false;
                isExecuting = false;
                currentFile = null;
                currentLines = null;
                Console.WriteLine("Скрипт завершен.");
            }
            branchingEngine.Clear();
            
            // Скрываем overlay через InstructionExecutor (с учётом autoMode)
            InstructionExecutor.HideOverlayForScript();
        }
        
        // Проверяет, нужно ли остановить выполнение
        private bool ShouldStopExecution()
        {
            if (cancelRequested)
                return true;
            
            gtaCheckCounter++;
            if (gtaCheckCounter >= GTA_CHECK_INTERVAL)
            {
                gtaCheckCounter = 0;
                if (!GtaMonitor.IsGtaSaActive())
                    return true;
            }
            
            return false;
        }
        
        // Останавливает скрипт
        private void StopScript(string reason)
        {
            lock (executionLock)
            {
                isWaiting = false;
                isExecuting = false;
                currentFile = null;
                currentLines = null;
                cancelRequested = false;
                Console.WriteLine(reason);
            }
            branchingEngine.Clear();
            
            // Скрываем overlay через InstructionExecutor (с учётом autoMode)
            InstructionExecutor.HideOverlayForScript();
        }
        
        // Обрабатывает строку в режиме буфера обмена
        private bool ProcessLineForClipboard(string line)
        {
            int pos = 0;
            System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
            
            while (pos < line.Length)
            {
                if (cancelRequested) return false;
                
                // Задержка [число]
                if (line[pos] == '[')
                {
                    int endPos = line.IndexOf(']', pos);
                    if (endPos > pos)
                    {
                        if (textBuilder.Length > 0)
                        {
                            InputSimulator.PasteViaClipboard(textBuilder.ToString());
                            textBuilder.Clear();
                            Thread.Sleep(50);
                        }
                        
                        string delayStr = line.Substring(pos + 1, endPos - pos - 1);
                        double delay;
                        if (double.TryParse(delayStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out delay))
                        {
                            int totalMs = (int)(delay * 1000);
                            int elapsed = 0;
                            while (elapsed < totalMs)
                            {
                                if (cancelRequested) return false;
                                int sleepTime = Math.Min(50, totalMs - elapsed);
                                Thread.Sleep(sleepTime);
                                elapsed += sleepTime;
                            }
                        }
                        pos = endPos + 1;
                        continue;
                    }
                }
                
                // Специальная клавиша {Key}
                if (line[pos] == '{')
                {
                    int endPos = line.IndexOf('}', pos);
                    if (endPos > pos)
                    {
                        string keyName = line.Substring(pos + 1, endPos - pos - 1);
                        
                        if (keyName.Equals("WAIT", StringComparison.OrdinalIgnoreCase))
                        {
                            if (textBuilder.Length > 0)
                            {
                                InputSimulator.PasteViaClipboard(textBuilder.ToString());
                                textBuilder.Clear();
                                Thread.Sleep(50);
                            }
                            return false;
                        }
                        
                        // Пропускаем команды управления потоком
                        if (keyName.StartsWith("LABEL=", StringComparison.OrdinalIgnoreCase) ||
                            keyName.StartsWith("BRANCH=", StringComparison.OrdinalIgnoreCase) ||
                            keyName.Equals("END_BRANCH", StringComparison.OrdinalIgnoreCase) ||
                            keyName.StartsWith("GOTO=", StringComparison.OrdinalIgnoreCase) ||
                            keyName.StartsWith("WAIT_BRANCH=", StringComparison.OrdinalIgnoreCase))
                        {
                            pos = endPos + 1;
                            continue;
                        }
                        
                        if (textBuilder.Length > 0)
                        {
                            InputSimulator.PasteViaClipboard(textBuilder.ToString());
                            textBuilder.Clear();
                            Thread.Sleep(50);
                        }
                        
                        // Обработка команд мыши и клавиш
                        ProcessSpecialCommand(keyName);
                        Thread.Sleep(50);
                        
                        pos = endPos + 1;
                        continue;
                    }
                }
                
                // Обычный текст
                int nextSpecial = line.Length;
                int nextBracket = line.IndexOf('[', pos);
                int nextBrace = line.IndexOf('{', pos);
                
                if (nextBracket >= 0) nextSpecial = Math.Min(nextSpecial, nextBracket);
                if (nextBrace >= 0) nextSpecial = Math.Min(nextSpecial, nextBrace);
                
                if (nextSpecial > pos)
                {
                    string text = line.Substring(pos, nextSpecial - pos);
                    textBuilder.Append(text);
                    pos = nextSpecial;
                }
                else
                {
                    pos++;
                }
            } if (textBuilder.Length > 0)
            {
                InputSimulator.PasteViaClipboard(textBuilder.ToString());
                Thread.Sleep(50);
            }
            
            return true;
        }
        
        // Обрабатывает строку в обычном режиме
        private bool ProcessLine(string line)
        {
            int pos = 0;
            while (pos < line.Length)
            {
                if (cancelRequested) return false;
                
                // Задержка [число]
                if (line[pos] == '[')
                {
                    int endPos = line.IndexOf(']', pos);
                    if (endPos > pos)
                    {
                        string delayStr = line.Substring(pos + 1, endPos - pos - 1);
                        double delay;
                        if (double.TryParse(delayStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out delay))
                        {
                            int totalMs = (int)(delay * 1000);
                            int elapsed = 0;
                            while (elapsed < totalMs)
                            {
                                if (cancelRequested) return false;
                                int sleepTime = Math.Min(50, totalMs - elapsed);
                                Thread.Sleep(sleepTime);
                                elapsed += sleepTime;
                            }
                        }
                        pos = endPos + 1;
                        continue;
                    }
                }
                
                // Специальная клавиша {Key}
                if (line[pos] == '{')
                {
                    int endPos = line.IndexOf('}', pos);
                    if (endPos > pos)
                    {
                        string keyName = line.Substring(pos + 1, endPos - pos - 1);
                        
                        if (keyName.Equals("WAIT", StringComparison.OrdinalIgnoreCase))
                            return false;
                        
                        // Пропускаем команды управления потоком
                        if (keyName.StartsWith("LABEL=", StringComparison.OrdinalIgnoreCase) ||
                            keyName.StartsWith("BRANCH=", StringComparison.OrdinalIgnoreCase) ||
                            keyName.Equals("END_BRANCH", StringComparison.OrdinalIgnoreCase) ||
                            keyName.StartsWith("GOTO=", StringComparison.OrdinalIgnoreCase) ||
                            keyName.StartsWith("WAIT_BRANCH=", StringComparison.OrdinalIgnoreCase))
                        {
                            pos = endPos + 1;
                            continue;
                        }
                        
                        ProcessSpecialCommand(keyName);
                        
                        pos = endPos + 1;
                        continue;
                    }
                }
                
                // Обычный текст
                int nextSpecial = line.Length;
                int nextBracket = line.IndexOf('[', pos);
                int nextBrace = line.IndexOf('{', pos);
                
                if (nextBracket >= 0) nextSpecial = Math.Min(nextSpecial, nextBracket);
                if (nextBrace >= 0) nextSpecial = Math.Min(nextSpecial, nextBrace);
                
                if (nextSpecial > pos)
                {
                    string text = line.Substring(pos, nextSpecial - pos);
                    InputSimulator.TypeTextFast(text, ref cancelRequested);
                    pos = nextSpecial;
                }
                else
                {
                    pos++;
                }
            }
            
            return true;
        }
        
        // Обрабатывает специальные команды (мышь, клавиши)
        private void ProcessSpecialCommand(string keyName)
        {
            if (keyName.Equals("GET_MOUSE", StringComparison.OrdinalIgnoreCase))
            {
                InputSimulator.GetMousePosition();
            }
            else if (keyName.StartsWith("MOVE_MOUSE=", StringComparison.OrdinalIgnoreCase))
            {
                string coords = keyName.Substring(11);
                string[] parts = coords.Split(';');
                if (parts.Length == 2)
                {
                    int x, y;
                    if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                        InputSimulator.MoveMouse(x, y);
                }
            }
            else if (keyName.Equals("CLICK_MOUSE", StringComparison.OrdinalIgnoreCase))
            {
                InputSimulator.ClickMouse("LEFT");
            }
            else if (keyName.StartsWith("CLICK_MOUSE=", StringComparison.OrdinalIgnoreCase))
            {
                string coords = keyName.Substring(12);
                string[] parts = coords.Split(';');
                if (parts.Length == 2)
                {
                    int x, y;
                    if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                        InputSimulator.ClickMouseAt(x, y, "LEFT");
                }
            }
            else if (keyName.Equals("RIGHT_CLICK", StringComparison.OrdinalIgnoreCase))
            {
                InputSimulator.ClickMouse("RIGHT");
            }
            else if (keyName.StartsWith("RIGHT_CLICK=", StringComparison.OrdinalIgnoreCase))
            {
                string coords = keyName.Substring(12);
                string[] parts = coords.Split(';');
                if (parts.Length == 2)
                {
                    int x, y;
                    if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                        InputSimulator.ClickMouseAt(x, y, "RIGHT");
                }
            }
            else if (keyName.Equals("MIDDLE_CLICK", StringComparison.OrdinalIgnoreCase))
            {
                InputSimulator.ClickMouse("MIDDLE");
            }
            else if (keyName.StartsWith("MIDDLE_CLICK=", StringComparison.OrdinalIgnoreCase))
            {
                string coords = keyName.Substring(13);
                string[] parts = coords.Split(';');
                if (parts.Length == 2)
                {
                    int x, y;
                    if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                        InputSimulator.ClickMouseAt(x, y, "MIDDLE");
                }
            }
            else
            {
                InputSimulator.PressKey(keyName);
            }
        }
    }