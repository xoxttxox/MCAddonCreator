using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MCAddonCreator.Controls;

public class MinecraftProgressBar : Control
{
  private int _value = 0;
  private int _maximum = 100;

  [DefaultValue(0)]
  public int Value
  {
    get => _value;
    set
    {
      _value = Math.Clamp(value, 0, Maximum);
      Invalidate();
    }
  }

  [DefaultValue(100)]
  public int Maximum
  {
    get => _maximum;
    set
    {
      _maximum = Math.Max(1, value);
      if (_value > _maximum) _value = _maximum;
      Invalidate();
    }
  }

  public MinecraftProgressBar()
  {
    DoubleBuffered = true;
    Size = new Size(200, 16);
    BackColor = Color.FromArgb(16, 17, 20);
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    base.OnPaint(e);

    e.Graphics.Clear(BackColor);

    // Border
    using Pen border = new(Color.FromArgb(55, 58, 62));
    e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

    // Background inner
    Rectangle inner = new(2, 2, Width - 4, Height - 4);
    using SolidBrush bg = new(Color.FromArgb(26, 28, 32));
    e.Graphics.FillRectangle(bg, inner);

    // Foreground progress
    float ratio = Maximum <= 0 ? 0f : Value / (float)Maximum;
    int fillWidth = (int)Math.Round(inner.Width * ratio);
    if (fillWidth > 0)
    {
      Rectangle fill = new(inner.X, inner.Y, fillWidth, inner.Height);
      using SolidBrush fillBrush = new(Color.FromArgb(91, 178, 88));
      e.Graphics.FillRectangle(fillBrush, fill);
    }

    // Text
    string text = $"Progress: {Math.Clamp((int)Math.Round(ratio * 100), 0, 100)}%";
    TextRenderer.DrawText(e.Graphics, text, Font, inner, Color.FromArgb(235, 238, 242), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
  }
}
