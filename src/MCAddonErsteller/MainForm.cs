using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using MCAddonCreator.Controls;
using MCAddonCreator.Models;
using MCAddonCreator.Services;

namespace MCAddonCreator;

public sealed partial class MainForm : Form
{
  private static readonly Color WindowBack = Color.FromArgb(242, 244, 248);
  private static readonly Color CardBack = Color.FromArgb(218, 18, 20, 23);
  private static readonly Color BorderColor = Color.FromArgb(55, 58, 62);
  private static readonly Color TextMain = Color.FromArgb(235, 238, 242);
  private static readonly Color TextMuted = Color.FromArgb(170, 176, 184);
  private static readonly Color StatusDark = Color.FromArgb(15, 18, 27);
  private static readonly Color LogColorInfo = Color.FromArgb(95, 170, 235); // Info -> blue
  private static readonly Color LogColorWarning = Color.FromArgb(245, 190, 70);
  private static readonly Color LogColorError = Color.FromArgb(240, 90, 90);
  private static readonly Color LogColorSuccess = Color.FromArgb(70, 220, 120);

  private readonly MinecraftCheckBox _includeBpCheck = new();
  private readonly MinecraftCheckBox _includeRpCheck = new();
  private readonly MinecraftTextBox _bpPathText = new();
  private readonly MinecraftTextBox _rpPathText = new();
  private readonly Label _bpInfoLabel = new();
  private readonly Label _rpInfoLabel = new();
  private readonly MinecraftTextBox _packageNameText = new();
  private readonly MinecraftTextBox _versionText = new();
  private readonly MinecraftTextBox _outputPathText = new();
  private readonly RichTextBox _logText = new();
  private readonly MinecraftTextBox _fileNamePreviewText = new();
  private readonly MinecraftProgressBar _buildProgress = new();
  private readonly Label _buildProgressLabel = new();
  private readonly MinecraftButton _buildButton = new();
  private readonly Label _statusLabel = new();
  private readonly Label _statusDot = new();
  private readonly Label _versionLabel = new();
  private readonly MinecraftButton _updateButton = new();
  private readonly ComboBox _logFilterCombo = new();
  private readonly MinecraftTextBox _logSearchBox = new();
  private MinecraftButton _logClearButton = null!;
  private MinecraftButton _logSaveButton = null!;
  private readonly List<LogEntry> _logEntries = [];
  private const int MaxLogEntries = 1000;
  private readonly bool _logAutoScroll = true;
  private string? _lastStatusLogText = null;
  private DateTime _lastStatusLogTime = DateTime.MinValue;

  public MainForm()
  {
    Text = "MC Addon Creator";
    StartPosition = FormStartPosition.CenterScreen;
    FormBorderStyle = FormBorderStyle.FixedSingle;
    MaximizeBox = false;
    MinimizeBox = true;
    ClientSize = new Size(920, 580);
    MinimumSize = Size;
    MaximumSize = Size;
    BackColor = WindowBack;
    BackgroundImage = Properties.Resources.background;
    BackgroundImageLayout = ImageLayout.Stretch;
    Font = new Font(FontManager.Noto, 9F, FontStyle.Regular, GraphicsUnit.Point);
    AutoScaleMode = AutoScaleMode.Dpi;

    ApplyAppIcon();
    BuildUi();
    UpdateFileNamePreview();
    SetDefaults();

    Shown += MainForm_Shown;
  }

