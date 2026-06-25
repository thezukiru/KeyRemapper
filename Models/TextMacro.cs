using System;

// Модель данных для текстового макроса (Text Expander)
// Триггер — короткая фраза (например, ".текст"), которую пользователь набирает в чате,
// программа заменяет её на содержимое связанного файла
public class TextMacro
{
    // Триггер, который должен набрать пользователь (например, ".текст")
    public string Trigger { get; set; }

    // Путь к файлу с текстом для вставки (например, "hello.txt")
    public string FilePath { get; set; }

    // Содержимое файла (загружается один раз при парсинге конфига)
    public string Content { get; set; }

    // Режим вставки: true — через буфер обмена, false — прямой ввод
    public bool UseClipboard { get; set; }

    // Проверяет, заканчивается ли буфер на этот триггер (без учёта регистра)
    public bool Matches(string buffer)
    {
        if (string.IsNullOrEmpty(Trigger) || string.IsNullOrEmpty(buffer))
            return false;
        return buffer.EndsWith(Trigger, StringComparison.OrdinalIgnoreCase);
    }
}