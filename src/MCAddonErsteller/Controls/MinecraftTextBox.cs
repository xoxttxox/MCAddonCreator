using System.ComponentModel;

namespace MCAddonCreator.Controls;

public sealed class MinecraftTextBox : UserControl
{
  private readonly TextBox _textBox = new();
  private readonly Label _placeholderLabel = new();

  private string _placeholderText = "";

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public new string Text
  {
    get => _textBox.Text;
    set
    {
      _textBox.Text = value ?? string.Empty;
      UpdatePlaceholder();
    }
  }

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public string PlaceholderText
  {
    get => _placeholderText;
    set
    {
      _placeholderText = value;
      _placeholderLabel.Text = value;
      UpdatePlaceholder();
    }
  }

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public bool ReadOnly
  {
    get => _textBox.ReadOnly;
    set
    {
      _textBox.ReadOnly = value;
      // Fully disable the inner textbox when read-only so no interaction is possible
      _textBox.Enabled = !value;
      // Prevent keyboard/tab focus when read-only and show default cursor
      _textBox.TabStop = !value;
      _textBox.Cursor = value ? Cursors.Default : Cursors.IBeam;
      _placeholderLabel.Cursor = value ? Cursors.Default : Cursors.IBeam;
      // Disable selection, context menu and shortcuts when read-only
      _textBox.ShortcutsEnabled = !value;
      _textBox.ContextMenuStrip = value ? null : new ContextMenuStrip();

      if (value)
      {
        _textBox.SelectionLength = 0;
        _textBox.DeselectAll();
      }

      UpdatePlaceholder();
    }
  }

  public new event EventHandler? TextChanged
  {
    add => _textBox.TextChanged += value;
    remove => _textBox.TextChanged -= value;
  }

  public MinecraftTextBox()
  {
    Size = new Size(292, 30);
    BackColor = Color.FromArgb(16, 17, 20);
    ForeColor = Color.FromArgb(235, 238, 242);
    Font = new Font("Segoe UI", 9F);

    _textBox.BorderStyle = BorderStyle.None;
    _textBox.BackColor = BackColor;
    _textBox.ForeColor = ForeColor;
    _textBox.Font = Font;
    _textBox.Location = new Point(8, 7);
    _textBox.Size = new Size(Width - 16, 18);

    _placeholderLabel.BackColor = Color.Transparent;
    _placeholderLabel.ForeColor = Color.FromArgb(120, 126, 135);
    _placeholderLabel.Font = Font;
    _placeholderLabel.Location = new Point(8, 0);
    _placeholderLabel.Size = new Size(Width - 16, Height);
    _placeholderLabel.TextAlign = ContentAlignment.MiddleLeft;
    _placeholderLabel.Cursor = Cursors.IBeam;
    _placeholderLabel.Click += (_, _) =>
    {
      if (!_textBox.ReadOnly)
        _textBox.Focus();
    };

    // Prevent context menu and shortcuts by default; will be toggled with ReadOnly setter
    _textBox.ContextMenuStrip = new ContextMenuStrip();
    _textBox.ShortcutsEnabled = true;

    // Prevent mouse interactions when read-only: redirect focus away and clear selection
    _textBox.MouseDown += (_, _) =>
    {
      if (_textBox.ReadOnly)
      {
        _textBox.SelectionLength = 0;
        this.SelectNextControl(this, forward: true, tabStopOnly: true, nested: true, wrap: true);
      }
    };

    _textBox.MouseClick += (_, _) =>
    {
      if (_textBox.ReadOnly)
      {
        _textBox.SelectionLength = 0;
        this.SelectNextControl(this, forward: true, tabStopOnly: true, nested: true, wrap: true);
      }
    };

    _textBox.DoubleClick += (_, _) =>
    {
      if (_textBox.ReadOnly)
      {
        _textBox.SelectionLength = 0;
        this.SelectNextControl(this, forward: true, tabStopOnly: true, nested: true, wrap: true);
      }
    };

    Controls.Add(_placeholderLabel);
    Controls.Add(_textBox);

    _textBox.TextChanged += (_, _) => UpdatePlaceholder();
    _textBox.GotFocus += (_, _) =>
    {
      // If textbox somehow gets focus while read-only, move focus to next control
      if (_textBox.ReadOnly)
      {
        this.SelectNextControl(this, forward: true, tabStopOnly: true, nested: true, wrap: true);
      }
      else
      {
        UpdatePlaceholder();
      }
    };
    _textBox.LostFocus += (_, _) => UpdatePlaceholder();

    UpdatePlaceholder();
  }

  private void UpdatePlaceholder()
  {
    _placeholderLabel.Visible = string.IsNullOrEmpty(_textBox.Text);
    _placeholderLabel.BringToFront();
  }

  public void Clear() => _textBox.Clear();
  public new void Focus() => _textBox.Focus();
  public void SelectAll() => _textBox.SelectAll();

  protected override void OnBackColorChanged(EventArgs e)
  {
    base.OnBackColorChanged(e);
    _textBox.BackColor = BackColor;
    _placeholderLabel.BackColor = BackColor;
    Invalidate();
  }

  protected override void OnForeColorChanged(EventArgs e)
  {
    base.OnForeColorChanged(e);
    _textBox.ForeColor = ForeColor;
  }

  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);
    _textBox.Font = Font;
    _placeholderLabel.Font = Font;
    Reposition();
  }

  protected override void OnResize(EventArgs e)
  {
    base.OnResize(e);
    Reposition();
  }

  private void Reposition()
  {
    _textBox.Location = new Point(8, Math.Max(1, ((Height - _textBox.Height) / 2) - 1));
    _textBox.Width = Math.Max(1, Width - 16);

    _placeholderLabel.Location = new Point(5, -1);
    _placeholderLabel.Size = new Size(Math.Max(1, Width - 16), Height);
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    using SolidBrush bg = new(BackColor);
    e.Graphics.FillRectangle(bg, ClientRectangle);

    using Pen border = new(Color.FromArgb(45, 48, 52));
    e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
  }
}