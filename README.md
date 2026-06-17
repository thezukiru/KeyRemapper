<div align="center">

# 🎮 KeyRemapper

**[🇷🇺 Русский](README_RU.md)**

### Text Input Automation for GTA SA RP

**Keystroke interception → Script execution → Automatic input**

[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows/)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)

</div>

---

## 📋 Table of Contents

- [What is it](#-what-is-it)
- [Features](#-features)
- [Quick Start](#-quick-start)
- [Script Syntax](#-script-syntax)
- [Branching System](#-branching-system)
- [Mouse Control](#-mouse-control)
- [Overlay Visualization](#-overlay-visualization)
- [Configuration](#-configuration)
- [Script Examples](#-script-examples)
- [NumPad Keys](#-numpad-keys)
- [Hotkeys](#-hotkeys)
- [Project Structure](#-project-structure)
- [Building](#-building)
- [Known Limitations](#-known-limitations)

---

## 🚀 What is it

**KeyRemapper** is a Windows C# program that intercepts keystrokes and executes auto-typing scripts.

Perfect for **GTA SA roleplay servers** (GTA San Andreas Multiplayer), where you need to quickly type long `/me`, `/do`, `/say` commands and other RP actions.

**How it works:**
1. You bind a key to a script file in `config.txt`
2. Press that key in the game
3. The program automatically types the text from the script with the specified delays

---

## ✨ Features

| Feature | Description |
|:---|:---|
| 🔄 **Multiple Bindings** | Bind dozens of scripts to different keys |
| ⚡ **Two Input Modes** | Fast (clipboard) and universal (character-by-character) |
| 🔀 **Branching System** | Conditional jumps, loops, multiple choice |
| 🏷 **Labels & Goto** | GOTO, LABEL — create complex scenarios |
| 🖱 **Mouse Control** | Clicks, cursor movement by coordinates |
| 📊 **Overlay Visualization** | Semi-transparent window showing execution progress |
| 🎯 **GTA SA Detection** | Scripts only execute when the game window is active |
| 🛡 **Cancel Protocol** | Safe stop with Win/Esc keys |
| 🏗 **Modular Architecture** | Clean code split into logical components |

---

## ⚡ Quick Start

### 1. Build

```bash
compile.bat
```

This creates `KeyRemapper.exe` in the current directory.

### 2. Configure Bindings

Edit the `config.txt` file:

```
# Format: Key=script_file.txt
Ctrl+1=medcard.txt
Ctrl+2=inspection.txt
NumPad5=quick_action.txt
```

### 3. Create a Script

Create a script file (e.g., `medcard.txt`):

```
{T}[0.1]/me takes out a medical card{Enter}
[0.3]{T}[0.1]/say Hello! Here is your medical card.{Enter}
```

### 4. Run

```bash
KeyRemapper.exe
```

The program will start and run in the background. Open GTA SA, press the bound key — the script executes automatically!

---

## 📝 Script Syntax

### Delays

```
[0.5]    — 0.5 second delay
[1]      — 1 second delay
[2.5]    — 2.5 second delay
```

### Special Keys

| Key | Script Notation |
|:---|:---|
| T (open chat) | `{T}` |
| Enter | `{Enter}` |
| Tab | `{Tab}` |
| Escape | `{Esc}` |
| F1–F12 | `{F1}` … `{F12}` |
| Space | `{Space}` |
| Backspace | `{Backspace}` |
| Delete | `{Delete}` |
| Arrow keys | `{Left}`, `{Up}`, `{Right}`, `{Down}` |
| Home / End | `{Home}`, `{End}` |
| PageUp / PageDown | `{PageUp}`, `{PageDown}` |

### Clipboard Mode

Add `!` at the beginning of a line for instant pasting via `Ctrl+V`:

```
!
{T}[0.1]Very long text that will be pasted instantly{Enter}
```

> ⚠️ Clipboard mode **does not work** in some game text chats. Use character-by-character mode instead.

### Plain Text

Any text is typed as-is:

```
/say Hello everyone!
/me walked up to the counter
/do There is a document on the table
```

---

## 🔀 Branching System

The branching system allows you to create **interactive scripts** with action choices during execution.

### Labels and Jumps

```
{LABEL=label_name}          — label declaration
{GOTO=label_name}           — unconditional jump
```

### Conditional Jumps

**Binary choice (yes/no):**
```
{GOTO=branch_yes;YES=RSHIFT;NO=RCTRL}
```

**Confirmation:**
```
{GOTO=branch_yes;CONFIRM=RSHIFT;CANCEL=ESC}
```

**Loops:**
```
{GOTO=loop_start;REPEAT=RCTRL;YES=RSHIFT}
```

**Multiple choice (F1–F12):**
```
{GOTO=default_branch;F1=branch1;F2=branch2;F3=branch3}
```

### Branching

```
{WAIT_BRANCH=RSHIFT:yes;RCTRL:no}

{BRANCH=yes}
  say Option selected!{Enter}
{END_BRANCH}

{BRANCH=no}
  say Cancelled{Enter}
{END_BRANCH}
```

### Available Keys for Conditions

| Key | Code |
|:---|:---|
| Right Shift | `RSHIFT` |
| Left Shift | `LSHIFT` |
| Right Ctrl | `RCTRL` |
| Left Ctrl | `LCTRL` |
| Right Alt | `RALT` |
| Left Alt | `LALT` |
| F1–F12 | `F1` … `F12` |
| Escape | `ESC` |

> ✅ **Russian names** for labels and branches are supported.
> ✅ Protection against infinite loops (limit of 100 iterations).

---

## 🖱 Mouse Control

```
{GET_MOUSE}                  — print mouse coordinates to console
{MOVE_MOUSE=X;Y}             — move cursor to coordinates
{CLICK_MOUSE}                — left click at current position
{CLICK_MOUSE=X;Y}            — move, click, return cursor
{RIGHT_CLICK}                — right click
{RIGHT_CLICK=X;Y}            — move, right click, return
{MIDDLE_CLICK}               — middle click
{MIDDLE_CLICK=X;Y}           — move, middle click, return
```

---

## 📊 Overlay Visualization

During script execution, a **semi-transparent window** appears over the game showing:

- 📝 **7 lines** of context: 3 past + current + 3 upcoming
- 🎨 **Color highlighting** for different command types
- 📍 **Breadcrumbs** — execution path (e.g., `conscious > check_pulse`)
- ×N **Iteration counter** for loops

### Command Icons

| Icon | Command |
|:---|:---|
| 🔀 | `WAIT_BRANCH` — branch selection |
| ➜ | `GOTO` — jump to label |
| 🏷 | `LABEL` — label |
| ┌ | `BRANCH` — branch start |
| └ | `END_BRANCH` — branch end |
| ⏸ | Waiting for input |
| ▶ | Current line |
| • | Future lines |

### Color Scheme

| Type | Current Line | Future Lines |
|:---|:---|:---|
| WAIT_BRANCH | 🟠 Orange | Dimmed |
| GOTO | 🟢 Green | Dimmed |
| LABEL | 🟣 Purple | Dimmed |
| BRANCH | 🔵 Blue | Dimmed |
| Plain text | 🔷 Bright cyan | Light gray |
| Past lines | ⬛ Dark gray | — |

> 🖱 The overlay window can be **dragged** to any position on the screen.

---

## ⚙️ Configuration

The `config.txt` file in the project root:

```
# Comments start with #
# Format: Modifiers+Key=script_file.txt

# Simple bindings
Ctrl+1=medcard.txt
Ctrl+2=inspection.txt

# NumPad
NumPad5=quick_action.txt
Ctrl+NumPad0=reset.txt

# Modifier combinations
Ctrl+Shift+R=special_script.txt
Alt+Q=quick_action.txt
```

### Supported Modifiers

- `Ctrl`, `Shift`, `Alt`
- Digits `0`–`9` (automatically converted to `D0`–`D9`)
- NumPad keys (see [NumPad Keys](#-numpad-keys))

---

## 📂 Script Examples

### Example 1: Simple Script

```
{T}[0.1]/say Let me check your blood pressure.{Enter}
[0.05]{T}[0.1]/me takes out a tonometer{Enter}
[1]{T}[0.1]/do The tonometer reads 120/80.{Enter}
```

### Example 2: Fast Clipboard Input

```
!
{T}[0.1]This text will be pasted instantly via Ctrl+V{Enter}
```

### Example 3: Medical Examination with Branching

Full example with conditional jumps, loops, and multiple choice:

```
!
{F8}[0.1]say Starting medical examination of the victim.{Enter}
[0.5]me approached the victim{Enter}

{LABEL=check_consciousness}
[0.5]do Is the victim conscious?{Enter}
[0.1]b /do Yes. or /do No.{Enter}
{WAIT_BRANCH=RSHIFT:conscious;RCTRL:unconscious}

{BRANCH=conscious}
say The victim is conscious{Enter}
me asks the victim questions{Enter}
{GOTO=check_pulse}
{END_BRANCH}

{BRANCH=unconscious}
say The victim is unconscious{Enter}
me checks breathing{Enter}
[0.5]do Is there breathing?{Enter}
[0.1]b /do Yes. or /do No.{Enter}
{GOTO=cpr;NO=RCTRL;YES=RSHIFT}
{END_BRANCH}

{LABEL=cpr}
say No breathing! Starting CPR!{Enter}
{LABEL=cpr_cycle}
me started chest compressions{Enter}
[2]me continues chest compressions{Enter}
[2]me performs artificial respiration{Enter}
[0.5]do Pulse restored?{Enter}
[0.1]b /do Yes (RShift) or /do Repeat (RCtrl){Enter}
{GOTO=cpr_cycle;REPEAT=RCTRL;YES=RSHIFT}
say Pulse restored!{Enter}

{LABEL=check_pulse}
[0.5]me placed two fingers on the carotid artery{Enter}
[0.5]do Is there a pulse?{Enter}
[0.1]b /do Yes (RShift) or /do No (RCtrl){Enter}
{GOTO=hospitalization;YES=RSHIFT;NO=RCTRL}
say Pulse is weak, help needed{Enter}
[0.5]do Start CPR?{Enter}
[0.1]b /do Yes (RShift) or /do Cancel (Esc){Enter}
{GOTO=cpr;CONFIRM=RSHIFT;CANCEL=ESC}

{LABEL=hospitalization}
say Preparing victim for transport{Enter}
me called an ambulance{Enter}
[0.5]do Hospitalization urgency?{Enter}
[0.1]b /do High. / Medium. / Low.{Enter}
{GOTO=end;F1=emergency;F2=planned;F3=low}

{LABEL=emergency}
say Code red! Emergency hospitalization!{Enter}
{GOTO=end}

{LABEL=planned}
say Planned hospitalization{Enter}
{GOTO=end}

{LABEL=low}
say Outpatient treatment{Enter}

{LABEL=end}
say Procedure completed{Enter}
```

> 📄 Full branching script example: [`branch_test.txt`](branch_test.txt)

---

## 🔢 NumPad Keys

### Digits

| Key | Config Code |
|:---|:---|
| 0–9 | `NumPad0` … `NumPad9` |

### Operators

| Key | Config Code |
|:---|:---|
| + | `Add` |
| − | `Subtract` |
| × | `Multiply` |
| ÷ | `Divide` |
| . (dot) | `Decimal` |

### Examples

```
NumPad1=script1.txt
Ctrl+NumPad0=reset.txt
Shift+NumPad9=quick_action.txt
Alt+Add=increase.txt
Ctrl+Subtract=decrease.txt
Ctrl+Shift+NumPad5=special_action.txt
```

---

## 🔥 Hotkeys

| Action | Combination |
|:---|:---|
| **Cancel script** | `Esc` × 3 per second |
| **Emergency close** | `Win` × 3 per second |
| **Continue after pause** | `Right Shift` |
| **Branch selection** | `RShift` / `RCtrl` / `F1`–`F12` / `Esc` |
| **Drag overlay** | Left mouse button + drag |

---

## 📁 Project Structure

```
KeyRemapper/
├── 📄 KeyRemapper.cs            # Main class, keyboard hook
├── 📄 config.txt                # Bindings configuration
├── 📄 compile.bat               # Build script
├── 📄 LICENSE                   # MIT License
│
├── 📁 Models/
│   └── KeyBinding.cs            # Binding data model
│
├── 📁 Core/
│   ├── ConfigParser.cs          # config.txt parser
│   ├── InputSimulator.cs        # Input emulation (Win32 API)
│   ├── GtaMonitor.cs            # GTA SA activity monitor
│   ├── BranchingEngine.cs       # Labels, branches, GOTO processing
│   ├── Overlay.cs               # Semi-transparent visualization window
│   ├── ScriptEngine.cs          # Main execution engine
│   └── InstructionExecutor.cs   # Backward compatibility adapter
│
├── 📁 UI/
│   ├── MainWindow.cs            # Application main window
│   └── StyledControls.cs        # Custom UI components
│
└── 📁 Script examples
    ├── branch_test.txt          # Medical exam with branching
    └── NUMPAD_KEYS.txt          # NumPad keys reference
```

---

## 🔧 Building

### Requirements

- **Windows** (7/8/10/11)
- **.NET Framework 4.0** or higher (usually pre-installed)
- No additional dependencies needed!

### Build

```bash
compile.bat
```

Uses the built-in .NET Framework compiler:
```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

### Run

```bash
KeyRemapper.exe
```

### Stop

```bash
taskkill /F /IM KeyRemapper.exe
```

Or press `Win` 3 times per second.

---

## ⚠️ Known Limitations

1. **Clipboard mode** does not work in some game text chats — use character-by-character mode
2. **English characters** are typed slower than Russian (via SendKeys)
3. **Layout switching** adds ~100ms delay
4. Works **only on Windows** (uses Win32 API hooks)
5. Requires **.NET Framework 4.0+**

---

## 📜 License

This project is distributed under the [MIT](LICENSE) license.

---

<div align="center">

**[v0.0.1] - Built with AI** 🤖

</div>