using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

/// <summary>
/// Overlay окно для отображения текущей строки скрипта
/// Полупрозрачное окно с визуализацией выполнения макросов
/// </summary>
public class OverlayForm : Form
{
    private Panel contentPanel;
    private bool isDragging = false;
    private Point dragStartPoint;
    private System.Windows.Forms.Timer fadeTimer;
    private bool isFadingIn = false;
    private bool isFadingOut = false;
    private const double FADE_STEP = 0.05;
    private const double TARGET_OPACITY = 0.95;
    private const string POSITION_FILE = "overlay_position.txt";
    
    // Список меток для строк
    private List<Label> lineLabels = new List<Label>();
    private const int MAX_LINES = 7; // 3 прошлые + 1 текущая + 3 следующие
    private Label breadcrumbLabel; // Метка для отображения пути (breadcrumbs)
    
    public OverlayForm()
    {
        // Настройки формы - Modern Glass стиль
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(20, 20, 30); // Темно-синий фон
        this.Opacity = 0.01;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.Size = new Size(500, 260); // Увеличенный размер для большего количества строк
        
        // Загружаем сохраненную позицию или используем позицию по умолчанию
        Point savedPosition = LoadPosition();
        if (savedPosition.X == -1 && savedPosition.Y == -1)
        {
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - 20, 20);
        }
        else
        {
            this.Location = savedPosition;
        }
        