  private void LogFilterCombo_DrawItem(object? sender, DrawItemEventArgs e)
  {
    if (e.Index < 0) return;

    if (sender is not ComboBox combo)
    {
      // Nothing to draw if sender is not a ComboBox
      return;
    }

    object? item = null;
    try { item = combo.Items[e.Index]; } catch { }
    string text = item?.ToString() ?? string.Empty;

    e.DrawBackground();

    // Safely compute hover/selection background
    bool isSelected = (e.State & DrawItemState.Selected) != 0;
    Point clientCursor = combo.PointToClient(Cursor.Position);
    Color back = (e.Bounds.Contains(clientCursor) && isSelected)
      ? Color.FromArgb(200, 200, 200)
      : combo.BackColor;

    using SolidBrush bg = new(back);
    e.Graphics.FillRectangle(bg, e.Bounds);

    Color fore = combo.ForeColor;
    TextRenderer.DrawText(e.Graphics, text, combo.Font, e.Bounds, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    e.DrawFocusRectangle();
  }

  [LibraryImport("dwmapi.dll")]
  private static partial int DwmSetWindowAttribute(
    IntPtr hwnd,
    int attr,
    ref int attrValue,
    int attrSize
  );

  private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
  private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

  protected override void OnHandleCreated(EventArgs e)
  {
    base.OnHandleCreated(e);

    int useDarkMode = 1;

    int result = DwmSetWindowAttribute(
      Handle,
      DWMWA_USE_IMMERSIVE_DARK_MODE,
      ref useDarkMode,
      sizeof(int)
    );

    if (result != 0)
    {
      _ = DwmSetWindowAttribute(
        Handle,
        DWMWA_USE_IMMERSIVE_DARK_MODE_OLD,
        ref useDarkMode,
        sizeof(int)
      );
    }
  }

  private sealed record LogEntry(DateTime Timestamp, string Message, MCAddonCreator.Models.LogLevel Level)
  {
    public Color Color => Level switch
    {
      MCAddonCreator.Models.LogLevel.Error => LogColorError,
      MCAddonCreator.Models.LogLevel.Warning => LogColorWarning,
      MCAddonCreator.Models.LogLevel.Success => LogColorSuccess,
      _ => LogColorInfo
    };
  }

  private async void MainForm_Shown(object? sender, EventArgs e)
  {
    await CheckForUpdatesAsync();
  }

  private async Task CheckForUpdatesAsync()
  {
    try
    {
      // Do not log here: UpdateChecker will emit the update check log itself
      SetStatusWorkingNoLog("Checking for updates...");

      UpdateResult update = await UpdateChecker.CheckForUpdateAsync(Log);

      if (update.IsUpdateAvailable)
      {
        _versionLabel.Text = $"v{update.CurrentVersion} → v{update.LatestVersion}";
        SetStatusWorking("Update available");

        DialogResult result = MessageBox.Show(
          this,
          $"A new version is available!\n\nCurrent: v{update.CurrentVersion}\nNew: v{update.LatestVersion}\n\nOpen the release page?",
          "Update available",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Information
        );

        if (result == DialogResult.Yes)
          UpdateChecker.OpenReleasePage();
        else
          _updateButton.Visible = true;
      }
      else
      {
        _versionLabel.Text = $"v{update.CurrentVersion}";
        SetStatusReady("Ready");
        _updateButton.Visible = false;
      }
    }
    catch
    {
      SetStatusReady("Ready");
    }
  }

  private void ApplyAppIcon()
  {
    try
    {
      string? exePath = Application.ExecutablePath;

      if (!string.IsNullOrWhiteSpace(exePath))
      {
        using Icon? extractedIcon = Icon.ExtractAssociatedIcon(exePath);

        if (extractedIcon is not null)
          Icon = (Icon)extractedIcon.Clone();
      }
    }
    catch
    {
      // Fallback: Standard Windows-Icon verwenden.
    }
  }

  private void BuildUi()
  {
    Controls.Add(CreateStatusFooter());
    Controls.Add(CreateHeader());

    Panel bpCard = CreateCard("Behavior Pack", "Select BP folder, ZIP, MCPACK or MCADDON", Properties.Resources.bp_icon, new Point(18, 72), new Size(430, 175));
    BuildPackSelector(bpCard, isBehaviorPack: true);
    Controls.Add(bpCard);

    Panel rpCard = CreateCard("Resource Pack", "Select RP folder, ZIP, MCPACK or MCADDON", Properties.Resources.rp_icon, new Point(472, 72), new Size(430, 175));
    BuildPackSelector(rpCard, isBehaviorPack: false);
    Controls.Add(rpCard);

    Panel outputCard = CreateCard("Output", "Name, version and location for the .mcaddon", Properties.Resources.folder_icon, new Point(18, 260), new Size(430, 218));
    BuildOutputArea(outputCard);
    _packageNameText.TextChanged += (_, _) => UpdateFileNamePreview();
    _versionText.TextChanged += (_, _) => UpdateFileNamePreview();
    Controls.Add(outputCard);

    Panel logCard = CreateCard("Build Log", "Here you can see what the launcher does.", Properties.Resources.log_icon, new Point(472, 260), new Size(430, 218));
    BuildLogArea(logCard);
    Controls.Add(logCard);

    _buildButton.Text = "CREATE MCADDON";
    _buildButton.Location = new Point(25, 495);
    _buildButton.Size = new Size(867, 40);
    StylePrimaryButton(_buildButton);
    _buildButton.Click += BuildButton_Click;
    Controls.Add(_buildButton);
  }

  private Panel CreateHeader()
  {
    Panel header = new()
    {
      Location = new Point(0, 0),
      Size = new Size(ClientSize.Width, 72),
      Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
      BackColor = Color.Transparent
    };

    PictureBox logo = new()
    {
      Image = Properties.Resources.app_icon_trans,
      SizeMode = PictureBoxSizeMode.Zoom,
      Location = new Point(24, 10),
      Size = new Size(55, 55),
      BackColor = Color.Transparent
    };

    Label title = new()
    {
      Text = "MC Addon Creator",
      AutoSize = true,
      ForeColor = Color.White,
      Font = new Font(FontManager.Minecraft, 13F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(85, 15),
      BackColor = Color.Transparent,
      UseCompatibleTextRendering = false
    };

    Label subtitle = new()
    {
      Text = "Pack BP/RP folders or ZIP files into a clean .mcaddon",
      AutoSize = true,
      ForeColor = Color.FromArgb(210, 224, 245),
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(87, 40),
      BackColor = Color.Transparent,
      UseCompatibleTextRendering = true
    };

    MinecraftButton tag = new()
    {
      Text = "MINECRAFT BEDROCK",
      ForeColor = Color.FromArgb(40, 40, 40),
      BackColor = Color.FromArgb(214, 214, 214),
      BorderLightColor = Color.FromArgb(230, 230, 230),
      BorderDarkColor = Color.FromArgb(190, 190, 190),
      TextAlign = ContentAlignment.MiddleCenter,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(ClientSize.Width - 188, 24),
      Size = new Size(164, 28),
      Anchor = AnchorStyles.Top | AnchorStyles.Right,
      Enabled = false
    };

    _updateButton.Text = "UPDATE";
    _updateButton.ForeColor = Color.White;
    _updateButton.BackColor = Color.FromArgb(91, 178, 88);
    _updateButton.BorderLightColor = Color.FromArgb(115, 200, 105);
    _updateButton.BorderDarkColor = Color.FromArgb(48, 105, 48);
    _updateButton.Font = new Font(FontManager.Metropolis, 8F, FontStyle.Bold, GraphicsUnit.Point);
    _updateButton.Size = new Size(88, 28);
    _updateButton.Location = new Point(ClientSize.Width - 288, 24);
    _updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    _updateButton.Visible = false;
    _updateButton.Click += (_, _) => UpdateChecker.OpenReleasePage();

    header.Controls.Add(_updateButton);

    header.Controls.Add(logo);
    header.Controls.Add(title);
    header.Controls.Add(subtitle);
    header.Controls.Add(tag);

    return header;
  }

  private Panel CreateStatusFooter()
  {
    Panel footer = new()
    {
      Location = new Point(0, ClientSize.Height - 28),
      Size = new Size(ClientSize.Width, 28),
      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
      BackColor = Color.Transparent
    };

    Panel topLine = new()
    {
      Location = new Point(0, 0),
      Size = new Size(footer.Width, 1),
      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
      BackColor = Color.FromArgb(55, 58, 62)
    };

    Panel bottomLine = new()
    {
      Location = new Point(0, footer.Height - 1),
      Size = new Size(footer.Width, 1),
      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
      BackColor = Color.FromArgb(18, 19, 22)
    };

    _statusDot.Text = "●";
    _statusDot.ForeColor = LogColorSuccess;
    _statusDot.BackColor = Color.Transparent;
    _statusDot.Location = new Point(14, 5);
    _statusDot.Size = new Size(16, 18);
    _statusDot.Font = new Font(FontManager.Metropolis, 9F, FontStyle.Bold);

    _statusLabel.Text = "Ready";
    _statusLabel.ForeColor = Color.FromArgb(228, 232, 240);
    _statusLabel.BackColor = Color.Transparent;
    _statusLabel.Location = new Point(34, 6);
    _statusLabel.Size = new Size(500, 18);
    _statusLabel.Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular);

    _versionLabel.Text = $"v{GetAppVersion()}";
    _versionLabel.ForeColor = Color.FromArgb(150, 163, 184);
    _versionLabel.BackColor = Color.Transparent;
    _versionLabel.TextAlign = ContentAlignment.MiddleRight;
    _versionLabel.Location = new Point(ClientSize.Width - 120, 6);
    _versionLabel.Size = new Size(100, 18);
    _versionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    _versionLabel.Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular);

    footer.Controls.Add(topLine);
    footer.Controls.Add(bottomLine);
    footer.Controls.Add(_statusDot);
    footer.Controls.Add(_statusLabel);
    footer.Controls.Add(_versionLabel);

    return footer;
  }

