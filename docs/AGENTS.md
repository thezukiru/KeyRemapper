# 🤖 KeyRemapper — AI Agent Instructions

> **Role:** Expert .NET Developer & Systems Architect  
> **Project:** KeyRemapper (GTA San Andreas Macro & Key Mapping Tool)  
> **Version:** v0.0.1 (Stable)  
> **Last Updated:** 2026-06-17 (YYYY.MM.DD)

---

## 📋 1. Project Overview
**KeyRemapper** is a Windows utility designed to remap keyboard/mouse inputs and execute macros specifically optimized for **GTA San Andreas**.
It features global hooks, macro recording, script branching, and an overlay system.

**Core Features:**
- Global Keyboard/Mouse Hooks (`LowLevelKeyboardProc`, `LowLevelMouseProc`)
- Macro Engine (Delays, Branches, Variables, Loops)
- Overlay UI (Visual feedback in-game)
- GTA SA Process Detection & Validation

---

## 🏗 2. Architecture
The project follows a modular architecture to separate UI logic from core processing.

```
KeyRemapper/
├── Core/                # Business logic, hooks, macro engine
│   ├── MacroEngine.cs   # Script execution logic
│   ├── HookManager.cs   # Global input hooks
│   └── Overlay.cs       # Visual overlay management
├── Models/              # Data structures (Macro, ScriptNode, Config)
├── UI/                  # WinForms (MainForm, Settings, etc.)
└── Utils/               # Helpers (Logger, ProcessValidator)
```

**Key Principles:**
- **Separation of Concerns:** UI must *never* contain macro execution logic.
- **Isolation:** Core logic should be testable without UI.

---

## ⚠️ 3. Critical Rules (READ FIRST)
> 🛑 **VIOLATING THESE RULES WILL BREAK THE APPLICATION.**

1. **NEVER Block the UI Thread:**
   - All heavy operations (hooks, delays, script execution, file I/O) **MUST** run on background threads (`Task.Run` or `BackgroundWorker`).
   - UI updates **MUST** use `Invoke` or `BeginInvoke`.
2. **Process Validation:**
   - Before injecting hooks or reading memory, **ALWAYS** check if `gta_sa.exe` is running and responsive.
   - Use `Process.GetProcessesByName("gta_sa")`. Handle `HasExited` safely.
3. **Hook Safety:**
   - Always return `CallNextHookEx` in hook procedures.
   - Ensure hooks are properly unhooked on exit to prevent system instability.
   - Isolate hook logic in `try-catch` blocks to prevent crashes from bubbling up to Windows.
4. **Delay Handling:**
   - **NEVER** use `Thread.Sleep` in the macro engine. Use `Task.Delay` for asynchronous non-blocking waits.
5. **Null Safety:**
   - Always check for null before accessing process handles or UI controls.

---

## 💻 4. Coding Standards
- **Language:** C# 12 / .NET 8.0
- **Naming:**
  - `PascalCase` for classes, methods, properties.
  - `camelCase` for local variables, parameters, private fields (prefix with `_` if private field).
- **Async/Await:** Prefer `async Task` over `void` for event handlers where possible.
- **Error Handling:**
  - Use specific exceptions (`InvalidOperationException`, `IOException`).
  - Log errors using the centralized Logger.
  - **No empty catch blocks.**
- **Comments:** XML docs for public methods. Inline comments for complex Windows API calls.

---

## 🧠 5. Workflow for AI Agent
1. **Analyze:** Read relevant files. Understand the context. **Do not rewrite code unnecessarily.**
2. **Plan:** For complex features, outline the steps and file changes.
3. **Implement:**
   - Make changes incrementally.
   - Follow **Critical Rules**.
   - Maintain backward compatibility.
4. **Verify:**
   - Check for compilation errors.
   - Ensure UI thread is not blocked.
   - Verify hooks are managed correctly.
5. **Commit:** Use semantic commit messages (`feat:`, `fix:`, `refactor:`, `docs:`).

---

