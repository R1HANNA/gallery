using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace Gallery;

public partial class MainWindow : Window
{
    private readonly List<ImageItem> _images = new();
    private int _currentIndex = -1;
    private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        _images.Clear();
        _currentIndex = -1;

        var raw = PathsTextBox.Text ?? string.Empty;
        var folders = raw.Split('@', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .Where(p => p.Length > 0)
                         .ToList();

        var errors = new List<string>();

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                errors.Add($"Папку не знайдено: {folder}");
                continue;
            }
            try
            {
                var files = Directory.EnumerateFiles(folder)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

                foreach (var f in files)
                {
                    try { _images.Add(new ImageItem(f)); }
                    catch (Exception ex) { errors.Add($"Помилка файлу {f}: {ex.Message}"); }
                }
            }
            catch (Exception ex) { errors.Add($"Помилка доступу до {folder}: {ex.Message}"); }
        }

        if (_images.Count == 0)
        {
            ImageView.Source = null;
            WelcomePanel.IsVisible = true;
            CounterTextBlock.Text = "0 / 0";
            InfoTextBlock.Text = errors.Count > 0 ? string.Join(Environment.NewLine, errors) : "Зображень не знайдено.";
            return;
        }

        WelcomePanel.IsVisible = false;
        _currentIndex = 0;
        ShowCurrent();

        if (errors.Count > 0)
            InfoTextBlock.Text += Environment.NewLine + "Попередження:" + Environment.NewLine + string.Join(Environment.NewLine, errors);
    }

    private void OnPreviousClick(object? sender, RoutedEventArgs e)
    {
        if (_images.Count == 0) return;
        _currentIndex = (_currentIndex - 1 + _images.Count) % _images.Count;
        ShowCurrent();
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (_images.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % _images.Count;
        ShowCurrent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void ShowCurrent()
    {
        if (_currentIndex < 0 || _currentIndex >= _images.Count) return;

        var item = _images[_currentIndex];
        using var fs = File.OpenRead(item.Path);
        ImageView.Source = new Bitmap(fs);

        CounterTextBlock.Text = $"{_currentIndex + 1} / {_images.Count}";

        InfoTextBlock.Text = 
            $"Ім'я файлу: {item.FileName}{Environment.NewLine}" +
            $"Дата створення: {item.CreationDate:dd.MM.yyyy HH:mm}{Environment.NewLine}" +
            $"Розмір: {item.FormatSize(item.SizeBytes)}{Environment.NewLine}" +
            $"Роздільна здатність: {item.Width} x {item.Height}{Environment.NewLine}" +
            $"Формат: {item.Format}";
    }
}

public sealed class ImageItem
{
    public string Path { get; }
    public string FileName { get; }
    public DateTime CreationDate { get; }
    public long SizeBytes { get; }
    public int Width { get; }
    public int Height { get; }
    public string Format { get; }

    public ImageItem(string path)
    {
        Path = path;
        FileName = System.IO.Path.GetFileName(path);
        var fi = new FileInfo(path);
        CreationDate = fi.CreationTime;
        SizeBytes = fi.Length;

        using var fs = File.OpenRead(path);
        var bmp = new Bitmap(fs);
        Width = bmp.PixelSize.Width;
        Height = bmp.PixelSize.Height;
        Format = System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
    }

    public string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}