  private static BorderPanel CreateCard(string title, string subtitle, Image icon, Point location, Size size)
  {
    const int padding = 5;
    const int iconSize = 60;
    const int headerHeight = 66;

    BorderPanel card = new()
    {
      Location = location,
      Size = size,
      BackColor = CardBack,
      BorderColor = BorderColor,
      BorderWidth = 1
    };

    PictureBox iconBox = new()
    {
      Image = icon,
      SizeMode = PictureBoxSizeMode.Zoom,
      Location = new Point(padding, 6),
      Size = new Size(iconSize, iconSize),
      BackColor = Color.Transparent
    };

    Label titleLabel = new()
    {
      Text = title,
      ForeColor = TextMain,
      Font = new Font(FontManager.Metropolis, 11.5F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(iconSize, 14),
      AutoSize = true,
      BackColor = Color.Transparent
    };

    Label subtitleLabel = new()
    {
      Text = subtitle,
      ForeColor = TextMuted,
      Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(iconSize + 1, 35),
      AutoSize = true,
      BackColor = Color.Transparent
    };

    Panel line = new()
    {
      Location = new Point(padding + 10, headerHeight),
      Size = new Size(size.Width - padding * 6, 1),
      BackColor = Color.FromArgb(52, 54, 58)
    };

    card.Controls.Add(iconBox);
    card.Controls.Add(titleLabel);
    card.Controls.Add(subtitleLabel);
    card.Controls.Add(line);

    return card;
  }

  private void BuildPackSelector(Panel parent, bool isBehaviorPack)
  {
    MinecraftCheckBox check = isBehaviorPack ? _includeBpCheck : _includeRpCheck;
    MinecraftTextBox path = isBehaviorPack ? _bpPathText : _rpPathText;
    Label info = isBehaviorPack ? _bpInfoLabel : _rpInfoLabel;

    check.Text = "";
    check.Location = new Point(16, 75);
    check.Size = new Size(18, 24);
    check.BackColor = CardBack;
    parent.Controls.Add(check);

    Label checkLabel = new()
    {
      Text = isBehaviorPack ? "Use BP" : "Use RP",
      Location = new Point(36, 80),
      Size = new Size(120, 24),
      ForeColor = TextMain,
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular, GraphicsUnit.Point)
    };

    checkLabel.Click += (_, _) => check.Checked = !check.Checked;

    parent.Controls.Add(checkLabel);

    path.Location = new Point(16, 105);
    path.Size = new Size(300, 25);
    path.ReadOnly = true;
    path.PlaceholderText = "Nothing selected";
    StyleTextBox(path);
    parent.Controls.Add(path);

    Button folderButton = CreateSmallButton("Folder", new Point(326, 105), new Size(88, 25));
    folderButton.Click += (_, _) => BrowseFolder(isBehaviorPack);
    parent.Controls.Add(folderButton);

    Button fileButton = CreateSmallButton("ZIP", new Point(326, 135), new Size(42, 25));
    fileButton.Click += (_, _) => BrowseFile(isBehaviorPack);
    parent.Controls.Add(fileButton);

    Button clearButton = CreateSmallButton("X", new Point(372, 135), new Size(42, 25));
    clearButton.Click += (_, _) => ClearPack(isBehaviorPack);
    parent.Controls.Add(clearButton);

    info.Text = "Manifest: -";
    info.Location = new Point(16, 143);
    info.Size = new Size(292, 34);
    info.ForeColor = TextMuted;
    info.BackColor = Color.Transparent;
    info.Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(info);
  }

