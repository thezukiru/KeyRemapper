@echo off
echo KeyRemapper - compiling with new architecture...
echo.
echo Using compiler: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

REM Compiling all files from the new structure (UI\MainWindow.cs and UI\StyledControls.cs added)
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
  echo Compilation successful! Created KeyRemapper.exe
  echo.
  echo To run: KeyRemapper.exe
) else (
  echo.
  echo Compilation error!
)
pause