        // Включаем двойную буферизацию для плавности
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                      ControlStyles.AllPaintingInWmPaint | 
                      ControlStyles.UserPaint, true);
        
        // Панель контента с отступами
        contentPanel = new Panel();
        contentPanel.BackColor = Color.Transparent;
        contentPanel.Location = new Point(15, 15);
        contentPanel.Size = new Size(this.Width - 30, this.Height - 30);
        
        // Создаем метку для breadcrumbs (путь выполнения)
        breadcrumbLabel = new Label();
        breadcrumbLabel.AutoSize = false;
        breadcrumbLabel.Width = contentPanel.Width;
        breadcrumbLabel.Height = 25;
        breadcrumbLabel.Location = new Point(0, 0);
        breadcrumbLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        breadcrumbLabel.TextAlign = ContentAlignment.MiddleLeft;
        breadcrumbLabel.BackColor = Color.Transparent;
        breadcrumbLabel.ForeColor = Color.FromArgb(120, 180, 220);
        breadcrumbLabel.Text = "";
        breadcrumbLabel.MouseDown += OnMouseDown;
        breadcrumbLabel.MouseMove += OnMouseMove;
        breadcrumbLabel.MouseUp += OnMouseUp;
        contentPanel.Controls.Add(breadcrumbLabel);
        
        // Создаем метки для строк (сдвигаем вниз для breadcrumbs)
        for (int i = 0; i < MAX_LINES; i++)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.Width = contentPanel.Width;
            label.Height = 26; // Уменьшенная высота для компактности
            label.Location = new Point(0, 28 + i * 28); // Уменьшенный отступ между строками
            label.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.BackColor = Color.Transparent;
            label.ForeColor = Color.Gray;
            label.Text = "";
            
            // События для перетаскивания
            label.MouseDown += OnMouseDown;
            label.MouseMove += OnMouseMove;
            label.MouseUp += OnMouseUp;
            
            lineLabels.Add(label);
            contentPanel.Controls.Add(label);
        }
        
        // События для перетаскивания самой панели
        contentPanel.MouseDown += OnMouseDown;
        contentPanel.MouseMove += OnMouseMove;
        contentPanel.MouseUp += OnMouseUp;
        
        this.Controls.Add(contentPanel);
        
        // Таймер для fade анимации
        fadeTimer = new System.Windows.Forms.Timer();
        fadeTimer.Interval = 20; // 50 FPS
        fadeTimer.Tick += FadeTimer_Tick;
        
        this.Paint += OverlayForm_Paint;
        this.Visible = false;
    }
    
    // Рисуем красивую рамку с градиентом и тенью
    private void OverlayForm_Paint(object sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        // Создаем скругленный прямоугольник
        System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRect(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 12);
        
        // Градиентный фон
        using (System.Drawing.Drawing2D.LinearGradientBrush brush = 
            new System.Drawing.Drawing2D.LinearGradientBrush(
                new Point(0, 0), 
                new Point(0, this.Height),
                Color.FromArgb(220, 25, 25, 40),  // Темно-синий с прозрачностью
                Color.FromArgb(220, 15, 15, 25))) // Еще темнее внизу
        {
            g.FillPath(brush, path);
        }
        
        // Светящаяся рамка (градиент от cyan к blue)
        using (System.Drawing.Drawing2D.LinearGradientBrush borderBrush = 
            new System.Drawing.Drawing2D.LinearGradientBrush(
                new Point(0, 0),
                new Point(this.Width, this.Height),
                Color.FromArgb(180, 100, 200, 255), // Cyan
                Color.FromArgb(180, 150, 100, 255))) // Blue-purple
        {
            using (Pen pen = new Pen(borderBrush, 2))
            {
                g.DrawPath(pen, path);
            }
        }
        
        // Внутренняя подсветка (тонкая светлая линия)
        System.Drawing.Drawing2D.GraphicsPath innerPath = GetRoundedRect(new Rectangle(2, 2, this.Width - 5, this.Height - 5), 10);
        using (Pen innerPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
        {
            g.DrawPath(innerPen, innerPath);
        }
    }
    
    // Создает скругленный прямоугольник
    private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
    {
        System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;
        
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        
        return path;
    }
    
    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDragging = true;
            dragStartPoint = e.Location;
            Control control = sender as Control;
            if (control != null)
            {
                // Корректируем точку относительно формы
                dragStartPoint = new Point(
                    dragStartPoint.X + control.Left,
                    dragStartPoint.Y + control.Top
                );
            }
        }
    }
    
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            Point currentPoint = e.Location;
            Control control = sender as Control;
            if (control != null)
            {
                currentPoint = new Point(
                    currentPoint.X + control.Left,
                    currentPoint.Y + control.Top
                );
            }
            
            Point newLocation = this.Location;
            newLocation.X += currentPoint.X - dragStartPoint.X;
            newLocation.Y += currentPoint.Y - dragStartPoint.Y;
            this.Location = newLocation;
        }
    }
    
    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        isDragging = false;
        // Сохраняем позицию после перемещения
        SavePosition();
    }
    
    // Сохраняет текущую позицию окна в файл
    private void SavePosition()
    {
        try
        {
            File.WriteAllText(POSITION_FILE, this.Location.X + "," + this.Location.Y);
        }
        catch
        {
            // Игнорируем ошибки сохранения
        }
    }
    
    // Загружает сохраненную позицию из файла
    private Point LoadPosition()
    {
        try
        {
            if (File.Exists(POSITION_FILE))
            {
                string[] parts = File.ReadAllText(POSITION_FILE).Split(',');
                if (parts.Length == 2)
                {
                    int x = int.Parse(parts[0]);
                    int y = int.Parse(parts[1]);
                    return new Point(x, y);
                }
            }
        }
        catch
        {
            // Игнорируем ошибки загрузки
        }
        return new Point(-1, -1); // Флаг что позиция не найдена
    } // Таймер для плавной анимации
    private void FadeTimer_Tick(object sender, EventArgs e)
    {
        if (isFadingIn)
        {
            if (this.Opacity < TARGET_OPACITY)
            {
                this.Opacity = Math.Min(TARGET_OPACITY, this.Opacity + FADE_STEP);
            }
            else
            {
                isFadingIn = false;
                fadeTimer.Stop();
            }
        }
        else if (isFadingOut)
        {
            if (this.Opacity > 0)
            {
                this.Opacity = Math.Max(0, this.Opacity - FADE_STEP);
            }
            else
            {
                isFadingOut = false;
                fadeTimer.Stop();
                this.Visible = false;
            }
        }
    }
    
    // Определяет тип команды в строке
    private string GetCommandType(string line)
    {
        string trimmed = line.Trim();
        
        if (trimmed.Contains("{WAIT_BRANCH=")) return "WAIT_BRANCH";
        if (trimmed.Contains("{GOTO=")) return "GOTO";
        if (trimmed.Contains("{LABEL=")) return "LABEL";
        if (trimmed.Contains("{BRANCH=")) return "BRANCH";
        if (trimmed.Contains("{END_BRANCH}")) return "END_BRANCH";
        if (trimmed.Contains("{WAIT}")) return "WAIT";
        
        return "TEXT";
    }
    
    // Получает иконку для типа команды
    private string GetCommandIcon(string commandType, bool isCurrent)
    {
        if (isCurrent)
        {
            switch (commandType)
            {
                case "WAIT_BRANCH": return "\U0001f500"; case "GOTO": return "\u279c";
                case "LABEL": return "\U0001f3f7";
                case "BRANCH": return "\u250c";
                case "END_BRANCH": return "\u2514";
                case "WAIT": return "\u23f8";
                default: return "\u25b6";
            }
        }
        else
        {
            switch (commandType)
            {
                case "WAIT_BRANCH": return "\U0001f500";
                case "GOTO": return "\u279c";
                case "LABEL": return "\U0001f3f7";
                case "BRANCH": return "\u250c";
                case "END_BRANCH": return "\u2514";
                case "WAIT": return "\u23f8";
                default: return "\u2022";
            }
        }
    }
    
    // Получает цвет для типа команды
    private Color GetCommandColor(string commandType, bool isCurrent, bool isPast)
    {
        if (isPast)
        {
            return Color.FromArgb(100, 100, 100); // Темно-серый для прошлых
        }
        
        if (isCurrent)
        {
            switch (commandType)
            {
                case "WAIT_BRANCH": return Color.FromArgb(255, 200, 100); // Оранжевый
                case "GOTO": return Color.FromArgb(150, 255, 150); // Зеленый
                case "LABEL": return Color.FromArgb(200, 150, 255); // Фиолетовый
                case "BRANCH": return Color.FromArgb(100, 200, 255); // Голубой
                case "END_BRANCH": return Color.FromArgb(100, 200, 255); // Голубой
                case "WAIT": return Color.FromArgb(255, 150, 150); // Красноватый
                default: return Color.FromArgb(100, 200, 255); // Яркий cyan
            }
        }
        else
        {
            // Будущие строки - приглушенные цвета
            switch (commandType)
            {
                case "WAIT_BRANCH": return Color.FromArgb(180, 140, 80);
                case "GOTO": return Color.FromArgb(120, 180, 120);
                case "LABEL": return Color.FromArgb(150, 120, 180);
                case "BRANCH": return Color.FromArgb(80, 150, 180);
                case "END_BRANCH": return Color.FromArgb(80, 150, 180);
                case "WAIT": return Color.FromArgb(180, 120, 120);
                default: return Color.FromArgb(150, 150, 150); // Светло-серый
            }
        }
    }
    
    // Форматирует строку для отображения (без сокращений для WAIT_BRANCH)
    private string FormatLineForDisplay(string line, string commandType)
    {
        string trimmed = line.Trim();
        
        // Для WAIT_BRANCH показываем полный текст без сокращений
        if (commandType == "WAIT_BRANCH")
        {
            return trimmed; // Возвращаем полную строку
        }
        
        // Для остальных команд обрезаем если слишком длинные
        if (trimmed.Length > 60)
        {
            return trimmed.Substring(0, 57) + "...";
        }
        
        return trimmed;
    }
    
    // Предварительная инициализация handle формы
    // Должна вызываться из UI-потока до первого запуска скрипта
    public void WarmUpHandle()
    {
        try
        {
            // Создаем handle явно
            IntPtr handle = this.Handle;
            this.CreateControl();
        }
        catch
        {
            // Игнорируем ошибки если форма уже создана или закрывается
        }
    }

    public void UpdateText(string[] lines, int currentIndex, string breadcrumbs)
    {
        if (this.InvokeRequired)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    this.BeginInvoke(new Action(() => UpdateText(lines, currentIndex, breadcrumbs)));
            }
            catch
            {
                // Игнорируем ошибки если форма закрыта
            }
            return;
        }
        
        // Обновляем breadcrumbs
        if (!string.IsNullOrEmpty(breadcrumbs))
        {
            breadcrumbLabel.Text = "\U0001f4cd " + breadcrumbs;
            breadcrumbLabel.Visible = true;
        }
        else
        {
            breadcrumbLabel.Text = "";
            breadcrumbLabel.Visible = false;
        }
        
        // Показываем 3 прошлые, текущую и 3 следующие строки
        int startIndex = Math.Max(0, currentIndex - 3);
        int labelIndex = 0;
        
        for (int i = startIndex; i < Math.Min(startIndex + MAX_LINES, lines.Length); i++)
        {
            if (labelIndex >= lineLabels.Count) break;
            
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                lineLabels[labelIndex].Text = "";
                labelIndex++;
                continue;
            }
            
            Label label = lineLabels[labelIndex];
            
            // Определяем тип команды
            string commandType = GetCommandType(line);
            bool isCurrent = (i == currentIndex);
            bool isPast = (i < currentIndex);
            
            // Форматируем строку для отображения
            string displayText = FormatLineForDisplay(line, commandType);
            
            // Для WAIT_BRANCH с длинным текстом используем перенос строк
            if (commandType == "WAIT_BRANCH" && displayText.Length > 60)
            {
                label.AutoSize = false;
                label.Height = 50; // Увеличиваем высоту для переноса
                
                // Включаем перенос текста
                label.Text = GetCommandIcon(commandType, isCurrent) + " " + displayText;
                label.TextAlign = ContentAlignment.TopLeft;
            }
            else
            {
                label.Height = 26;
                label.Text = GetCommandIcon(commandType, isCurrent) + " " + displayText;
                label.TextAlign = ContentAlignment.MiddleLeft;
            }
            
            // Устанавливаем цвет
            label.ForeColor = GetCommandColor(commandType, isCurrent, isPast);
            
            // Устанавливаем шрифт
            if (isCurrent)
            {
                label.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            }
            else if (isPast)
            {
                label.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            }
            else
            {
                label.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            }
            
            labelIndex++;
        }
        
        // Очищаем неиспользованные метки
        for (int i = labelIndex; i < lineLabels.Count; i++)
        {
            lineLabels[i].Text = "";
        }
        
        // Плавное появление (или показываем сразу если это первый запуск)
        if (!this.Visible || this.Opacity < 0.1)
        {
            this.Visible = true;
            isFadingIn = true;
            isFadingOut = false;
            fadeTimer.Start();
        }
    }
    
    // Показывает пустое окно для возможности перемещения
    public void ShowEmpty()
    {
        if (this.InvokeRequired)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    this.BeginInvoke(new Action(ShowEmpty));
            }
            catch
            {
                // Игнорируем ошибки если форма закрыта
            }
            return;
        }
        
        // Очищаем все метки
        for (int i = 0; i < lineLabels.Count; i++)
        {
            lineLabels[i].Text = "";
        }
        
        // Показываем подсказку в первой метке
        lineLabels[0].Text = "Ожидание скрипта...";
        lineLabels[0].ForeColor = Color.FromArgb(150, 150, 150);
        lineLabels[0].Font = new Font("Segoe UI", 9f, FontStyle.Italic);
        
        // Плавное появление
        this.Visible = true;
        isFadingIn = true;
        isFadingOut = false;
        fadeTimer.Start();
    }
    
    public void ShowWithoutAnimation()
    {
        if (this.InvokeRequired)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    this.BeginInvoke(new Action(ShowWithoutAnimation));
            }
            catch
            {
                // Игнорируем ошибки если форма закрыта
            }
            return;
        }

        try
        {
            // Убеждаемся что handle создан
            if (!this.IsHandleCreated)
            {
                IntPtr handle = this.Handle;
            }

            // Останавливаем любые анимации
            if (fadeTimer != null && fadeTimer.Enabled)
                fadeTimer.Stop();

            isFadingIn = false;
            isFadingOut = false;

            // Устанавливаем полную прозрачность сразу
            this.Opacity = TARGET_OPACITY;
            this.Visible = true;
        }
        catch
        {
            // Игнорируем ошибки если форма еще не создана или закрывается
        }
    }

    public void HideText()
    {
        if (this.InvokeRequired)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    this.BeginInvoke(new Action(HideText));
            }
            catch
            {
                // Игнорируем ошибки если форма закрыта
            }
            return;
        }
        
        // Плавное исчезновение
        isFadingOut = true;
        isFadingIn = false;
        fadeTimer.Start();
    }
}
