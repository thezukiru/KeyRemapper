using System.Windows.Forms;

// Класс для хранения информации о биндинге клавиш
public class KeyBinding
{
    public Keys Key { get; set; }           // Основная клавиша
    public bool Ctrl { get; set; }          // Нужен ли Ctrl
    public bool Shift { get; set; }         // Нужен ли Shift
    public bool Alt { get; set; }           // Нужен ли Alt
    public string InstructionFile { get; set; }  // Файл с инструкциями

    // Проверяет, соответствует ли нажатие клавиш этому биндингу
    public bool Matches(Keys pressedKey, bool ctrlPressed, bool shiftPressed, bool altPressed)
    {
        return Key == pressedKey && 
               Ctrl == ctrlPressed && 
               Shift == shiftPressed && 
               Alt == altPressed;
    }
}
