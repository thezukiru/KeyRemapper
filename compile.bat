@echo off
echo KeyRemapper - компиляция с новой архитектурой...
echo Используется компилятор: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

REM Компилируем все файлы из новой структуры (UI\MainWindow.cs и UI\StyledControls.cs добавлены)
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:KeyRemapper.exe ^
    Models\KeyBinding.cs ^
    Core\ConfigParser.cs ^
    Core\InputSimulator.cs ^
    Core\GtaMonitor.cs ^
    Core\BranchingEngine.cs ^
    Core\Overlay.cs ^
    Core\ScriptEngine.cs ^
    Core\InstructionExecutor.cs ^
    UI\StyledControls.cs ^
    UI\MainWindow.cs ^
    KeyRemapper.cs ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll

if %errorlevel% equ 0 (
    echo.
    echo Компиляция успешна! Создан файл KeyRemapper.exe
    echo.
    echo Для запуска выполните: KeyRemapper.exe
) else (
    echo.
    echo Ошибка компиляции!
)

pause
