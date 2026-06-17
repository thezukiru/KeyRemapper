using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

/// <summary>
/// Коллекция стилизованных UI-контролов в стиле Province Helper Lite.
/// Все элементы имеют закругленные углы, мягкие цвета и современный вид.
/// </summary>

// ============================================================================
// СТИЛИЗОВАННАЯ КНОПКА (RoundedButton)
// ============================================================================
public class RoundedButton : Button
{
    private int _cornerRadius = 8;
    private Color _hoverColor;
    private Color _normalColor;
    private bool _isHovered = false;

    public int CornerRadius
    {
        get { return _cornerRadius; }
        set { _cornerRadius = value; Invalidate(); }
    }

    public Color NormalColor
    {
        get { return _normalColor; }
        set { _normalColor = value; Invalidate(); }
    }

    public Color HoverColor
    {
        get { return _hoverColor; }
        set { _hoverColor = value; Invalidate(); }
    }

    public RoundedButton()
    {
        _normalColor = Color.FromArgb(60, 60, 70);
        _hoverColor = Color.FromArgb(80, 80, 95);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = _normalColor;
        ForeColor = Color.FromArgb(220, 220, 230);
        Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        
        MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
        MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath path = GetRoundedRect(rect, _cornerRadius))
        {
            Color bgColor = _isHovered ? _hoverColor : _normalColor;
            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            if (_isHovered)
            {
                using (Pen pen = new Pen(Color.FromArgb(100, 200, 255), 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor, 
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = radius * 2;
        
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        
        return path;
    }
}

// ============================================================================
// СТИЛИЗОВАННОЕ ТЕКСТОВОЕ ПОЛЕ (StyledTextBox)
// Реализовано как UserControl с внутренним TextBox для корректной отрисовки
// ============================================================================
public class StyledTextBox : UserControl
{
    private TextBox _textBox;
    private Panel _borderPanel;
    private int _cornerRadius = 6;
    private Color _borderColor = Color.FromArgb(80, 80, 95);
    private Color _focusedBorderColor = Color.FromArgb(100, 200, 255);
    private Color _backgroundColor = Color.FromArgb(45, 45, 55);
    private Color _foregroundColor = Color.FromArgb(220, 220, 230);
    private string _placeholderText = "";
    private Color _placeholderColor = Color.FromArgb(100, 100, 120);
    private Label _placeholderLabel;

    public int CornerRadius
    {
        get { return _cornerRadius; }
        set { _cornerRadius = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get { return _borderColor; }
        set { _borderColor = value; Invalidate(); }
    }

    public Color FocusedBorderColor
    {
        get { return _focusedBorderColor; }
        set { _focusedBorderColor = value; }
    }

    public new Color BackColor
    {
        get { return _backgroundColor; }
        set 
        { 
            _backgroundColor = value; 
            if (_textBox != null) _textBox.BackColor = value; if (_borderPanel != null) _borderPanel.BackColor = value;
        }
    }

    public new Color ForeColor
    {
        get { return _foregroundColor; }
        set 
        { 
            _foregroundColor = value; 
            if (_textBox != null) _textBox.ForeColor = value;
        }
    }

    public string PlaceholderText
    {
        get { return _placeholderText; }
        set 
        { 
            _placeholderText = value; 
            if (_placeholderLabel != null)
            {
                _placeholderLabel.Text = value;
                UpdatePlaceholderVisibility();
            }
        }
    }

    public string Text
    {
        get { return _textBox != null ? _textBox.Text : ""; }
        set 
        { 
            if (_textBox != null) 
            {
                _textBox.Text = value;
                UpdatePlaceholderVisibility();
            }
        }
    }

    public new Font Font
    {
        get { return _textBox != null ? _textBox.Font : base.Font; }
        set 
        { 
            if (_textBox != null) _textBox.Font = value;
            if (_placeholderLabel != null) _placeholderLabel.Font = value;
        }
    }

    public event EventHandler TextChanged
    {
        add { if (_textBox != null) _textBox.TextChanged += value; }
        remove { if (_textBox != null) _textBox.TextChanged -= value; }
    }

    public event EventHandler GotFocus
    {
        add { if (_textBox != null) _textBox.GotFocus += value; }
        remove { if (_textBox != null) _textBox.GotFocus -= value; }
    }

    public event EventHandler LostFocus
    {
        add { if (_textBox != null) _textBox.LostFocus += value; }
        remove { if (_textBox != null) _textBox.LostFocus -= value; }
    }

    public StyledTextBox()
    {
        this.Size = new Size(200, 32);
        this.Padding = new Padding(2);
        
        // Создаем панель для фона
        _borderPanel = new Panel();
        _borderPanel.Dock = DockStyle.Fill;
        _borderPanel.BackColor = _backgroundColor;
        _borderPanel.Padding = new Padding(2);
        this.Controls.Add(_borderPanel);

        // Создаем внутренний TextBox ДО добавления в Controls
        _textBox = new TextBox();
        _textBox.Dock = DockStyle.Fill;
        _textBox.BorderStyle = BorderStyle.None;
        _textBox.BackColor = _backgroundColor;
        _textBox.ForeColor = _foregroundColor;
        _textBox.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        _textBox.Multiline = false;
        
        // Placeholder label создаем ДО использования
        _placeholderLabel = new Label();
        _placeholderLabel.Text = _placeholderText;
        _placeholderLabel.ForeColor = _placeholderColor;
        _placeholderLabel.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        _placeholderLabel.AutoSize = false;
        _placeholderLabel.TextAlign = ContentAlignment.MiddleLeft;
        _placeholderLabel.Location = new Point(8, 4);
        _placeholderLabel.Visible = false;
        _placeholderLabel.Click += (s, e) => { if (_textBox != null) _textBox.Focus(); };
        
        // Добавляем controls в правильном порядке _borderPanel.Controls.Add(_placeholderLabel);
        _borderPanel.Controls.Add(_textBox);
        
        // Bring textbox to front so it's above placeholder
        _textBox.BringToFront();
        
        // Подписываемся на события ПОСЛЕ создания всех элементов
        _textBox.GotFocus += (s, e) => { if (_placeholderLabel != null) _placeholderLabel.Visible = false; };
        _textBox.LostFocus += (s, e) => UpdatePlaceholderVisibility();
        _textBox.TextChanged += (s, e) => UpdatePlaceholderVisibility();
    }

    private void UpdatePlaceholderVisibility()
    {
        if (_placeholderLabel != null && _textBox != null)
        {
            bool showPlaceholder = string.IsNullOrEmpty(_textBox.Text) && 
                                   !string.IsNullOrEmpty(_placeholderText) &&
                                   !_textBox.Focused;
            _placeholderLabel.Visible = showPlaceholder;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_placeholderLabel != null)
        {
            _placeholderLabel.Size = new Size(this.Width - 16, this.Height - 8);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Рисуем закругленную рамку поверх всего
        Rectangle rect = new Rectangle(1, 1, Width - 3, Height - 3);
        bool isFocused = _textBox != null && _textBox.Focused;
        Color borderColor = isFocused ? _focusedBorderColor : _borderColor;
        
        using (GraphicsPath path = GetRoundedRect(rect, _cornerRadius))
        {
            using (Pen pen = new Pen(borderColor, isFocused ? 2f : 1.5f))
            {
                g.DrawPath(pen, path);
            }
        }
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = radius * 2;
        
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        
        return path;
    }
}

// ============================================================================
// ЛЕЙБЛ С ЗАГОЛОВКОМ (LabeledField) - комбинация Label + TextBox
// ============================================================================
public class LabeledField : UserControl
{
    private Label _label;
    private StyledTextBox _textBox;
    private string _fieldText = "";

    public string FieldText
    {
        get { return _textBox.Text; } set { _textBox.Text = value; _fieldText = value; }
    }

    public string FieldLabel
    {
        get { return _label.Text; }
        set { _label.Text = value; }
    }

    public LabeledField()
    {
        BackColor = Color.Transparent;
        
        _label = new Label();
        _label.AutoSize = true;
        _label.ForeColor = Color.FromArgb(150, 160, 180);
        _label.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _label.Location = new Point(0, 0);
        
        _textBox = new StyledTextBox();
        _textBox.Location = new Point(0, 22);
        _textBox.Size = new Size(200, 32);
        Controls.Add(_label);
        Controls.Add(_textBox);
        
        Size = new Size(200, 60);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _textBox.Width = Width;
    }
}

// ============================================================================
// ИНДИКАТОР СТАТУСА (StatusIndicator) - кружок с текстом
// ============================================================================
public class StatusIndicator : UserControl
{
    private Color _activeColor = Color.FromArgb(80, 200, 120);
    private Color _inactiveColor = Color.FromArgb(200, 80, 80);
    private bool _isActive = false;
    private string _statusText = "Не активно";

    public bool IsActive
    {
        get { return _isActive; }
        set { _isActive = value; Invalidate(); }
    }

    public string StatusText
    {
        get { return _statusText; }
        set { _statusText = value; Invalidate(); }
    }

    public StatusIndicator()
    {
        Size = new Size(180, 30);
        BackColor = Color.Transparent;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int circleSize = 12;
        int circleY = (Height - circleSize) / 2;
        
        Color circleColor = _isActive ? _activeColor : _inactiveColor;
        using (SolidBrush brush = new SolidBrush(circleColor))
        {
            g.FillEllipse(brush, 0, circleY, circleSize, circleSize);
        }

        TextRenderer.DrawText(g, _statusText, Font, 
            new Rectangle(circleSize + 8, 0, Width - circleSize - 8, Height),
            ForeColor, TextFormatFlags.VerticalCenter);
    }
}

// ============================================================================
// ПАНЕЛЬ С ЗАКРУГЛЕННЫМИ УГЛАМИ (RoundedPanel)
// ============================================================================
public class RoundedPanel : Panel
{
    private int _cornerRadius = 10;
    private Color _borderColor = Color.FromArgb(60, 60, 75);
    private bool _showBorder = true;

    public int CornerRadius
    {
        get { return _cornerRadius; }
        set { _cornerRadius = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get { return _borderColor; }
        set { _borderColor = value; Invalidate(); }
    }

    public bool ShowBorder
    {
        get { return _showBorder; }
        set { _showBorder = value; Invalidate(); }
    }

    public RoundedPanel()
    {
        BackColor = Color.FromArgb(35, 35, 45);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath path = GetRoundedRect(rect, _cornerRadius))
        {
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            if (_showBorder)
            {
                using (Pen pen = new Pen(_borderColor, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = radius * 2;
        
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        
        return path;
    }
}