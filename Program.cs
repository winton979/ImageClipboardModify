using System;
using System.Threading;
using System.Windows.Forms;

namespace ImageClipboardModify;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        const string mutexName = "Global\\ImageClipboardModify_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("ImageClipboardModify is already running.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        using var context = new MainApplicationContext();
        Application.Run(context);
    }
}
