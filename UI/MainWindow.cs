using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

/// <summary>
/// Главное окно программы KeyRemapper v0.3
/// Современный дизайн в стиле Province Helper Lite с темной темой.
/// </summary>
class MainWindow : Form
{
    // ========================================================================
    // КОНТРОЛЫ ВЕРХНЕЙ ПАНЕЛИ
    // ========================================================================
    private Panel headerPanel;
    private Label titleLabel;
    private Label versionLabel;
    private Button minimizeButton;
    private Button closeButton;

    // ========================================================================
    // КОНТРОЛЫ ОСНОВНОЙ ОБЛАСТИ - БЛОК ТЕГОВ РАЦИИ
    // ========================================================================
    
    // !!! ВАЖНО: Теги рации пока никуда не сохраняются и не используются !!!
    // !!! Это заглушка для будущей функциональности !!!
    // !!! Когда будет нужно - добавить сохранение в config.txt или отдельный файл !!!
    
    private RoundedPanel radioTagsPanel;
    private Label radioTagsTitle;
    private StyledTextBox tagRTextBox;      // Тег /r (основная рация)
    private StyledTextBox tagRoTextBox;     // Тег /ro (официальная рация)
    private StyledTextBox tagDTextBox;      // Тег /d (диспетчерская)

    // ========================================================================
    // КОНТРОЛЫ СТАТУСА И БИНДИНГОВ
    // ========================================================================
    private RoundedPanel statusPanel;
    private StatusIndicator gtaProcessIndicator;   // Индикатор наличия процесса gta_sa.exe
    private StatusIndicator gtaActiveIndicator;    // Индикатор активности окна GTA SA
    private ListBox bindingsList;
    private Label bindingsTitle;
    private RoundedButton addBindingButton;

    // ========================================================================
    // КОНТРОЛЫ НИЖНЕЙ ПАНЕЛИ
    // ========================================================================
    private Panel footerPanel;
    private RoundedButton reloadButton;
    private RoundedButton showOverlayButton;
    private RoundedButton exitButton;
    
    // Таймер для обновления состояния кнопки overlay
    private Timer overlayStateTimer;

    // ========================================================================
    // СИСТЕМНЫЙ ТРЕЙ
    // ========================================================================
    private ContextMenu trayMenu;
    private NotifyIcon trayIcon;

    // ========================================================================
    // ТАЙМЕР ОБНОВЛЕНИЯ СТАТУСА GTA
    // ========================================================================
    private Timer statusTimer;

    private const string CONFIG_FILE = "config.txt";

    // ========================================================================
    // ЦВЕТОВАЯ СХЕМА (мягкие цвета в стиле Province Helper)
    // ========================================================================
    private static readonly Color BG_COLOR = Color.FromArgb(35, 35, 45);
    private static readonly Color PANEL_BG = Color.FromArgb(42, 42, 55);
    private static readonly Color HEADER_BG = Color.FromArgb(30, 30, 40);
    private static readonly Color TEXT_PRIMARY = Color.FromArgb(220, 220, 230);
    private static readonly Color TEXT_SECONDARY = Color.FromArgb(150, 160, 180);
    private static readonly Color ACCENT_ORANGE = Color.FromArgb(255, 140, 50);
    private static readonly Color ACCENT_GREEN = Color.FromArgb(80, 200, 120);
    private static readonly Color ACCENT_RED = Color.FromArgb(220, 80, 80);
    private static readonly Color BORDER_COLOR = Color.FromArgb(60, 60, 75);
    
    // Цвета для кнопки overlay
    private static readonly Color OVERLAY_BUTTON_HIDDEN = Color.FromArgb(70, 70, 80);
    private static readonly Color OVERLAY_BUTTON_HIDDEN_HOVER = Color.FromArgb(90, 90, 100);
    private static readonly Color OVERLAY_BUTTON_VISIBLE = Color.FromArgb(55, 90, 70);
    private static readonly Color OVERLAY_BUTTON_VISIBLE_HOVER = Color.FromArgb(65, 110, 85);

