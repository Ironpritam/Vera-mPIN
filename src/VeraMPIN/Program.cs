using System.Windows.Forms;

namespace VeraMPIN;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            AppDataPaths.MigrateLegacyData();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Vera mPIN could not move its saved data to the new app-data folder.{Environment.NewLine}{exception.Message}",
                "Vera mPIN",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}