  private void BuildOutputArea(Panel parent)
  {
    AddFieldLabel(parent, "Addon Name", new Point(16, 80));

    _packageNameText.Location = new Point(112, 76);
    _packageNameText.Size = new Size(300, 25);
    _packageNameText.PlaceholderText = "z.B. MeinAddon";
    StyleTextBox(_packageNameText);
    parent.Controls.Add(_packageNameText);

    AddFieldLabel(parent, "Version", new Point(16, 113));

    _versionText.Location = new Point(112, 110);
    _versionText.Size = new Size(80, 25);
    _versionText.PlaceholderText = "1.0.0";
    StyleTextBox(_versionText);
    parent.Controls.Add(_versionText);

    Label fileNameLabel = new()
    {
      Text = "File name:",
      Location = new Point(202, 115),
      Size = new Size(72, 22),
      ForeColor = TextMuted,
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point)
    };

    parent.Controls.Add(fileNameLabel);

    _fileNamePreviewText.Location = new Point(274, 110);
    _fileNamePreviewText.Size = new Size(138, 25);
    _fileNamePreviewText.ReadOnly = true;
    StyleTextBox(_fileNamePreviewText);
    parent.Controls.Add(_fileNamePreviewText);

    AddFieldLabel(parent, "Output", new Point(16, 150));

    _outputPathText.Location = new Point(112, 145);
    _outputPathText.Size = new Size(225, 25);
    _outputPathText.ReadOnly = true;
    StyleTextBox(_outputPathText);
    parent.Controls.Add(_outputPathText);

    Button outputButton = CreateSmallButton("Choose", new Point(345, 144), new Size(66, 27));
    outputButton.Click += (_, _) => BrowseOutputFolder();
    parent.Controls.Add(outputButton);

