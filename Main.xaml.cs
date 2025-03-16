using Microsoft.Win32;
using System.IO;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TestUnit;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private ImageViewer imageViewer;

    public MainWindow()
    {
        InitializeComponent();
        LoadLastFilterRadius();

        imageViewer = new ImageViewer(FilteredImage);
        imageViewer.LoadLastImage();
    }

    private void ButtonSaveMenu_click(object sender, RoutedEventArgs e)
    {
        SaveImage(NoFilteredImage);
        imageViewer = new ImageViewer(NoFilteredImage);
        imageViewer.DeleteImage();
    }


    private void SaveImage(Image imageControl)
    {
        if (imageControl.Source == null)
        {
            MessageBox.Show("Нет изображения для сохранения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "Изображение (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp",
            Title = "Сохранения изображения"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string filePath = saveFileDialog.FileName;

                BitmapSource? bitmapSource = imageControl.Source as BitmapSource;
                if (bitmapSource == null)
                    throw new Exception("Ошибка конвертации изображения!"); 

                BitmapEncoder encoder;
                if (filePath.EndsWith(".png"))
                    encoder = new PngBitmapEncoder();
                else if (filePath.EndsWith(".jpg") || filePath.EndsWith(".jpeg"))
                    encoder = new JpegBitmapEncoder();
                else if (filePath.EndsWith(".bmp"))
                    encoder = new BmpBitmapEncoder();
                else
                    throw new Exception("Неподерживаейммый формат!"); ;

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show("Сохранения изображения прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошбика сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ButtonOpenMenu_click(object sender, RoutedEventArgs e)
    {
        imageViewer = new ImageViewer(FilteredImage);

        ButtonSaveMenu.IsEnabled = false;

        imageViewer.OpenImage();

    }

    private async void ButtonTakeFilter_Click(object sender, RoutedEventArgs e)
    {
        imageViewer = new ImageViewer(NoFilteredImage);
        ButtonTakeFilter.IsEnabled = false;
        ProgressDialog progressDialog = new ProgressDialog();
        progressDialog.Owner = this;

        try
        {
            ButtonTakeFilter.IsEnabled = false;
            ButtonSaveMenu.IsEnabled = false;
            progressDialog.Show();

            int radius = (int)SliderFilter.Value;
            SaveLastRadius(radius);


            if (!progressDialog.Token.IsCancellationRequested)
            {
                await imageViewer.ApplyGaussianBlur(radius, progressDialog.Token);
            }

        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Операция была отменена.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            progressDialog.Close();
            ButtonTakeFilter.IsEnabled = true;
            ButtonSaveMenu.IsEnabled = true;
        }
    }

    private void SliderFilter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Properties.Settings.Default["LastFilterRadius"] = SliderFilter.Value;
        Properties.Settings.Default.Save();
    }

    private void SaveLastRadius(int radius)
    {
        Properties.Settings.Default.LastFilterRadius = radius;
        Properties.Settings.Default.Save();
    }

    private void LoadLastFilterRadius()
    {
        if (Properties.Settings.Default["LastFilterRadius"] != null)
        {
            SliderFilter.Value = (int)Properties.Settings.Default["LastFilterRadius"];
        }
    }

    private void ButtonExitMenu_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void ButtonAboutMenu_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Программа для обработки изображения\nВерсия: 1.0\nРазработчик: Pieman",
            "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}




