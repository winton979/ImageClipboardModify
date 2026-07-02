using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ImageClipboardModify
{
    public class ClipboardWatcher : IDisposable
    {
        private readonly MainForm _form;
        private bool _disposed;
        private bool _registered;
        private IntPtr _registeredHandle = IntPtr.Zero;

        public event Action<Image> ClipboardImageChanged;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public ClipboardWatcher(MainForm form)
        {
            _form = form;
            _form.ClipboardUpdate += OnClipboardUpdate;
            _form.HandleCreated += OnHandleCreated;
            _form.HandleDestroyed += OnHandleDestroyed;
            TryRegister();
        }

        private void OnHandleCreated(object sender, EventArgs e) => TryRegister();
        private void OnHandleDestroyed(object sender, EventArgs e) => TryUnregister();

        private void TryRegister()
        {
            if (_disposed) return;
            if (_registered) return;
            if (!_form.IsHandleCreated) return;
            var h = _form.Handle;
            if (AddClipboardFormatListener(h))
            {
                _registered = true;
                _registeredHandle = h;
            }
            else
            {
                try { _form.SetStatus("Clipboard listener registration failed"); } catch { }
            }
        }

        private void TryUnregister()
        {
            if (!_registered) return;
            try { RemoveClipboardFormatListener(_registeredHandle); } catch { }
            _registered = false;
            _registeredHandle = IntPtr.Zero;
        }

        private void OnClipboardUpdate()
        {
            var image = TryReadClipboardImage();
            if (image == null) return;
            try
            {
                ClipboardImageChanged?.Invoke(image);
            }
            finally
            {
                image.Dispose();
            }
        }

        // 三级降级读取剪切板图片:
        //   1) PNG 私有格式 (Chrome/Figma/浏览器复制,保留 alpha)
        //   2) DIBV5 32bpp 手工解析 (保留 alpha)
        //   3) Clipboard.GetImage() 兜底
        // 遇到剪切板被其他进程占用 (ExternalException) 时重试
        public static Image TryReadClipboardImage()
        {
            const int maxAttempts = 4;
            const int delayMs = 80;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var img = TryReadPng();
                    if (img != null) return img;

                    img = TryReadDibV5();
                    if (img != null) return img;

                    if (Clipboard.ContainsImage())
                    {
                        var std = Clipboard.GetImage();
                        if (std != null) return std;
                    }

                    // 无图,不重试
                    return null;
                }
                catch (ExternalException)
                {
                    // 剪切板被其他进程占用,退避重试
                }
                catch
                {
                    return null;
                }

                Thread.Sleep(delayMs);
            }

            return null;
        }

        private static Image TryReadPng()
        {
            foreach (var name in new[] { "PNG", "image/png" })
            {
                if (!Clipboard.ContainsData(name)) continue;
                var data = Clipboard.GetData(name);
                var bytes = ToByteArray(data);
                if (bytes == null || bytes.Length == 0) continue;
                try
                {
                    // 复制到独立 MemoryStream,避免 Image.FromStream 后原流被释放
                    var ms = new MemoryStream(bytes);
                    return Image.FromStream(ms);
                }
                catch
                {
                    // 数据坏了,继续下一格式
                }
            }
            return null;
        }

        private static Image TryReadDibV5()
        {
            // CF_DIBV5 = 17
            var fmt = DataFormats.GetFormat(17);
            if (fmt == null) return null;
            if (!Clipboard.ContainsData(fmt.Name)) return null;
            var data = Clipboard.GetData(fmt.Name);
            var bytes = ToByteArray(data);
            if (bytes == null) return null;
            try
            {
                return ParseDibV5(bytes);
            }
            catch
            {
                return null;
            }
        }

        // 只处理 32bpp uncompressed / BI_BITFIELDS 的 DIB,其余交给上层 fallback
        private static Image ParseDibV5(byte[] data)
        {
            if (data.Length < 40) return null;

            int headerSize = BitConverter.ToInt32(data, 0);
            if (headerSize < 40 || headerSize > 200) return null;

            int width = BitConverter.ToInt32(data, 4);
            int height = BitConverter.ToInt32(data, 8);
            short bitCount = BitConverter.ToInt16(data, 14);
            int compression = BitConverter.ToInt32(data, 16);

            if (bitCount != 32) return null;
            // BI_RGB=0, BI_BITFIELDS=3
            if (compression != 0 && compression != 3) return null;
            if (width <= 0 || height == 0) return null;

            int absHeight = Math.Abs(height);
            bool topDown = height < 0;

            int pixelOffset = headerSize;
            // BITMAPINFOHEADER (40) + BI_BITFIELDS 后跟 3 个 DWORD 掩码;V4/V5 header 已含掩码
            if (headerSize == 40 && compression == 3)
                pixelOffset += 12;

            long need = (long)pixelOffset + (long)width * absHeight * 4;
            if (data.Length < need) return null;

            var bmp = new Bitmap(width, absHeight, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, absHeight);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = bmpData.Stride;
                for (int y = 0; y < absHeight; y++)
                {
                    int srcY = topDown ? y : (absHeight - 1 - y);
                    int srcOff = pixelOffset + srcY * width * 4;
                    var dstRowPtr = IntPtr.Add(bmpData.Scan0, y * stride);
                    Marshal.Copy(data, srcOff, dstRowPtr, width * 4);
                }
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
            return bmp;
        }

        private static byte[] ToByteArray(object data)
        {
            if (data is byte[] b) return b;
            if (data is MemoryStream ms) return ms.ToArray();
            if (data is Stream s)
            {
                using (var copy = new MemoryStream())
                {
                    s.CopyTo(copy);
                    return copy.ToArray();
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _form.HandleCreated -= OnHandleCreated;
            _form.HandleDestroyed -= OnHandleDestroyed;
            _form.ClipboardUpdate -= OnClipboardUpdate;
            TryUnregister();
        }
    }
}