    public MainWindow()
    {
        InitializeWindow();
        InitializeHeader();
        InitializeRadioTagsPanel();
        InitializeStatusPanel();
        InitializeFooter();
        InitializeTray();
        InitializeStatusTimer();
        InitializeOverlayStateTimer();
        
        LoadBindings();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // Overlay должен быть создан на UI-потоке до первого запуска скрипта
        InstructionExecutor.InitializeOverlay();
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ ОКНА
    // ========================================================================
    private void InitializeWindow()
    {
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ClientSize = new Size(540, 520);
        this.BackColor = BG_COLOR;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "KeyRemapper v0.3";
        
        Screen screen = Screen.PrimaryScreen;
        this.Location = new Point(
            (screen.WorkingArea.Width - this.Width) / 2,
            (screen.WorkingArea.Height - this.Height) / 2
        );
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ ВЕРХНЕЙ ПАНЕЛИ (Header)
    // ========================================================================
    private void InitializeHeader()
    {
        headerPanel = new Panel();
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 45;
        headerPanel.BackColor = HEADER_BG;
        headerPanel.Paint += HeaderPanel_Paint;

        titleLabel = new Label();
        titleLabel.Text = "\u2328 KeyRemapper";
        titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        titleLabel.ForeColor = TEXT_PRIMARY;
        titleLabel.Location = new Point(15, 12);
        titleLabel.AutoSize = true;
        headerPanel.Controls.Add(titleLabel);

        versionLabel = new Label();
        versionLabel.Text = "v0.3";
        versionLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        versionLabel.ForeColor = TEXT_SECONDARY;
        versionLabel.Location = new Point(titleLabel.Right + 8, 14);
        versionLabel.AutoSize = true;
        headerPanel.Controls.Add(versionLabel);

        minimizeButton = CreateWindowControlButton("\u2014", Color.FromArgb(80, 80, 95));
        minimizeButton.Click += new EventHandler(MinimizeButton_Click);
        headerPanel.Controls.Add(minimizeButton);

        closeButton = CreateWindowControlButton("\u2715", Color.FromArgb(180, 60, 60));
        closeButton.Click += new EventHandler(ExitApplication);
        headerPanel.Controls.Add(closeButton);

        headerPanel.Resize += new EventHandler(HeaderPanel_Resize); this.Controls.Add(headerPanel);
    }

    private void HeaderPanel_Paint(object sender, PaintEventArgs e)
    {
        using (Pen pen = new Pen(BORDER_COLOR, 1))
        {
            e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        }
    }

    private void HeaderPanel_Resize(object sender, EventArgs e)
    {
        UpdateWindowButtonPositions();
    }

    private void MinimizeButton_Click(object sender, EventArgs e)
    {
        this.WindowState = FormWindowState.Minimized;
    }

    private void UpdateWindowButtonPositions()
    {
        minimizeButton.Location = new Point(headerPanel.Width - 75, 8);
        closeButton.Location = new Point(headerPanel.Width - 45, 8);
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ ПАНЕЛИ ТЕГОВ РАЦИИ
    // ========================================================================
    private void InitializeRadioTagsPanel()
    {
        int panelX = 15;
        int panelY = 55;
        int panelWidth = 510;
        int panelHeight = 155;
        
        radioTagsPanel = new RoundedPanel();
        radioTagsPanel.CornerRadius = 12;
        radioTagsPanel.BackColor = PANEL_BG;
        radioTagsPanel.BorderColor = BORDER_COLOR;
        radioTagsPanel.ShowBorder = true;
        radioTagsPanel.Location = new Point(panelX, panelY);
        radioTagsPanel.Size = new Size(panelWidth, panelHeight);

        // Заголовок панели
        radioTagsTitle = new Label();
        radioTagsTitle.Text = "\uD83D\uDCFb Теги рации";
        radioTagsTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        radioTagsTitle.ForeColor = ACCENT_ORANGE;
        radioTagsTitle.Location = new Point(16, 10);
        radioTagsTitle.AutoSize = true;
        radioTagsPanel.Controls.Add(radioTagsTitle);

        // Подзаголовок
        Label subtitleLabel = new Label();
        subtitleLabel.Text = "Текст перед сообщением в чате";
        subtitleLabel.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
        subtitleLabel.ForeColor = TEXT_SECONDARY;
        subtitleLabel.Location = new Point(16, 32);
        subtitleLabel.AutoSize = true;
        radioTagsPanel.Controls.Add(subtitleLabel);

        // Отступы для полей ввода
        int contentStartX = 16;
        int contentStartY = 52;
        int labelHeight = 16;
        int fieldWidth = 230;
        int fieldHeight = 28;
        int fieldGapX = 14;
        int rowGap = 12;

        // Вычисляем позиции для двух колонок
        int col1X = contentStartX;
        int col2X = contentStartX + fieldWidth + fieldGapX;

        // === РЯД 1: /r и /ro ===
        int row1Y = contentStartY;
        
        // Лейбл /r
        Label labelR = new Label();
        labelR.Text = "/r Основная рация:";
        labelR.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        labelR.ForeColor = TEXT_SECONDARY;
        labelR.Location = new Point(col1X, row1Y);
        labelR.AutoSize = true;
        radioTagsPanel.Controls.Add(labelR);

        // Поле /r
        tagRTextBox = new StyledTextBox();
        tagRTextBox.Location = new Point(col1X, row1Y + labelHeight);
        tagRTextBox.Size = new Size(fieldWidth, fieldHeight);
        tagRTextBox.PlaceholderText = "[ОГИБДД-Н]";
        radioTagsPanel.Controls.Add(tagRTextBox);

        // Лейбл /ro
        Label labelRo = new Label();
        labelRo.Text = "/ro Офиц. рация:";
        labelRo.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        labelRo.ForeColor = TEXT_SECONDARY;
        labelRo.Location = new Point(col2X, row1Y);
        labelRo.AutoSize = true;
        radioTagsPanel.Controls.Add(labelRo);

        // Поле /ro
        tagRoTextBox = new StyledTextBox();
        tagRoTextBox.Location = new Point(col2X, row1Y + labelHeight);
        tagRoTextBox.Size = new Size(fieldWidth, fieldHeight);
        tagRoTextBox.PlaceholderText = "Рация";
        radioTagsPanel.Controls.Add(tagRoTextBox);

        // === РЯД 2: /d и кнопка Сохранить ===
        int row2Y = contentStartY + labelHeight + fieldHeight + rowGap;

        // Лейбл /d
        Label labelD = new Label();
        labelD.Text = "/d Диспетчер:";
        labelD.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        labelD.ForeColor = TEXT_SECONDARY;
        labelD.Location = new Point(col1X, row2Y);
        labelD.AutoSize = true;
        radioTagsPanel.Controls.Add(labelD);

        // Поле /d
        tagDTextBox = new StyledTextBox();
        tagDTextBox.Location = new Point(col1X, row2Y + labelHeight);
        tagDTextBox.Size = new Size(fieldWidth, fieldHeight);
        tagDTextBox.PlaceholderText = "Диспетчер";
        radioTagsPanel.Controls.Add(tagDTextBox);

        // Кнопка Сохранить (без лейбла, выравниваем по полю)
        RoundedButton saveTagsBtn = new RoundedButton();
        saveTagsBtn.Text = "\uD83D\uDCBE Сохранить";
        saveTagsBtn.NormalColor = Color.FromArgb(50, 120, 80);
        saveTagsBtn.HoverColor = Color.FromArgb(60, 140, 95);
        saveTagsBtn.ForeColor = Color.FromArgb(220, 255, 230);
        saveTagsBtn.Size = new Size(fieldWidth, fieldHeight);
        saveTagsBtn.Location = new Point(col2X, row2Y + labelHeight);
        saveTagsBtn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        saveTagsBtn.Click += new EventHandler(SaveTagsButton_Click);
        radioTagsPanel.Controls.Add(saveTagsBtn);

        this.Controls.Add(radioTagsPanel);
    }

    private void SaveTagsButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show(
            "Теги пока не сохраняются.\n\n" +
            "Это заглушка для будущей функциональности.\n" +
            "Когда будет реализовано - теги будут сохранены и использоваться в скриптах.",
            "KeyRemapper",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ ПАНЕЛИ СТАТУСА И БИНДИНГОВ
    // ========================================================================
    private void InitializeStatusPanel()
    {
        statusPanel = new RoundedPanel();
        statusPanel.CornerRadius = 12;
        statusPanel.BackColor = PANEL_BG;
        statusPanel.BorderColor = BORDER_COLOR;
        statusPanel.ShowBorder = true;
        statusPanel.Location = new Point(15, 220);
        statusPanel.Size = new Size(510, 220);

        // Верхняя строка: два индикатора GTA (процесс и активность окна)
        int indicatorWidth = 240;
        int indicatorHeight = 28;
        int indicatorGap = 8;
        
        // Индикатор наличия процесса gta_sa.exe
        gtaProcessIndicator = new StatusIndicator();
        gtaProcessIndicator.Size = new Size(indicatorWidth, indicatorHeight);
        gtaProcessIndicator.Location = new Point(18, 14);
        gtaProcessIndicator.IsActive = false;
        gtaProcessIndicator.StatusText = "Процесс: не найден";
        gtaProcessIndicator.ForeColor = ACCENT_RED;
        gtaProcessIndicator.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        statusPanel.Controls.Add(gtaProcessIndicator);

        // Индикатор активности окна GTA SA
        gtaActiveIndicator = new StatusIndicator();
        gtaActiveIndicator.Size = new Size(indicatorWidth, indicatorHeight);
        gtaActiveIndicator.Location = new Point(18 + indicatorWidth + indicatorGap, 14);
        gtaActiveIndicator.IsActive = false;
        gtaActiveIndicator.StatusText = "Окно: не активно";
        gtaActiveIndicator.ForeColor = ACCENT_RED;
        gtaActiveIndicator.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        statusPanel.Controls.Add(gtaActiveIndicator);

        addBindingButton = new RoundedButton();
        addBindingButton.Text = "+ Добавить";
        addBindingButton.NormalColor = Color.FromArgb(55, 70, 95);
        addBindingButton.HoverColor = Color.FromArgb(65, 85, 115);
        addBindingButton.Size = new Size(100, 28);
        addBindingButton.Location = new Point(392, 14);
        addBindingButton.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        addBindingButton.Click += new EventHandler(AddBindingButton_Click);
        statusPanel.Controls.Add(addBindingButton);

        bindingsTitle = new Label();
        bindingsTitle.Text = "\uD83D\uDCCB Загруженные биндинги";
        bindingsTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        bindingsTitle.ForeColor = TEXT_PRIMARY;
        bindingsTitle.Location = new Point(18, 52);
        bindingsTitle.AutoSize = true;
        statusPanel.Controls.Add(bindingsTitle);

        bindingsList = new ListBox();
        bindingsList.Location = new Point(18, 78);
        bindingsList.Size = new Size(474, 128);
        bindingsList.BorderStyle = BorderStyle.None;
        bindingsList.BackColor = Color.FromArgb(30, 30, 40);
        bindingsList.ForeColor = TEXT_PRIMARY;
        bindingsList.Font = new Font("Consolas", 9f, FontStyle.Regular);
        bindingsList.ItemHeight = 22;
        bindingsList.DrawMode = DrawMode.OwnerDrawFixed;
        bindingsList.DrawItem += new DrawItemEventHandler(BindingsList_DrawItem);
        statusPanel.Controls.Add(bindingsList);

        this.Controls.Add(statusPanel);
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ НИЖНЕЙ ПАНЕЛИ (Footer)
    // ========================================================================
    private void InitializeFooter()
    {
        footerPanel = new Panel();
        footerPanel.Dock = DockStyle.Bottom;
        footerPanel.Height = 55;
        footerPanel.BackColor = HEADER_BG;
        footerPanel.Paint += FooterPanel_Paint;

        reloadButton = new RoundedButton();
        reloadButton.Text = "\u27F3 Перезагрузить конфиг";
        reloadButton.NormalColor = Color.FromArgb(55, 70, 95);
        reloadButton.HoverColor = Color.FromArgb(65, 85, 115);
        reloadButton.Size = new Size(180, 36);
        reloadButton.Location = new Point(20, 10);
        reloadButton.Click += new EventHandler(ReloadButton_Click);
        footerPanel.Controls.Add(reloadButton);

        // Кнопка для показа overlay (серая по умолчанию)
        showOverlayButton = new RoundedButton();
        showOverlayButton.Text = "\ud83d\udc41 Overlay";
        showOverlayButton.NormalColor = OVERLAY_BUTTON_HIDDEN;
        showOverlayButton.HoverColor = OVERLAY_BUTTON_HIDDEN_HOVER;
        showOverlayButton.Size = new Size(110, 36);
        showOverlayButton.Location = new Point(210, 10);
        showOverlayButton.Click += new EventHandler(ShowOverlayButton_Click);
        footerPanel.Controls.Add(showOverlayButton);

        exitButton = new RoundedButton();
        exitButton.Text = "\u2715 Выход";
        exitButton.NormalColor = Color.FromArgb(100, 45, 45);
        exitButton.HoverColor = Color.FromArgb(120, 55, 55);
        exitButton.ForeColor = Color.FromArgb(240, 200, 200);
        exitButton.Size = new Size(120, 36);
        exitButton.Location = new Point(380, 10);
        exitButton.Click += new EventHandler(ExitApplication);
        footerPanel.Controls.Add(exitButton);

        this.Controls.Add(footerPanel);
    }

    private void ShowOverlayButton_Click(object sender, EventArgs e)
    {
        // Переключаем состояние overlay
        InstructionExecutor.ToggleOverlay();
        UpdateOverlayButtonColor();
    }
    
    // Обновляет цвет кнопки в зависимости от видимости overlay
    private void UpdateOverlayButtonColor()
    {
        if (InstructionExecutor.IsOverlayVisible())
        {
            // Зеленый когда overlay виден
            showOverlayButton.NormalColor = OVERLAY_BUTTON_VISIBLE;
            showOverlayButton.HoverColor = OVERLAY_BUTTON_VISIBLE_HOVER;
        }
        else
        {
            // Серый когда overlay скрыт
            showOverlayButton.NormalColor = OVERLAY_BUTTON_HIDDEN;
            showOverlayButton.HoverColor = OVERLAY_BUTTON_HIDDEN_HOVER;
        }
    }
    
    // Таймер для обновления состояния кнопки overlay
    private void InitializeOverlayStateTimer()
    {
        overlayStateTimer = new Timer();
        overlayStateTimer.Interval = 500; // Проверяем каждые 500мс
        overlayStateTimer.Tick += new EventHandler(OverlayStateTimer_Tick);
        overlayStateTimer.Start();
    }
    
    private void OverlayStateTimer_Tick(object sender, EventArgs e)
    {
        UpdateOverlayButtonColor();
    }

    private void FooterPanel_Paint(object sender, PaintEventArgs e)
    {
        using (Pen pen = new Pen(BORDER_COLOR, 1))
        {
            e.Graphics.DrawLine(pen, 0, 0, footerPanel.Width, 0);
        }
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ СИСТЕМНОГО ТРЕЯ
    // ========================================================================
    private void InitializeTray()
    {
        trayIcon = new NotifyIcon();
        trayIcon.Text = "KeyRemapper v0.3";
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.Visible = true;

        trayMenu = new ContextMenu();
        trayMenu.MenuItems.Add("Показать окно", TrayShow_Click);
        trayMenu.MenuItems.Add("-");
        trayMenu.MenuItems.Add("Перезагрузить конфиг", new EventHandler(TrayReload_Click));
        trayMenu.MenuItems.Add("-");
        trayMenu.MenuItems.Add("Выход", new EventHandler(ExitApplication));
        trayIcon.ContextMenu = trayMenu;

        trayIcon.DoubleClick += new EventHandler(TrayShow_Click);

        this.Resize += new EventHandler(MainWindow_Resize);
        this.FormClosing += new FormClosingEventHandler(MainWindow_FormClosing);
    }

    private void TrayShow_Click(object sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
    }

    private void TrayReload_Click(object sender, EventArgs e)
    {
        LoadBindings();
    }

    private void MainWindow_Resize(object sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized)
            this.Hide();
    }

    private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (overlayStateTimer != null)
        {
            overlayStateTimer.Stop();
            overlayStateTimer.Dispose();
        }
        
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
    }

    // ========================================================================
    // ИНИЦИАЛИЗАЦИЯ ТАЙМЕРА СТАТУСА
    // ========================================================================
    private void InitializeStatusTimer()
    {
        statusTimer = new Timer();
        statusTimer.Interval = 1000;
        statusTimer.Tick += new EventHandler(StatusTimer_Tick);
        statusTimer.Start();
    }

    private void StatusTimer_Tick(object sender, EventArgs e)
    {
        UpdateGtaStatus();
    }

    // ========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ========================================================================

    private Button CreateWindowControlButton(string text, Color bgColor)
    {
        Button btn = new Button();
        btn.Text = text;
        btn.Size = new Size(28, 28);
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = bgColor;
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
        
        btn.MouseEnter += new EventHandler(WindowBtn_MouseEnter);
        btn.MouseLeave += new EventHandler(WindowBtn_MouseLeave);        
        return btn;
    }

    private void WindowBtn_MouseEnter(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        if (btn != null)
            btn.BackColor = ControlPaint.Light(btn.BackColor);
    }

    private void WindowBtn_MouseLeave(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        if (btn != null)
            btn.BackColor = Color.FromArgb(80, 80, 95);
    }

    private Label CreateFieldLabel(string text)
    {
        Label label = new Label();
        label.Text = text;
        label.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        label.ForeColor = TEXT_SECONDARY;
        label.AutoSize = true;
        return label;
    }

    private void BindingsList_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        e.DrawBackground();

        string item = bindingsList.Items[e.Index].ToString();
        int arrowPos = item.IndexOf("\u2192");

        if (arrowPos > 0)
        {
            string keyPart = item.Substring(0, arrowPos);
            string filePart = item.Substring(arrowPos);

            e.Graphics.DrawString(keyPart, e.Font, 
                new SolidBrush(Color.FromArgb(100, 200, 255)), 
                e.Bounds.Left + 5, e.Bounds.Top + 4);
            e.Graphics.DrawString(filePart, e.Font, 
                new SolidBrush(Color.FromArgb(150, 150, 165)), 
                e.Bounds.Left + 5 + (int)e.Graphics.MeasureString(keyPart, e.Font).Width, 
                e.Bounds.Top + 4);
        }
        else
        {
            e.Graphics.DrawString(item, e.Font, 
                new SolidBrush(TEXT_PRIMARY), 
                e.Bounds.Left + 5, e.Bounds.Top + 4);
        }

        if ((e.State & DrawItemState.Selected) != 0)
        {
            using (SolidBrush highlightBrush = new SolidBrush(Color.FromArgb(50, 70, 100)))
            {
                e.Graphics.FillRectangle(highlightBrush, e.Bounds);
            }
            
            if (arrowPos > 0)
            {
                string keyPart = item.Substring(0, arrowPos);
                string filePart = item.Substring(arrowPos);
                
                e.Graphics.DrawString(keyPart, e.Font, 
                    new SolidBrush(Color.FromArgb(130, 220, 255)), 
                    e.Bounds.Left + 5, e.Bounds.Top + 4);
                e.Graphics.DrawString(filePart, e.Font, 
                    new SolidBrush(Color.FromArgb(180, 180, 195)), e.Bounds.Left + 5 + (int)e.Graphics.MeasureString(keyPart, e.Font).Width, 
                    e.Bounds.Top + 4);
            }
        }
    }

    private void UpdateGtaStatus()
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(UpdateGtaStatus));
            return;
        }

        // Проверяем наличие процесса gta_sa.exe
        bool isGtaRunning = GtaMonitor.IsGtaSaRunning();
        
        // Проверяем активность окна GTA SA
        bool isGtaActive = GtaMonitor.IsGtaSaActive();
        
        // Обновляем индикатор наличия процесса
        if (isGtaRunning)
        {
            gtaProcessIndicator.IsActive = true;
            gtaProcessIndicator.StatusText = "Процесс: запущен";
            gtaProcessIndicator.ForeColor = ACCENT_GREEN;
        }
        else
        {
            gtaProcessIndicator.IsActive = false;
            gtaProcessIndicator.StatusText = "Процесс: не найден";
            gtaProcessIndicator.ForeColor = ACCENT_RED;
        }
        
        // Обновляем индикатор активности окна
        if (isGtaActive)
        {
            gtaActiveIndicator.IsActive = true;
            gtaActiveIndicator.StatusText = "Окно: активно";
            gtaActiveIndicator.ForeColor = ACCENT_GREEN;
        }
        else
        {
            gtaActiveIndicator.IsActive = false;
            gtaActiveIndicator.StatusText = "Окно: не активно";
            gtaActiveIndicator.ForeColor = isGtaRunning ? ACCENT_ORANGE : ACCENT_RED;
        }
    }

