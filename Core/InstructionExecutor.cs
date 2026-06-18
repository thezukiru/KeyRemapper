using System;
using System.Windows.Forms;


    // Адаптер для обратной совместимости - использует новый ScriptEngine
    public static class InstructionExecutor
    {
        private static ScriptEngine scriptEngine = new ScriptEngine();
        private static OverlayForm overlayForm;
        
        // Флаг автоматического режима overlay
        // true = overlay показывается/скрывается автоматически при выполнении скрипта
        // false = overlay управляется вручную через ToggleOverlay (для фиксации позиции)
        private static bool overlayAutoMode = true;
        
        public static void RequestCancel()
        {
            scriptEngine.RequestCancel();
            // Немедленно скрываем overlay при отмене (в авто режиме)
            HideOverlayForScript();
        }
        
        public static void CancelWaiting()
        {
            scriptEngine.CancelWaiting();
            // Немедленно скрываем overlay при отмене на паузе
            HideOverlayForScript();
        }
        
        public static bool IsWaiting()
        {
            return scriptEngine.IsWaiting();
        }
        
        public static bool IsExecuting()
        {
            return scriptEngine.IsExecuting();
        }
        
        public static void InitializeOverlay()
        {
            if (overlayForm != null && !overlayForm.IsDisposed)
                return;

            // Создаем экземпляр OverlayForm и передаем его в ScriptEngine
            overlayForm = new OverlayForm();

            // Важно: handle должен быть создан заранее на UI-потоке,
            // до первого запуска скрипта.
            overlayForm.WarmUpHandle();

            scriptEngine.SetOverlayForm(overlayForm);
            Console.WriteLine("Overlay инициализирован");
        }
        
        public static OverlayForm GetOverlayForm()
        {
            return overlayForm;
        }
        
        public static bool IsOverlayVisible()
        {
            return overlayForm != null && overlayForm.Visible;
        }
        
        // Переключает режим работы overlay (авто/ручной)
        // При переключении в ручной режим - показывает пустой overlay для позиционирования
        // При переключении в авто режим - скрывает overlay если скрипт не выполняется
        public static void ToggleOverlay()
        {
            overlayAutoMode = !overlayAutoMode;
            
            if (overlayForm == null) return;
            
            if (!overlayAutoMode)
            {
                // Ручной режим - показываем overlay для позиционирования
                overlayForm.ShowEmpty(); Console.WriteLine("Overlay: ручной режим (для фиксации позиции)");
            }
            else
            {
                // Авто режим - скрываем overlay если скрипт не выполняется
                if (!scriptEngine.IsExecuting())
                {
                    overlayForm.HideText();
                }
                Console.WriteLine("Overlay: автоматический режим");
            }
        }
        
        // Возвращает true если overlay в автоматическом режиме
        public static bool IsOverlayAutoMode()
        {
            return overlayAutoMode;
        }
        
        // Показывает overlay (вызывается из ScriptEngine при начале выполнения скрипта)
        public static void ShowOverlayForScript()
        {
            if (overlayForm == null) return;
            
            // Показываем overlay если:
            // 1. Автоматический режим (по умолчанию)
            // 2. Или ручной режим и overlay уже виден (пользователь зафиксировал позицию)
            if (overlayAutoMode || overlayForm.Visible)
            {
                // Просто делаем форму видимой БЕЗ запуска fade-анимации
                // Реальный контент будет установлен через UpdateText()
                overlayForm.ShowWithoutAnimation();
            }
        }
        
        // Скрывает overlay (вызывается из ScriptEngine при завершении скрипта)
        public static void HideOverlayForScript()
        {
            if (overlayForm == null) return;
            
            // Скрываем overlay только в автоматическом режиме
            // В ручном режиме overlay остаётся видимым для позиционирования
            if (overlayAutoMode)
            {
                overlayForm.HideText();
            }
        }
        
        public static bool CanContinue(string filePath)
        {
            return scriptEngine.CanContinue(filePath);
        }
        
        public static void ContinueExecution()
        {
            scriptEngine.ContinueExecution();
        }
        
        public static void ExecuteInstructions(string filePath)
        {
            scriptEngine.ExecuteInstructions(filePath);
        }
    }