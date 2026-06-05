using MCAddonCreator;
using System.Windows.Forms;

namespace MCAddonCreator;

internal static class Program
{
  [STAThread]
  private static void Main()
  {
    ApplicationConfiguration.Initialize();

    FontManager.Load();

    Application.Run(new MainForm());
  }
}