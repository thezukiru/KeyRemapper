using System.Text;

// Отслеживает ввод символов и ведёт буфер последних ~50 символов
// Используется TextExpander для определения триггеров
public class InputTracker
{
    private StringBuilder buffer = new StringBuilder();
    private const int MAX_SIZE = 50;

    // Добавляет символ в буфер (с ротацией при переполнении)
    public void AddCharacter(char c)
    {
        if (buffer.Length >= MAX_SIZE)
            buffer.Remove(0, 1); // Удаляем самый старый символ
        buffer.Append(c);
    }

    // Возвращает текущее содержимое буфера
    public string GetBuffer()
    {
        return buffer.ToString();
    }

    // Очищает буфер (после срабатывания макроса)
    public void Clear()
    {
        buffer.Clear();
    }

    // Полный сброс (при потере фокуса GTA)
    public void Reset()
    {
        buffer.Clear();
    }
}