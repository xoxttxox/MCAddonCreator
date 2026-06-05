using System.ComponentModel;

namespace MCAddonCreator.Controls;

public sealed class MinecraftCheckBox : CheckBox
{
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public Color BoxBackColor { get; set; } = Color.FromArgb(214, 214, 214);
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public Color BoxLightColor { get; set; } = Color.FromArgb(232, 232, 232);
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public Color BoxDarkColor { get; set; } = Color.FromArgb(155, 155, 155);
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public Color CheckColor { get; set; } = Color.FromArgb(25, 25, 25);

  public MinecraftCheckBox()
  {
    SetStyle(ControlStyles.UserPaint, true);
    SetStyle(ControlStyles.AllPaintingInWmPaint, true);
    SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
    SetStyle(ControlStyles.ResizeRedraw, true);
    SetStyle(ControlStyles.SupportsTransparentBackColor, true);

    AutoSize = false;
    TabStop = false;
    Cursor = Cursors.Hand;
    BackColor = Color.Transparent;
    ForeColor = Color.FromArgb(235, 238, 242);
    FlatStyle = FlatStyle.Flat;
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    Color bg = Parent?.BackColor ?? Color.FromArgb(18, 20, 23);

    if (bg == Color.Transparent || bg.A < 255)
    {
      bg = Color.FromArgb(18, 20, 23); // deine Card-Farbe
    }

    using SolidBrush bgBrush = new(bg);
    e.Graphics.FillRectangle(bgBrush, ClientRectangle);

    Rectangle box = new(1, 5, 15, 15);

    using SolidBrush boxBrush = new(BoxBackColor);
    using Pen light = new(BoxLightColor, 2);
    using Pen dark = new(BoxDarkColor, 2);

    e.Graphics.FillRectangle(boxBrush, box);

    // oben + links hell
    e.Graphics.DrawLine(light, box.Left, box.Top, box.Right, box.Top);
    e.Graphics.DrawLine(light, box.Left, box.Top, box.Left, box.Bottom);

    // unten + rechts dunkel
    e.Graphics.DrawLine(dark, box.Left, box.Bottom, box.Right, box.Bottom);
    e.Graphics.DrawLine(dark, box.Right, box.Top, box.Right, box.Bottom);

    if (Checked)
    {
      using Font checkFont = new(Font.FontFamily, 10F, FontStyle.Bold);

      TextRenderer.DrawText(
        e.Graphics,
        "✓",
        checkFont,
        new Rectangle(0, 2, 16, 18),
        CheckColor,
        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
      );
    }

    TextRenderer.DrawText(
      e.Graphics,
      Text,
      Font,
      new Rectangle(24, 0, Width - 24, Height),
      ForeColor,
      TextFormatFlags.Left | TextFormatFlags.VerticalCenter
    );
  }

  protected override void OnCheckedChanged(EventArgs e)
  {
    base.OnCheckedChanged(e);
    Invalidate();
  }

  protected override void OnMouseEnter(EventArgs e)
  {
    base.OnMouseEnter(e);
    Invalidate();
  }

  protected override void OnMouseLeave(EventArgs e)
  {
    base.OnMouseLeave(e);
    Invalidate();
  }
}