## 📜 6. Lessons Learned (History of Fixes)
> 📂 **Refer to these to avoid repeating past mistakes.**
- **v0.3 Fix 1:** `Thread.Sleep` caused UI freezing → Replaced with `Task.Delay`.
- **v0.3 Fix 2:** Hooking before GTA check caused crashes → Moved validation before `Task.Run`.
- **v0.3 Fix 3:** Hook unhooking failed on exit → Added proper cleanup in `FormClosing`.
- **v0.3 Fix 4:** `Process.HasExited` threw exceptions → Wrapped in try-catch/safe checks.
- **v0.3 Fix 5:** Global try-catch masked errors → Isolated catch blocks per logical unit.
- **v0.3 UI Update:** PowerShell command chaining with `&&` is not supported → Use `;` or separate commands instead (e.g., `cd path; .\compile.bat`).
- **v0.3 UI Update:** C# 5 compiler (.NET Framework 4.0) does not support expression-bodied members (`=>`) → Use traditional getter/setter syntax.
- **v0.3.1 Fix 1:** C# 5 does not support string interpolation (`$"..."`) → Use concatenation instead.
- **v0.3.1 Feature 1:** GTA SA status indicators split into two: process running vs window active.
- **v0.3.1 Feature 2:** Overlay auto-mode flag added for automatic show/hide during script execution.
- **v0.3.1 Fix 2:** Esc key cancellation logic separated from Win key handling for better reliability.
- **v0.3.2 Fix 1:** Overlay freezing on first script launch → `ShowWithoutAnimation()` now uses `BeginInvoke` instead of direct call to avoid cross-thread deadlock.

---

## 🗺 7. Roadmap
- [ ] **Stability Testing:** Rapid key presses, minimize/restore stress test.
- [ ] **Variables Engine:** Support `{DATE}`, `{TIME}`, `{CLIPBOARD}`, `{RANDOM}`.
- [ ] **GUI Overhaul:** Dedicated macro editor, settings persistence.
- [ ] **Advanced Mouse:** Smooth movement, relative coordinates.
- [ ] **Optimization:** Script caching, adaptive delays.

---

## 📝 8. Context & Memory
- **State Storage:** Agent state is tracked in `.agent_state/`.
- **Documentation:** See `DOCUMENTATION.md` for detailed feature specs.
- **Config:** User settings stored in `config.txt` (project root).

---

## 🧠 9. Project Notes (AI Agent Memory)

### Текущее состояние проекта (v0.3.2 Stable)

#### Git: ветки и теги (2026-05-12)
- **main** — основная ветка разработки (текущая HEAD = `d0705a0`)
- **v0.3.2-stable** — ветка-сохранение стабильной версии v0.3.2
- **literate-polonium** — старая ветка (documentation by claude)
- **Тег `v0.3.2`** — указывает на коммит `d0705a0` (release: v0.3.2 stable)
- **Как восстановить v0.3.2:** `git checkout v0.3.2` или `git checkout v0.3.2-stable`

#### Архитектура (актуальная на 2026-05-11)

**Основные файлы:**
- `KeyRemapper.cs` - Точка входа, глобальный хук клавиатуры (`WH_KEYBOARD_LL`), обработка Win/Esc для экстренного закрытия
- `Core/ScriptEngine.cs` - Главный движок выполнения скриптов (587 строк), асинхронное выполнение, overlay updates
- `Core/InstructionExecutor.cs` - Адаптер-обертка над ScriptEngine, управление overlay autoMode (110+ строк)
- `Core/BranchingEngine.cs` - Обработка меток ({LABEL=}), веток ({BRANCH=}/{END_BRANCH}), GOTO, WAIT_BRANCH
- `Core/Overlay.cs` - OverlayForm с fade-анимацией, breadcrumbs, drag-and-drop позиционированием (524 строки)
- `Core/GtaMonitor.cs` - Проверка наличия процесса (`IsGtaSaRunning()`) и активности окна (`IsGtaSaActive()`) с кэшированием (100мс)
- `Core/InputSimulator.cs` - Эмуляция ввода: keybd_event, clipboard paste, мышь (273 строки)
- `Core/ConfigParser.cs` - Парсинг config.txt (формат: `Ctrl+Shift+E=file.txt`)
- `UI/MainWindow.cs` - Главное окно в стиле Province Helper Lite (717 строк)
- `UI/StyledControls.cs` - Кастомные контролы: RoundedButton, StyledTextBox, StatusIndicator, RoundedPanel
- `Models/KeyBinding.cs` - Модель биндинга (Key, Ctrl, Shift, Alt, InstructionFile)

#### Ключевые особенности реализации

**Потокобезопасность:**
- ScriptEngine использует `executionLock` для синхронизации
- volatile флаги `cancelRequested`, `isExecuting`
- UI обновления через `Invoke` в OverlayForm

**Delay handling:**
- ⚠️ **ВАЖНО:** В ScriptEngine используется `Thread.Sleep` внутри ExecuteLines (строки 337, 351, 358, 374, 399, 427, 458, 460)
- Это потенциально может блокировать поток выполнения, но не UI thread (скрипт выполняется в Task.Run)
- Рекомендуется миграция на `Task.Delay` при рефакторинге

