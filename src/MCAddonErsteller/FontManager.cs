using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MCAddonCreator;

public static partial class FontManager
{
  private static readonly PrivateFontCollection Fonts = new();
  private static readonly List<IntPtr> FontPointers = [];
  // Handles returned by AddFontMemResourceEx — keep so fonts remain available to GDI (TextRenderer)
  private static readonly List<IntPtr> FontHandles = [];

  [LibraryImport("gdi32.dll", SetLastError = true)]
  private static partial IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

  [LibraryImport("gdi32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool RemoveFontMemResourceEx(IntPtr handle);

  // Optional: free allocated memory and GDI resources when the process exits
  static FontManager()
  {
    AppDomain.CurrentDomain.ProcessExit += (_, _) =>
    {
      foreach (IntPtr h in FontHandles)
      {
        try { RemoveFontMemResourceEx(h); } catch { }
      }

      foreach (IntPtr p in FontPointers)
      {
        try { Marshal.FreeCoTaskMem(p); } catch { }
      }
    };
  }

  public static FontFamily Metropolis { get; private set; } = FontFamily.GenericSansSerif;
  public static FontFamily Noto { get; private set; } = FontFamily.GenericSansSerif;
  public static FontFamily Minecraft { get; private set; } = FontFamily.GenericSansSerif;

  public static void Load()
  {
    if (Fonts.Families.Length > 0)
      return;

    // Debug: list embedded resources so we can verify the font files are included
    try
    {
      var asm = Assembly.GetExecutingAssembly();
      System.Diagnostics.Debug.WriteLine("[FontManager] Embedded resources: " + string.Join(", ", asm.GetManifestResourceNames()));
    }
    catch { }

    LoadEmbeddedFont("metropolis-regular-webfont.ttf");
    LoadEmbeddedFont("noto-sans-v9-latin-regular.ttf");
    LoadEmbeddedFont("minecraft_five_bold-webfont.ttf");

    // Debug: list loaded font family names
    try
    {
      System.Diagnostics.Debug.WriteLine("[FontManager] Loaded font families: " + string.Join(", ", Fonts.Families.Select(f => f.Name)));
    }
    catch { }

    Metropolis = FindFont("Metropolis");
    Noto = FindFont("Noto Sans");
    Minecraft = FindFont("Minecraft Five", "Minecraft", "Minecrafter");
  }

  private static void LoadEmbeddedFont(string fileName)
  {
    Assembly asm = Assembly.GetExecutingAssembly();

    string resourceName = asm.GetManifestResourceNames()
        .FirstOrDefault(x => x.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
      ?? throw new FileNotFoundException($"Embedded font not found: {fileName}");

    using Stream stream = asm.GetManifestResourceStream(resourceName)
      ?? throw new FileNotFoundException($"Embedded font resource stream not found: {resourceName}");

    byte[] fontData = new byte[stream.Length];
    stream.ReadExactly(fontData);

    nint fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
    Marshal.Copy(fontData, 0, fontPtr, fontData.Length);

    Fonts.AddMemoryFont(fontPtr, fontData.Length);
    // Register font with GDI so TextRenderer (which uses GDI) can use the embedded font
    try
    {
      uint cFonts = 0;
      IntPtr handle = AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref cFonts);
      if (handle != IntPtr.Zero)
        FontHandles.Add(handle);
    }
    catch { }

    try
    {
      System.Diagnostics.Debug.WriteLine($"[FontManager] Loaded embedded font resource: {resourceName} ({fontData.Length} bytes)");
    }
    catch { }

    // Nicht freigeben, solange die App läuft.
    FontPointers.Add(fontPtr);
  }

  private static FontFamily FindFont(params string[] names)
  {
    foreach (string name in names)
    {
      FontFamily? family = Fonts.Families.FirstOrDefault(f =>
          f.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

      if (family is not null)
        return family;
    }

    return FontFamily.GenericSansSerif;
  }
}