    Label bottomHint = new()
    {
      Text = "Original BP/RP files are not modified.",
      Location = new Point(16, 190),
      Size = new Size(388, 22),
      ForeColor = Color.FromArgb(95, 170, 235),
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point)
    };

    parent.Controls.Add(bottomHint);
  }

  private void BuildLogArea(Panel parent)
  {
    _buildProgressLabel.Text = "Progress: 0%";
    _buildProgressLabel.Location = new Point(16, 80);
    _buildProgressLabel.Size = new Size(120, 20);
    _buildProgressLabel.ForeColor = TextMuted;
    _buildProgressLabel.BackColor = Color.Transparent;
    _buildProgressLabel.Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(_buildProgressLabel);

    _buildProgress.Location = new Point(141, 79);
    _buildProgress.Size = new Size(271, 17);
    _buildProgress.Font = new Font(FontManager.Metropolis, 7.5F, FontStyle.Bold);
    _buildProgress.Maximum = 100;
    _buildProgress.Value = 0;
    parent.Controls.Add(_buildProgress);

    // toolbar: filter, search, clear, save
    _logFilterCombo.Location = new Point(17, 104);
    _logFilterCombo.Size = new Size(90, 24);
    _logFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
    _logFilterCombo.Items.AddRange(["All", "Info", "Warning", "Error", "Success"]);
    _logFilterCombo.SelectedIndex = 0;
    // Minecraft-style owner-drawn combo
    _logFilterCombo.DrawMode = DrawMode.OwnerDrawFixed;
    _logFilterCombo.Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Bold, GraphicsUnit.Point);
    _logFilterCombo.BackColor = Color.FromArgb(214, 214, 214);
    _logFilterCombo.ForeColor = Color.FromArgb(40, 40, 40);
    _logFilterCombo.DrawItem += LogFilterCombo_DrawItem;
    _logFilterCombo.SelectedIndexChanged += (_, _) => RenderLog();
    parent.Controls.Add(_logFilterCombo);

    _logSearchBox.Location = new Point(115, 104);
    _logSearchBox.Size = new Size(175, 24);
    _logSearchBox.PlaceholderText = "Search";
    StyleTextBox(_logSearchBox);
    _logSearchBox.TextChanged += (_, _) => RenderLog();
    parent.Controls.Add(_logSearchBox);

    _logClearButton = new MinecraftButton()
    {
      Location = new Point(296, 104),
      Size = new Size(56, 24),
      Text = "Clear",
      Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Bold),
      ForeColor = Color.FromArgb(40, 40, 40),
      BackColor = Color.FromArgb(214, 214, 214),
      BorderLightColor = Color.FromArgb(230, 230, 230),
      BorderDarkColor = Color.FromArgb(190, 190, 190),
      BorderSize = 3,
      Cursor = Cursors.Hand
    };
    _logClearButton.Click += (_, _) => ClearLog();
    // ensure _logClearButton is added to the parent before any potential use
    parent.Controls.Add(_logClearButton);

    _logSaveButton = new MinecraftButton()
    {
      Location = new Point(356, 104),
      Size = new Size(56, 24),
      Text = "Save",
      Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Bold),
      ForeColor = Color.FromArgb(40, 40, 40),
      BackColor = Color.FromArgb(214, 214, 214),
      BorderLightColor = Color.FromArgb(230, 230, 230),
      BorderDarkColor = Color.FromArgb(190, 190, 190),
      BorderSize = 3,
      Cursor = Cursors.Hand
    };
    _logSaveButton.Click += (_, _) => SaveLogToFile();
    // ensure _logSaveButton is added to the parent before any potential use
    parent.Controls.Add(_logSaveButton);

    _logText.Location = new Point(17, 132);
    _logText.Size = new Size(395, 75);
    _logText.ReadOnly = true;
    _logText.ScrollBars = RichTextBoxScrollBars.Vertical;
    _logText.BackColor = Color.FromArgb(16, 17, 20);
    _logText.ForeColor = Color.FromArgb(235, 238, 242);
    _logText.BorderStyle = BorderStyle.FixedSingle;
    _logText.Font = new Font("Consolas", 8.6F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(_logText);
  }

  private static void AddFieldLabel(Control parent, string text, Point location)
  {
    Label label = new()
    {
      Text = text + ":",
      Location = location,
      Size = new Size(94, 22),
      ForeColor = TextMuted,
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point)
    };

    parent.Controls.Add(label);
  }

  private static MinecraftButton CreateSmallButton(string text, Point location, Size size)
  {
    MinecraftButton button = new()
    {
      Text = text,
      Location = location,
      Size = size,
      Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Bold, GraphicsUnit.Point),
      ForeColor = Color.FromArgb(40, 40, 40),
      BackColor = Color.FromArgb(214, 214, 214),
      BorderLightColor = Color.FromArgb(230, 230, 230),
      BorderDarkColor = Color.FromArgb(190, 190, 190),
      BorderSize = 3,
      Cursor = Cursors.Hand
    };

    return button;
  }

  private static void StylePrimaryButton(Button button)
  {
    button.FlatStyle = FlatStyle.Flat;
    button.FlatAppearance.BorderSize = 0;
    button.BackColor = Color.FromArgb(91, 178, 88);      // Minecraft Grün
    button.ForeColor = Color.White;
    button.Cursor = Cursors.Hand;
    button.Font = new Font(FontManager.Metropolis, 11F, FontStyle.Bold, GraphicsUnit.Point);

    if (button is MinecraftButton mcButton)
    {
      mcButton.BorderLightColor = Color.FromArgb(115, 200, 105);
      mcButton.BorderDarkColor = Color.FromArgb(48, 105, 48);
      mcButton.BorderSize = 4;
    }
  }

  private static void StyleTextBox(MinecraftTextBox textBox)
  {
    textBox.BackColor = Color.FromArgb(16, 17, 20);
    textBox.ForeColor = TextMain;
    textBox.BorderStyle = BorderStyle.FixedSingle;
    textBox.Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular, GraphicsUnit.Point);
  }

  private void SetDefaults()
  {
    _packageNameText.Text = "MyAddon";
    _versionText.Text = "1.0.0";

    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    _outputPathText.Text = string.IsNullOrWhiteSpace(desktop)
      ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
      : desktop;

    Log("Ready. Select BP/RP as folder or ZIP.");
  }

  private void UpdateFileNamePreview()
  {
    if (_fileNamePreviewText.IsDisposed)
      return;

    string safeName = FileNameTools.ToSafeFileName(_packageNameText.Text, "MyAddon");
    string safeVersion = FileNameTools.VersionForFileName(_versionText.Text);

    _fileNamePreviewText.Text = $"{safeName}_v{safeVersion}.mcaddon";
  }

  private void BrowseFolder(bool isBehaviorPack)
  {
    using FolderBrowserDialog dialog = new()
    {
      Description = isBehaviorPack ? "Select Behavior Pack folder" : "Select Resource Pack folder",
      ShowNewFolderButton = false
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
      SetPackPath(isBehaviorPack, dialog.SelectedPath);
  }

  private void BrowseFile(bool isBehaviorPack)
  {
    using OpenFileDialog dialog = new()
    {
      Title = isBehaviorPack ? "Select Behavior Pack file" : "Select Resource Pack file",
      Filter = "Bedrock Packs (*.zip;*.mcpack;*.mcaddon)|*.zip;*.mcpack;*.mcaddon|All files (*.*)|*.*",
      Multiselect = false,
      CheckFileExists = true
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
      SetPackPath(isBehaviorPack, dialog.FileName);
  }

  private void BrowseOutputFolder()
  {
    using FolderBrowserDialog dialog = new()
    {
      Description = "Select output folder",
      ShowNewFolderButton = true,
      SelectedPath = Directory.Exists(_outputPathText.Text)
        ? _outputPathText.Text
        : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
      _outputPathText.Text = dialog.SelectedPath;
  }

  private void SetPackPath(bool isBehaviorPack, string path)
  {
    if (isBehaviorPack)
    {
      _bpPathText.Text = path;
      _includeBpCheck.Checked = true;
    }
    else
    {
      _rpPathText.Text = path;
      _includeRpCheck.Checked = true;
    }

    PreviewPack(isBehaviorPack, path);
  }

  private void ClearPack(bool isBehaviorPack)
  {
    if (isBehaviorPack)
    {
      _bpPathText.Clear();
      _bpInfoLabel.Text = "Manifest: -";
      _includeBpCheck.Checked = false;
    }
    else
    {
      _rpPathText.Clear();
      _rpInfoLabel.Text = "Manifest: -";
      _includeRpCheck.Checked = false;
    }

    UpdateStatus("Source removed.");
  }

  private void PreviewPack(bool isBehaviorPack, string path)
  {
    string role = isBehaviorPack ? "BP" : "RP";
    ResolvedPack? pack = null;

    try
    {
      pack = PackResolver.Resolve(path, role, Log);
      string preview = $"Manifest: {pack.Manifest.Name} | v{pack.Manifest.Version} | {pack.Manifest.Kind}";

      if (isBehaviorPack)
        _bpInfoLabel.Text = preview;
      else
        _rpInfoLabel.Text = preview;

      if (string.IsNullOrWhiteSpace(_packageNameText.Text) || _packageNameText.Text.Equals("MyAddon", StringComparison.OrdinalIgnoreCase))
        _packageNameText.Text = pack.Manifest.Name;

      if (string.IsNullOrWhiteSpace(_versionText.Text) || _versionText.Text == "1.0.0")
        _versionText.Text = pack.Manifest.Version;

      Log($"{role} selected: {pack.ArchiveFolderName}", MCAddonCreator.Models.LogLevel.Info);
      UpdateStatus($"{role} erkannt.");
    }
    catch (Exception ex)
    {
      if (isBehaviorPack)
        _bpInfoLabel.Text = "Manifest: Error";
      else
        _rpInfoLabel.Text = "Manifest: Error";

      Log($"ERROR {role}: {ex.Message}", MCAddonCreator.Models.LogLevel.Error);
      UpdateStatus($"{role} Error.");

      MessageBox.Show(this, ex.Message, "Pack could not be read", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    finally
    {
      if (pack is not null)
        PackResolver.DeleteTemporaryDirectory(pack);
    }
  }

  private async void BuildButton_Click(object? sender, EventArgs e)
  {
    try
    {
      _buildButton.Enabled = false;
      SetBuildProgress(0);
      Log("──────────────── Build started ────────────────");
      SetStatusWorking("Creating MCADDON ...");

      BuildOptions options = new()
      {
        BehaviorPackPath = _bpPathText.Text,
        ResourcePackPath = _rpPathText.Text,
        IncludeBehaviorPack = _includeBpCheck.Checked,
        IncludeResourcePack = _includeRpCheck.Checked,
        PackageName = _packageNameText.Text,
        Version = _versionText.Text,
        OutputDirectory = _outputPathText.Text,
        Log = Log,
        Status = text => SetStatusTextOnly(text),
        StepDelayMilliseconds = 180,
        Progress = new Progress<double>(SetBuildProgress)
      };

      string outputPath = await McAddonBuilder.BuildAsync(options);

      SetBuildProgress(100);
      SetStatusReady("Done.");

      MessageBox.Show(this, $"MCADDON created:\n\n{outputPath}", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
      SetStatusError("Error.");
      Log("ERROR: " + ex.Message);

      MessageBox.Show(this, ex.Message, "Build failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
      _buildButton.Enabled = true;
    }
  }

  // Backwards-compatible overload: infer level from message content/prefix
  private void Log(string message)
  {
    if (_logText.InvokeRequired)
    {
      _logText.BeginInvoke(new Action(() => AppendLogEntry(message)));
      return;
    }

    AppendLogEntry(message);
  }

  // Central logging entry point with explicit level
  private void Log(string message, MCAddonCreator.Models.LogLevel level)
  {
    if (_logText.InvokeRequired)
    {
      _logText.BeginInvoke(new Action(() => AppendLogEntry(message, level)));
      return;
    }

    AppendLogEntry(message, level);
  }

  private void AppendLogEntry(string message)
  {
    // Infer level from message prefixes or content
    LogLevel level = LogLevel.Info;
    string m = message.Trim();
    if (m.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase) || m.Contains("error", StringComparison.OrdinalIgnoreCase) || m.Contains("fehler", StringComparison.OrdinalIgnoreCase))
      level = LogLevel.Error;
    else if (m.StartsWith("WARN", StringComparison.OrdinalIgnoreCase) || m.Contains("warn", StringComparison.OrdinalIgnoreCase) || m.Contains("pack", StringComparison.OrdinalIgnoreCase) || m.Contains("creating", StringComparison.OrdinalIgnoreCase) || m.Contains("prepare", StringComparison.OrdinalIgnoreCase))
      level = LogLevel.Warning;
    else if (m.StartsWith("DONE", StringComparison.OrdinalIgnoreCase) || m.StartsWith("FERTIG", StringComparison.OrdinalIgnoreCase) || m.Contains("done", StringComparison.OrdinalIgnoreCase) || m.Contains("fertig", StringComparison.OrdinalIgnoreCase))
      level = LogLevel.Success;
    AppendLogEntry(message, level);
  }

  private void AppendLogEntry(string message, MCAddonCreator.Models.LogLevel level)
  {
    var entry = new LogEntry(DateTime.Now, message, level);
    lock (_logEntries)
    {
      _logEntries.Add(entry);
      while (_logEntries.Count > MaxLogEntries)
        _logEntries.RemoveAt(0);
    }

    RenderLog();
  }

  private void RenderLog()
  {
    if (_logText.IsDisposed)
      return;

    if (_logText.InvokeRequired)
    {
      _logText.BeginInvoke(new Action(RenderLog));
      return;
    }

    string filter = _logFilterCombo.SelectedItem?.ToString() ?? "All";
    string search = _logSearchBox.Text ?? string.Empty;

    _logText.Clear();

    LogEntry[] entries;
    lock (_logEntries)
      entries = [.. _logEntries];

    foreach (var e in entries)
    {
      if (filter != "All")
      {
        if (filter == "Info" && e.Level != LogLevel.Info) continue;
        if (filter == "Warning" && e.Level != LogLevel.Warning) continue;
        if (filter == "Error" && e.Level != LogLevel.Error) continue;
        if (filter == "Success" && e.Level != LogLevel.Success) continue;
      }

      if (!string.IsNullOrEmpty(search) && !e.Message.Contains(search, StringComparison.OrdinalIgnoreCase))
        continue;

      string time = $"[{e.Timestamp:HH:mm:ss}] ";
      int tstart = _logText.TextLength;
      _logText.AppendText(time);
      _logText.Select(tstart, time.Length);
      _logText.SelectionColor = Color.FromArgb(130, 140, 150);

      int mstart = _logText.TextLength;
      string msg = e.Message + Environment.NewLine;
      _logText.AppendText(msg);
      _logText.Select(mstart, msg.Length);
      _logText.SelectionColor = e.Color;
      _logText.SelectionStart = _logText.TextLength;
      _logText.SelectionLength = 0;
    }

    if (_logAutoScroll)
      _logText.ScrollToCaret();
  }

  private void ClearLog()
  {
    lock (_logEntries)
      _logEntries.Clear();
    RenderLog();
  }

  private void SaveLogToFile()
  {
    using SaveFileDialog dlg = new() { Filter = "Text Files|*.txt|All Files|*.*", FileName = "mcaddon-log.txt" };
    if (dlg.ShowDialog(this) != DialogResult.OK) return;

    try
    {
      File.WriteAllText(dlg.FileName, string.Join(Environment.NewLine, _logEntries.Select(e => $"[{e.Timestamp:O}] {e.Message}")));
    }
    catch (Exception ex)
    {
      MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  private void SetBuildProgress(double value)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetBuildProgress(value)));
      return;
    }

    int progress = Math.Clamp((int)Math.Round(value), 0, 100);

    _buildProgress.Value = progress;
    _buildProgressLabel.Text = $"Progress: {progress}%";
  }

  private void SetStatusTextOnly(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusTextOnly(text)));
      return;
    }

    _statusLabel.Text = text;
    // Log status but avoid immediate duplicates
    if (!IsRecentDuplicateStatus(text))
      Log(text, MCAddonCreator.Models.LogLevel.Info);
  }

  private void SetStatusReady(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusReady(text)));
      return;
    }

    _statusLabel.Text = text;
    _statusDot.ForeColor = LogColorSuccess;
    if (!IsRecentDuplicateStatus(text))
      Log(text, MCAddonCreator.Models.LogLevel.Success);
  }

  private void SetStatusWorking(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusWorking(text)));
      return;
    }

    SetStatusWorkingNoLog(text);
  }

  private void SetStatusWorkingNoLog(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusWorkingNoLog(text)));
      return;
    }

    _statusLabel.Text = text;
    _statusDot.ForeColor = LogColorWarning;
  }

  private void SetStatusError(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusError(text)));
      return;
    }

    _statusLabel.Text = text;
    _statusDot.ForeColor = LogColorError;
    if (!IsRecentDuplicateStatus(text))
      Log(text, MCAddonCreator.Models.LogLevel.Error);
  }

  private bool IsRecentDuplicateStatus(string text)
  {
    if (string.IsNullOrEmpty(_lastStatusLogText))
    {
      _lastStatusLogText = text;
      _lastStatusLogTime = DateTime.Now;
      return false;
    }

    bool same = string.Equals(_lastStatusLogText, text, StringComparison.Ordinal);
    if (same && (DateTime.Now - _lastStatusLogTime).TotalSeconds < 2)
      return true;

    _lastStatusLogText = text;
    _lastStatusLogTime = DateTime.Now;
    return false;
  }

  private void UpdateStatus(string text)
  {
    // Detect error keywords in German and English
    if (text.Contains("Fehler", StringComparison.OrdinalIgnoreCase) || text.Contains("error", StringComparison.OrdinalIgnoreCase))
      SetStatusError(text);
    else
      SetStatusReady(text);
  }

  private static string GetAppVersion()
  {
    return Assembly
      .GetExecutingAssembly()
      .GetName()
      .Version?
      .ToString(3) ?? "1.0.3";
  }

  private sealed class LineStatusRenderer : ToolStripProfessionalRenderer
  {
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
      e.Graphics.Clear(Color.Transparent);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
      Rectangle r = e.ToolStrip.ClientRectangle;

      using Pen topLine = new(Color.FromArgb(65, 68, 74));
      using Pen bottomLine = new(Color.FromArgb(18, 19, 22));

      e.Graphics.DrawLine(topLine, 0, 0, r.Width, 0);
      e.Graphics.DrawLine(bottomLine, 0, r.Height - 1, r.Width, r.Height - 1);
    }
  }

  private sealed class DarkStatusRenderer : ToolStripProfessionalRenderer
  {
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
      e.Graphics.Clear(StatusDark);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
      using Pen pen = new(Color.FromArgb(42, 50, 66));
      e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width, 0);
    }
  }

  private sealed class BorderPanel : Panel
  {
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; init; } = Color.LightGray;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderWidth { get; init; } = 1;

    public BorderPanel()
    {
      DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      using Pen pen = new(BorderColor, BorderWidth);
      Rectangle rect = new(0, 0, Width - 1, Height - 1);
      e.Graphics.DrawRectangle(pen, rect);
    }
  }
}