**Overlay система:**
- Показывает 7 строк: 3 прошлые + текущая + 3 следующие
- Breadcrumbs отображают путь выполнения веток
- Позиция сохраняется в `overlay_position.txt`
- Fade анимация через Timer (20ms interval)

**GTA SA валидация:**
- Проверка перед запуском скрипта
- Проверка каждые 3 строки (GTA_CHECK_INTERVAL = 3)
- Кэширование результата на 100мс в GtaMonitor

**GTA SA статус индикаторы (обновлено v0.3.1):**
- Два независимых индикатора в MainWindow: 1. `gtaProcessIndicator` - показывает наличие процесса gta_sa.exe (зелёный/красный)
  2. `gtaActiveIndicator` - показывает активность окна GTA SA (зелёный/оранжевый/красный)
- Оранжевый цвет для "Окно: не активно" означает что процесс запущен, но окно не в фокусе

**Overlay система (обновлено v0.3.1):**
- Добавлен флаг `overlayAutoMode` в InstructionExecutor
- По умолчанию: overlay автоматически показывается при старте скрипта и скрывается при завершении
- Кнопка "Overlay" в GUI переключает между авто и ручным режимом
- В ручном режиме overlay остаётся видимым для позиционирования даже после завершения скрипта

**Отмена скрипта:**
- 3x Win за секунду → Environment.Exit(0) (не трогать!)
- 3x Esc за секунду → CancelWaiting() или RequestCancel() (исправлено в v0.3.1)
- Right Shift → ContinueExecution() (пауза {WAIT})
- Логика обработки Esc вынесена в отдельный блок от Win для надёжности

#### Известные ограничения
- Теги рации в MainWindow пока заглушка (не сохраняются), и созданы для будущего обновления
- Добавление биндингов только через редактирование config.txt, до будущего обновления
- Thread.Sleep в ScriptEngine вместо Task.Delay

#### Roadmap приоритеты
1. [ ] Миграция Thread.Sleep → Task.Delay в ScriptEngine
2. [ ] Сохранение тегов рации в config
3. [ ] GUI редактор биндингов
4. [ ] Variables Engine ({DATE}, {TIME}, {CLIPBOARD}, {RANDOM})
5. [ ] **Доработать документацию** до адекватного варианта (DOCUMENTATION.md, PROJECT_SUMMARY.md устарели)
6. [ ] **Создать README.md** для красивого отображения на главной странице GitHub-репозитория

#### Изменения v0.3.1 (2026-05-11)
1. **GtaMonitor.cs**: Добавлен метод `IsGtaSaRunning()` для проверки наличия процесса без проверки активности окна
2. **MainWindow.cs**: 
   - Заменён один индикатор `gtaStatusIndicator` на два: `gtaProcessIndicator` и `gtaActiveIndicator`
   - Обновлён метод `UpdateGtaStatus()` для работы с двумя индикаторами
   - Индикатор активности окна показывает оранжевый цвет если процесс запущен но окно не активно
3. **InstructionExecutor.cs**:
   - Добавлен флаг `overlayAutoMode` (по умолчанию true)
   - Добавлены методы `ShowOverlayForScript()` и `HideOverlayForScript()` для управления overlay с учётом режима
   - Метод `ToggleOverlay()` теперь переключает между авто и ручным режимом
   - **Исправление:** `RequestCancel()` и `CancelWaiting()` теперь немедленно скрывают overlay при отмене скрипта
4. **ScriptEngine.cs**:
   - Заменены прямые вызовы `HideOverlay()` на `InstructionExecutor.HideOverlayForScript()`
   - Добавлен вызов `InstructionExecutor.ShowOverlayForScript()` при начале выполнения скрипта
5. **KeyRemapper.cs**:
   - Логика обработки Esc вынесена из общего блока с Win в отдельный блок
   - Добавлен отладочный вывод количества нажатий Esc
   - Исправлена ошибка компиляции: заменена интерполяция строк на конкатенацию (C# 5 совместимость)

#### Изменения v0.3.2 (2026-05-11)
1. **Core/Overlay.cs**: 
   - **Исправление:** Метод `ShowWithoutAnimation()` теперь использует `BeginInvoke` для асинхронного вызова в UI потоке
   - Добавлена проверка `IsHandleCreated` перед вызовом
   - Причина: при первом запуске скрипта overlay "зависал" из-за попытки доступа к UI элементам из фонового потока без маршаллинга
   - `Invoke` заменён на `BeginInvoke` для избежания deadlock
