using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// Класс для парсинга конфигурационного файла
public static class ConfigParser
    {
        // Загружает биндинги из config.txt
        public static List<KeyBinding> LoadConfig(string configFile)
        {
            List<KeyBinding> bindings = new List<KeyBinding>();
            
            if (!File.Exists(configFile))
            {
                Console.WriteLine("Конфигурационный файл не найден: " + configFile);
                return bindings;
            }

            string[] lines = File.ReadAllLines(configFile);
            
            // Парсим каждую строку конфига
            foreach (string line in lines)
            {
                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                    continue;

                // Разделяем по знаку "="
                string[] parts = line.Split('=');
                if (parts.Length != 2)
                    continue;

                string keyCombo = parts[0].Trim();
                string instructionFile = parts[1].Trim();

                // Парсим комбинацию клавиш
                KeyBinding binding = ParseKeyCombo(keyCombo, instructionFile);
                if (binding != null)
                {
                    bindings.Add(binding);
                    Console.WriteLine("Загружен биндинг: " + keyCombo + " -> " + instructionFile);
                }
            }

            return bindings;
        }

        // Парсит строку типа "Ctrl+Shift+E" в объект KeyBinding
        private static KeyBinding ParseKeyCombo(string combo, string instructionFile)
        {
            KeyBinding binding = new KeyBinding { InstructionFile = instructionFile };
            
            // Разделяем по "+"
            string[] parts = combo.Split('+');
            
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                
                // Проверяем модификаторы
                if (trimmed.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                    binding.Ctrl = true;
                else if (trimmed.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    binding.Shift = true;
                else if (trimmed.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    binding.Alt = true;
                else
                {
                    // Пытаемся распарсить основную клавишу
                    try
                    {
                        // Специальная обработка для цифр 0-9
                        if (trimmed.Length == 1 && char.IsDigit(trimmed[0]))
                        {
                            // Преобразуем "1" в Keys.D1, "2" в Keys.D2 и т.д.
                            binding.Key = (Keys)Enum.Parse(typeof(Keys), "D" + trimmed, true);
                        }
                        else
                        {
                            binding.Key = (Keys)Enum.Parse(typeof(Keys), trimmed, true);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Неизвестная клавиша: " + trimmed);
                        return null;
                    }
                }
            }

            // Проверяем что основная клавиша указана
            if (binding.Key == Keys.None)
            {
                Console.WriteLine("Не указана основная клавиша в комбинации: " + combo);
                return null;
            }

            return binding;
        }

        // Загружает текстовые макросы (Text Expander) из config.txt
        // Формат строки: "триггер"=файл.txt
        public static List<TextMacro> LoadTextMacros(string configFile)
        {
            List<TextMacro> macros = new List<TextMacro>();

            if (!File.Exists(configFile))
            {
                Console.WriteLine("Конфигурационный файл не найден: " + configFile);
                return macros;
            }

            string[] lines = File.ReadAllLines(configFile);

            foreach (string line in lines)
            {
                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                    continue;

                // Парсим формат: "триггер"=файл.txt
                Match match = Regex.Match(line, @"""(.+?)""\s*=\s*(.+\.txt)");
                if (match.Success)
                {
                    string trigger = match.Groups[1].Value;
                    string filePath = match.Groups[2].Value.Trim();

                    if (File.Exists(filePath))
                    {
                        string content = File.ReadAllText(filePath);
                        macros.Add(new TextMacro
                        {
                            Trigger = trigger,
                            FilePath = filePath,
                            Content = content,
                            UseClipboard = content.StartsWith("!")
                        });
                        Console.WriteLine("Загружен макрос: '" + trigger + "' -> " + filePath);
                    }
                    else
                    {
                        Console.WriteLine("Файл для макроса не найден: " + filePath);
                    }
                }
            }

            return macros;
        }
    }
