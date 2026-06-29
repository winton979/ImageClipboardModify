using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImageClipboardModify
{
    public class ClipboardWatcher : IDisposable
    {
        private readonly MainForm _form;
        private bool _disposed;

        public event Action<Image> ClipboardImageChanged;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        public ClipboardWatcher(MainForm form)
        {
            _form = form;
            _form.ClipboardUpdate += OnClipboardUpdate;
            AddClipboardFormatListener(_form.Handle);
        }

        private void OnClipboardUpdate()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                    return;

                using (var image = Clipboard.GetImage())
                {
                    if (image == null)
                        return;

                    ClipboardImageChanged?.Invoke(image);
                }
            }
            catch
            {
                // clipboard access can fail transiently
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            RemoveClipboardFormatListener(_form.Handle);
            _form.ClipboardUpdate -= OnClipboardUpdate;
        }
    }
}
