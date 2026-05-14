using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Lab5Notepad;

public sealed class EditorViewModel : INotifyPropertyChanged
{
    private string _documentText = string.Empty;
    private string _filePath = string.Empty;
    private string _statusMessage = "Создан новый документ.";
    private bool _hasUnsavedChanges;
    private bool _isWrappingEnabled = true;

    public EditorViewModel()
    {
        NewCommand = new RelayCommand(_ => NewDocument());
        OpenCommand = new RelayCommand(_ => OpenDocument());
        SaveCommand = new RelayCommand(_ => SaveDocument());
        SaveAsCommand = new RelayCommand(_ => SaveDocumentAs());
        ToggleWrapCommand = new RelayCommand(_ => IsWrappingEnabled = !IsWrappingEnabled);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand NewCommand { get; }

    public ICommand OpenCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand SaveAsCommand { get; }

    public ICommand ToggleWrapCommand { get; }

    public string DocumentText
    {
        get => _documentText;
        set
        {
            if (SetField(ref _documentText, value))
            {
                HasUnsavedChanges = true;
                UpdateTextStatistics();
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        private set
        {
            if (SetField(ref _filePath, value))
            {
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(FilePathText));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string FileName => string.IsNullOrWhiteSpace(FilePath) ? "Новый документ" : Path.GetFileName(FilePath);

    public string FilePathText => string.IsNullOrWhiteSpace(FilePath) ? "Файл ещё не сохранён" : FilePath;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetField(ref _hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(UnsavedText));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public bool IsWrappingEnabled
    {
        get => _isWrappingEnabled;
        set
        {
            if (SetField(ref _isWrappingEnabled, value))
            {
                OnPropertyChanged(nameof(WrapModeText));
            }
        }
    }

    public string UnsavedText => HasUnsavedChanges ? "Есть несохранённые изменения" : "Все изменения сохранены";

    public string WindowTitle => $"{FileName}{(HasUnsavedChanges ? " *" : string.Empty)} - Блокнот";

    public string WrapModeText => IsWrappingEnabled ? "Перенос строк включён" : "Перенос строк выключен";

    public int CharacterCount { get; private set; }

    public int WordCount { get; private set; }

    public int LineCount { get; private set; } = 1;

    public string StatisticsText => $"{LineCount} строк | {WordCount} слов | {CharacterCount} символов";

    public bool ConfirmClose()
    {
        return AskToSaveIfNeeded();
    }

    private void NewDocument()
    {
        if (!AskToSaveIfNeeded())
        {
            return;
        }

        SetDocument(string.Empty, string.Empty, "Создан новый документ.");
    }

    private void OpenDocument()
    {
        if (!AskToSaveIfNeeded())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Открыть текстовый файл",
            Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            SetDocument(text, dialog.FileName, "Файл открыт.");
        }
        catch (Exception exception)
        {
            ShowError($"Не удалось открыть файл: {exception.Message}");
        }
    }

    private void SaveDocument()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            SaveDocumentAs();
            return;
        }

        SaveToPath(FilePath);
    }

    private void SaveDocumentAs()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить документ",
            Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
            FileName = FileName == "Новый документ" ? "document.txt" : FileName
        };

        if (dialog.ShowDialog() == true)
        {
            SaveToPath(dialog.FileName);
        }
    }

    private void SaveToPath(string path)
    {
        try
        {
            File.WriteAllText(path, DocumentText, Encoding.UTF8);
            FilePath = path;
            HasUnsavedChanges = false;
            StatusMessage = "Документ сохранён.";
        }
        catch (Exception exception)
        {
            ShowError($"Не удалось сохранить файл: {exception.Message}");
        }
    }

    private bool AskToSaveIfNeeded()
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        var result = MessageBox.Show(
            "Сохранить изменения перед продолжением?",
            "Несохранённый документ",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.Yes)
        {
            SaveDocument();
            return !HasUnsavedChanges;
        }

        return true;
    }

    private void SetDocument(string text, string path, string status)
    {
        _documentText = text;
        OnPropertyChanged(nameof(DocumentText));
        FilePath = path;
        HasUnsavedChanges = false;
        StatusMessage = status;
        UpdateTextStatistics();
    }

    private void UpdateTextStatistics()
    {
        CharacterCount = DocumentText.Length;
        WordCount = DocumentText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        LineCount = string.IsNullOrEmpty(DocumentText) ? 1 : DocumentText.Split('\n').Length;

        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(WordCount));
        OnPropertyChanged(nameof(LineCount));
        OnPropertyChanged(nameof(StatisticsText));
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