    private void LoadBindings()
    {
        bindingsList.Items.Clear();

        if (!File.Exists(CONFIG_FILE))
        {
            bindingsList.Items.Add("\u26A0 config.txt не найден");
            return;
        }

        List<KeyBinding> bindings = ConfigParser.LoadConfig(CONFIG_FILE);

        if (bindings.Count == 0)
        {
            bindingsList.Items.Add("Нет биндингов в конфиге");
            return;
        }

        foreach (var b in bindings)
        {
            string combo = "";
            if (b.Ctrl) combo += "Ctrl+";
            if (b.Shift) combo += "Shift+";
            if (b.Alt) combo += "Alt+";
            combo += b.Key.ToString();
            bindingsList.Items.Add(combo + " \u2192 " + b.InstructionFile);
        }
    }

    private void ReloadButton_Click(object sender, EventArgs e)
    {
        LoadBindings();
    }

    private void AddBindingButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show(
            "Функция добавления биндингов через GUI будет реализована в следующей версии.\n\n" +
            "Сейчас вы можете добавить биндинги вручную в файл config.txt.",
            "KeyRemapper",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    public void ExitApplication(object sender, EventArgs e)
    {
        if (statusTimer != null)
        {
            statusTimer.Stop();
            statusTimer.Dispose();
        }
        
        if (overlayStateTimer != null)
        {
            overlayStateTimer.Stop();
            overlayStateTimer.Dispose();
        }
        
        trayIcon.Visible = false;
        trayIcon.Dispose();
        
        Environment.Exit(0);
    }
}