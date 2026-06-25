using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

// Основной движок Text Expander
// Отслеживает ввод символов, проверяет триггеры и выполняет замену
public class TextExpander
{
    private List<TextMacro> macros = new List<TextMacro>();
    private InputTracker tracker = new InputTracker();
    private bool isExpanding = false; // Защита от повторного срабатывания

    // Загружает список макросов (вызывается при старте и перезагрузке конфига)
    public void LoadMacros(List<TextMacro> newMacros)
    {
        macros = newMacros;
        tracker.Clear();
        Console.WriteLine("TextExpander: загружено " + macros.Count + " макросов");
    }

    // Возвращает количество загруженных макросов
    public int MacroCount
    {
        get { return macros.Count; }
    }

    // Обрабатывает нажатие клавиши. Возвращает true, если нужно подавить клавишу.
    // Вызывается из HookCallback только когда GTA активна.
    public bool ProcessKeyPress(Keys key, char character)
    {
        if (isExpanding) return false;

        // Добавляем символ в буфер (если это печатаемый символ)
        if (character != '\0')
        {
            tracker.AddCharacter(character);
        }

        // Проверяем завершающие клавиши (Enter, Space, Tab)
        if (IsTriggerKey(key))
        {
            return CheckAndExpand(key);
        }

        return false;
    }

    // Сбрасывает буфер при потере фокуса GTA
    public void Reset()
    {
        tracker.Reset();
    }

    // Проверяет, является ли клавиша завершающей для триггера
    private bool IsTriggerKey(Keys key)
    {
        return key == Keys.Enter || key == Keys.Space || key == Keys.Tab;
    }

    // Проверяет буфер на совпадение с триггерами и выполняет макрос
    // Возвращает true, если клавишу нужно подавить
    private bool CheckAndExpand(Keys triggerKey)
    {
        string buffer = tracker.GetBuffer();

        foreach (var macro in macros)
        {
            if (macro.Matches(buffer))
            {
                // Найдено совпадение! Запускаем макрос
                ExecuteMacro(macro, triggerKey);
                tracker.Clear();
                return true; // Подавляем Enter/Space
            }
        }

        return false;
    }

    // Выполняет макрос: удаляет триггер + вставляет текст
    private void ExecuteMacro(TextMacro macro, Keys triggerKey)
    {
        isExpanding = true;

        try
        {
            Console.WriteLine("TextExpander: сработал триггер '" + macro.Trigger + "' -> " + macro.FilePath);

            // 1. Удаляем триггер (Backspace × длина триггера)
            InputSimulator.SendBackspaces(macro.Trigger.Length);
            Thread.Sleep(50);

            // 2. Вставляем текст из файла
            bool cancelFlag = false;
            if (macro.UseClipboard)
                InputSimulator.PasteViaClipboard(macro.Content);
            else
                InputSimulator.TypeTextFast(macro.Content, ref cancelFlag);

            Thread.Sleep(50);

            // 3. Если завершающая клавиша была Enter — отправляем Enter (для отправки в чат)
            if (triggerKey == Keys.Enter)
            {
                InputSimulator.PressKey("Enter");
            }
            // Если Space — вставляем пробел (чтобы текст не склеился со следующим словом)
            else if (triggerKey == Keys.Space)
            {
                InputSimulator.PressKey("Space");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("TextExpander: ошибка при выполнении макроса: " + ex.Message);
        }
        finally
        {
            isExpanding = false;
        }
    }
}