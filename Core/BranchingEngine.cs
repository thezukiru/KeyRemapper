using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;


    // Класс для обработки ветвления скриптов (метки, ветки, GOTO)
    public class BranchingEngine
    {
        // Структура для хранения информации о ветке
        public class BranchInfo
        {
            public int StartLine { get; set; }
            public int EndLine { get; set; }
            public string Name { get; set; }
        }
        
        private Dictionary<string, int> labels = new Dictionary<string, int>();
        private Dictionary<string, BranchInfo> branches = new Dictionary<string, BranchInfo>();
        public List<string> Breadcrumbs { get; private set; }
        public int LoopIterationCount { get; private set; }
        public string CurrentLoopLabel { get; private set; }
        
        private bool cancelRequested = false;
        
        // Конструктор для инициализации свойств (C# 5 совместимость)
        public BranchingEngine()
        {
            Breadcrumbs = new List<string>();
            LoopIterationCount = 0;
            CurrentLoopLabel = null;
        }
        
        // Парсит метки и ветки из скрипта
        public void ParseLabelsAndBranches(string[] lines)
        {
            labels.Clear();
            branches.Clear();
            Breadcrumbs.Clear();
            LoopIterationCount = 0;
            CurrentLoopLabel = null;
            
            Stack<BranchInfo> branchStack = new Stack<BranchInfo>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;
                
                // Ищем {LABEL=имя}
                if (line.Contains("{LABEL="))
                {
                    int startIdx = line.IndexOf("{LABEL=") + 7;
                    int endIdx = line.IndexOf('}', startIdx);
                    if (endIdx > startIdx)
                    {
                        string labelName = line.Substring(startIdx, endIdx - startIdx).Trim();
                        if (!labels.ContainsKey(labelName))
                        {
                            labels[labelName] = i;
                            Console.WriteLine("Найдена метка: " + labelName + " на строке " + i);
                        }
                        else
                        {
                            Console.WriteLine("ПРЕДУПРЕЖДЕНИЕ: Дублирующаяся метка " + labelName + " на строке " + i);
                        }
                    }
                }
                
                // Ищем {BRANCH=имя}
                if (line.Contains("{BRANCH="))
                {
                    int startIdx = line.IndexOf("{BRANCH=") + 8;
                    int endIdx = line.IndexOf('}', startIdx);
                    if (endIdx > startIdx)
                    {
                        string branchName = line.Substring(startIdx, endIdx - startIdx).Trim();
                        BranchInfo branch = new BranchInfo
                        {
                            Name = branchName,
                            StartLine = i,
                            EndLine = -1
                        };
                        branchStack.Push(branch);
                        Console.WriteLine("Начало ветки: " + branchName + " на строке " + i);
                    }
                }
                
                // Ищем {END_BRANCH}
                if (line.Contains("{END_BRANCH}"))
                {
                    if (branchStack.Count > 0)
                    {
                        BranchInfo branch = branchStack.Pop();
                        branch.EndLine = i;
                        branches[branch.Name] = branch;
                        Console.WriteLine("Конец ветки: " + branch.Name + " на строке " + i);
                    }
                    else
                    {
                        Console.WriteLine("ПРЕДУПРЕЖДЕНИЕ: {END_BRANCH} без соответствующего {BRANCH=...} на строке " + i);
                    }
                }
            }
            
            if (branchStack.Count > 0)
            {
                Console.WriteLine("ПРЕДУПРЕЖДЕНИЕ: Найдены незакрытые ветки:");
                foreach (BranchInfo branch in branchStack)
                {
                    Console.WriteLine("  - " + branch.Name + " начинается на строке " + branch.StartLine);
                }
            }
        }
        
        // Обрабатывает команды ветвления
        public int ProcessBranchingCommands(string line, int currentLine, string[] lines, ref bool shouldStop)
        {
            string trimmed = line.Trim();
            
            // Пропускаем LABEL, BRANCH, END_BRANCH
            if (trimmed.Contains("{LABEL=") || trimmed.Contains("{BRANCH=") || trimmed.Contains("{END_BRANCH}"))
            {
                return currentLine + 1;
            }
            
            // Обработка {GOTO=...}
            if (trimmed.Contains("{GOTO="))
            {
                return ProcessGotoCommand(trimmed, currentLine, lines, ref shouldStop);
            }
            
            // Обработка {WAIT_BRANCH=...}
            if (trimmed.Contains("{WAIT_BRANCH="))
            {
                return ProcessWaitBranchCommand(trimmed, currentLine);
            }
            
            return currentLine;
        }
        
        // Обрабатывает команду GOTO
        private int ProcessGotoCommand(string line, int currentLine, string[] lines, ref bool shouldStop)
        {
            int startIdx = line.IndexOf("{GOTO=") + 6;
            int endIdx = line.IndexOf('}', startIdx);
            if (endIdx <= startIdx) return currentLine + 1;
            
            string args = line.Substring(startIdx, endIdx - startIdx);
            string[] parts = args.Split(';');
            
            if (parts.Length == 0) return currentLine + 1;
            
            string targetLabel = parts[0].Trim();
            
            // Безусловный переход
            if (parts.Length == 1)
            {
                if (labels.ContainsKey(targetLabel))
                {
                    // Проверяем на цикл
                    if (labels[targetLabel] <= currentLine)
                    {
                        if (CurrentLoopLabel == targetLabel)
                        {
                            LoopIterationCount++;
                        }
                        else
                        {
                            CurrentLoopLabel = targetLabel;
                            LoopIterationCount = 1;
                        }
                        
                        // Защита от бесконечных циклов
                        if (LoopIterationCount > 100)
                        {
                            Console.WriteLine("ПРЕДУПРЕЖДЕНИЕ: Превышен лимит итераций цикла (100). Остановка.");
                            shouldStop = true;
                            return lines.Length;
                        }
                    }
                    
                    Console.WriteLine("GOTO -> " + targetLabel);
                    return labels[targetLabel] + 1;
                }
                else
                {
                    Console.WriteLine("ОШИБКА: Метка не найдена: " + targetLabel);
                    return currentLine + 1;
                }
            }
            
            // Условный переход - ждем нажатия клавиши
            Console.WriteLine("Ожидание выбора для GOTO...");
            Dictionary<Keys, string> keyActions = new Dictionary<Keys, string>();
            
            for (int i = 1; i < parts.Length; i++)
            {
                string[] keyValue = parts[i].Split('=');
                if (keyValue.Length == 2)
                {
                    string action = keyValue[0].Trim().ToUpper();
                    string keyStr = keyValue[1].Trim().ToUpper();
                    Keys key = ParseKeyString(keyStr);
                    
                    if (key != Keys.None)
                    {
                        keyActions[key] = action;
                        Console.WriteLine("  " + keyStr + " -> " + action);
                    }
                }
            }
            
            Keys pressedKey = WaitForKeyPress(keyActions.Keys.ToArray());
            
            if (keyActions.ContainsKey(pressedKey))
            {
                string action = keyActions[pressedKey];
                
                if (action == "YES" || action == "CONFIRM" || action == "CONTINUE")
                {
                    if (labels.ContainsKey(targetLabel))
                    {
                        Console.WriteLine("Выбрано: " + action + " -> " + targetLabel);
                        return labels[targetLabel] + 1;
                    }
                }
                else if (action == "NO" || action == "CANCEL")
                {
                    Console.WriteLine("Выбрано: " + action + " -> продолжить");
                    return currentLine + 1;
                }
                else if (action == "SKIP")
                {
                    Console.WriteLine("Выбрано: SKIP -> пропуск блока");
                    return FindNextLabel(currentLine, lines);
                }
                else if (action == "REPEAT")
                {
                    if (labels.ContainsKey(targetLabel))
                    {
                        LoopIterationCount++;
                        if (LoopIterationCount > 100)
                        {
                            Console.WriteLine("ПРЕДУПРЕЖДЕНИЕ: Превышен лимит итераций цикла (100).");
                            return currentLine + 1;
                        }
                        Console.WriteLine("Выбрано: REPEAT -> " + targetLabel + " (итерация " + LoopIterationCount + ")");
                        return labels[targetLabel] + 1;
                    }
                }
                else if (labels.ContainsKey(action))
                {
                    Console.WriteLine("Выбрано: переход к " + action);
                    return labels[action] + 1;
                }
            }
            
            return currentLine + 1;
        }
        
        // Обрабатывает команду WAIT_BRANCH
        private int ProcessWaitBranchCommand(string line, int currentLine)
        {
            int startIdx = line.IndexOf("{WAIT_BRANCH=") + 13;
            int endIdx = line.IndexOf('}', startIdx);
            if (endIdx <= startIdx) return currentLine + 1;
            
            string args = line.Substring(startIdx, endIdx - startIdx);
            string[] parts = args.Split(';');
            
            Console.WriteLine("Ожидание выбора ветки...");
            Dictionary<Keys, string> keyBranches = new Dictionary<Keys, string>();
            
            foreach (string part in parts)
            {
                string[] keyValue = part.Split(':');
                if (keyValue.Length == 2)
                {
                    string keyStr = keyValue[0].Trim().ToUpper();
                    string branchName = keyValue[1].Trim();
                    Keys key = ParseKeyString(keyStr);
                    
                    if (key != Keys.None)
                    {
                        keyBranches[key] = branchName;
                        Console.WriteLine("  " + keyStr + " -> ветка " + branchName);
                    }
                }
            }
            
            Keys pressedKey = WaitForKeyPress(keyBranches.Keys.ToArray());
            
            if (keyBranches.ContainsKey(pressedKey))
            {
                string branchName = keyBranches[pressedKey];
                
                if (branches.ContainsKey(branchName))
                {
                    BranchInfo branch = branches[branchName];
                    Console.WriteLine("Выбрана ветка: " + branchName);
                    Breadcrumbs.Add(branchName);
                    return branch.StartLine + 1;
                }
                else
                {
                    Console.WriteLine("ОШИБКА: Ветка не найдена: " + branchName);
                }
            }
            
            return currentLine + 1;
        }
        
        // Ждет нажатия клавиши
        private Keys WaitForKeyPress(Keys[] allowedKeys)
        {
            Console.WriteLine("Ожидание нажатия клавиши...");
            
            while (true)
            {
                if (cancelRequested)
                    return Keys.Escape;
                
                foreach (Keys key in allowedKeys)
                {
                    if ((InputSimulator.GetAsyncKeyState((int)key) & 0x8000) != 0)
                    {
                        while ((InputSimulator.GetAsyncKeyState((int)key) & 0x8000) != 0)
                        {
                            Thread.Sleep(10);
                        }
                        return key;
                    }
                }
                
                if ((InputSimulator.GetAsyncKeyState((int)Keys.Escape) & 0x8000) != 0)
                {
                    while ((InputSimulator.GetAsyncKeyState((int)Keys.Escape) & 0x8000) != 0)
                    {
                        Thread.Sleep(10);
                    }
                    cancelRequested = true;
                    return Keys.Escape;
                }
                
                Thread.Sleep(50);
            }
        }
        
        // Парсит строку клавиши
        private Keys ParseKeyString(string keyStr)
        {
            try
            {
                if (keyStr == "RSHIFT") return Keys.RShiftKey;
                if (keyStr == "LSHIFT") return Keys.LShiftKey;
                if (keyStr == "RCTRL") return Keys.RControlKey;
                if (keyStr == "LCTRL") return Keys.LControlKey;
                if (keyStr == "RALT") return Keys.RMenu;
                if (keyStr == "LALT") return Keys.LMenu;
                
                if (keyStr.StartsWith("F") && keyStr.Length <= 3)
                {
                    return (Keys)Enum.Parse(typeof(Keys), keyStr, true);
                }
                
                return (Keys)Enum.Parse(typeof(Keys), keyStr, true);
            }
            catch
            {
                Console.WriteLine("Неизвестная клавиша: " + keyStr);
                return Keys.None;
            }
        }
        
        // Находит следующую метку
        private int FindNextLabel(int currentLine, string[] lines)
        {
            for (int i = currentLine + 1; i < lines.Length; i++)
            {
                if (lines[i].Contains("{LABEL="))
                {
                    return i;
                }
            }
            return lines.Length;
        }
        
        // Очищает состояние
        public void Clear()
        {
            Breadcrumbs.Clear();
            LoopIterationCount = 0;
            CurrentLoopLabel = null;
        }
        
        // Устанавливает флаг отмены
        public void RequestCancel()
        {
            cancelRequested = true;
        }
        
        // Сбрасывает флаг отмены
        public void ResetCancel()
        {
            cancelRequested = false;
        }
    }
