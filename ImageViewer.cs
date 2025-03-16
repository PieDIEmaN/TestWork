using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;



using System.Windows.Controls;
using System.IO;

namespace TestUnit
{
    public class ImageViewer
    {
        private readonly Image imageControl;
        private const string LastPathKey = "LastImagePath";

        public string LastPath { get; private set; }

        public ImageViewer(Image imageControl)
        {
            if (imageControl == null)
                throw new ArgumentNullException(nameof(imageControl));

            this.imageControl = imageControl;
            LoadLastImage();
        }

        public void OpenImage()
        {
            string selectedPath = SelectImage();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                LoadImage(selectedPath);
                SaveLastPath(selectedPath);
            }
        }

        private string SelectImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp",
                Title = "Выбор изображения"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        private void LoadImage(string imagePath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imageControl.Source = bitmap;
                LastPath = imagePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображеняи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void DeleteImage()
        {
            try
            {
                imageControl.Source = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка улаления изображения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveLastPath(string path)
        {
            Properties.Settings.Default[LastPathKey] = path;
            Properties.Settings.Default.Save();
        }

        public void LoadLastImage()
        {
            string savedPath = Properties.Settings.Default[LastPathKey] as string;

            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                LoadImage(savedPath);
            }
        }

        public async Task ApplyGaussianBlur(int radius, CancellationToken token)
        {
            if (imageControl.Source == null) return;

            try
            {
                BitmapSource bitmapSource = (BitmapSource)imageControl.Source;

                int width = bitmapSource.PixelWidth;
                int height = bitmapSource.PixelHeight;
                int stride = width * 4;
                byte[] pixelData = new byte[height * stride];

                bitmapSource.CopyPixels(pixelData, stride, 0);

                byte[] processedPixels = await Task.Run(() =>
                {
                    if (token.IsCancellationRequested) return pixelData;

                    GaussianBlur(pixelData, width, height, stride, radius, token);
                    return pixelData;
                }, token);

                if (!token.IsCancellationRequested)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        WriteableBitmap processedBitmap = new WriteableBitmap(width, height, bitmapSource.DpiX, bitmapSource.DpiY, bitmapSource.Format, null);
                        processedBitmap.WritePixels(new Int32Rect(0, 0, width, height), processedPixels, stride, 0);
                        imageControl.Source = processedBitmap;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Процесс обработки завершен досрочно!", "Отмена", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка Блюра: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void GaussianBlur(byte[] pixels, int width, int height, int stride, int radius, CancellationToken token)
        {
            byte[] tempPixels = new byte[pixels.Length];

            int blurSize = radius * 2 + 1;

            for (int y = 0; y < height; y++)
            {
                if (token.IsCancellationRequested) return;

                for (int x = radius; x < width - radius; x++)
                {
                    int index = (y * width + x) * 4;
                    int sumR = 0, sumG = 0, sumB = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        int sampleIndex = index + k * 4;
                        sumB += pixels[sampleIndex];
                        sumG += pixels[sampleIndex + 1];
                        sumR += pixels[sampleIndex + 2];
                    }

                    tempPixels[index] = (byte)(sumB / blurSize);
                    tempPixels[index + 1] = (byte)(sumG / blurSize);
                    tempPixels[index + 2] = (byte)(sumR / blurSize);
                }
            }

            for (int y = radius; y < height - radius; y++)
            {
                if (token.IsCancellationRequested) return;

                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * 4;
                    int sumR = 0, sumG = 0, sumB = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        int sampleIndex = index + k * stride;
                        sumB += tempPixels[sampleIndex];
                        sumG += tempPixels[sampleIndex + 1];
                        sumR += tempPixels[sampleIndex + 2];
                    }

                    pixels[index] = (byte)(sumB / blurSize);
                    pixels[index + 1] = (byte)(sumG / blurSize);
                    pixels[index + 2] = (byte)(sumR / blurSize);
                }
            }
        }



    }
}
