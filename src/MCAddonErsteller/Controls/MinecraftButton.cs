using System.ComponentModel;

namespace MCAddonCreator.Controls;

public sealed class MinecraftButton : Button
{
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public Color BorderLightColor { get; set; } = Color.FromArgb(230, 230, 230);
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public Color BorderDarkColor { get; set; } = Color.FromArgb(190, 190, 190);
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int BorderSize { get; set; } = 6;

  public MinecraftButton()
  {
    FlatStyle = FlatStyle.Flat;
    FlatAppearance.BorderSize = 0;
    UseVisualStyleBackColor = false;
    Cursor = Cursors.Hand;
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    e.Graphics.Clear(BackColor);

    Rectangle r = ClientRectangle;

    using Pen topRight = new(BorderLightColor, BorderSize);
    using Pen leftBottom = new(BorderDarkColor, BorderSize);

    e.Graphics.DrawLine(topRight, 0, 0, r.Width, 0);
    e.Graphics.DrawLine(topRight, r.Width - 1, 0, r.Width - 1, r.Height);

    e.Graphics.DrawLine(leftBottom, 0, 0, 0, r.Height);
    e.Graphics.DrawLine(leftBottom, 0, r.Height - 1, r.Width, r.Height - 1);

    TextRenderer.DrawText(
      e.Graphics,
      Text,
      Font,
      r,
      ForeColor,
      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
    );
  }
}