using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Lab8LocalizedNotepad;

public sealed class EditorViewModel : INotifyPropertyChanged
{
    private readonly ResourceManager _resources = new("Lab8LocalizedNotepad.Resources.Strings", Assembly.GetExecutingAssembly());
    private CultureInfo _culture = new("ru");
    private string _documentText = string.Empty;
    private string _filePath = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _hasUnsavedChanges;
    private LanguageOption _selectedLanguage;

    public EditorViewModel()
    {
        Languages =
        [
            new LanguageOption("Русский", "ru"),
            new LanguageOption("English", "en")
        ];

        _selectedLanguage = Languages[0];
        NewCommand = new RelayCommand(_ => NewDocument());
        OpenCommand = new RelayCommand(_ => OpenDocument());
        SaveCommand = new RelayCommand(_ => SaveDocument());
        CloseCommand = new RelayCommand(_ => Application.Current.MainWindow?.Close());
        StatusMessage = T("StatusReady");
        UpdateStatistics();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LanguageOption> Languages { get; }

    public ICommand NewCommand { get; }

    public ICommand OpenCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand CloseCommand { get; }

    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetField(ref _selectedLanguage, value))
            {
                _culture = new CultureInfo(value.CultureName);
                RefreshLocalizedProperties();
            }
        }
    }

    public string DocumentText
    {
        get => _documentText;
        set
        {
            if (SetField(ref _documentText, value))
            {
                HasUnsavedChanges = true;
                UpdateStatistics();
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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public int LineCount { get; private set; } = 1;

    public int WordCount { get; private set; }

    public int CharacterCount { get; private set; }

    public string WindowTitle => $"{T("WindowTitle")} - {FileName}{(HasUnsavedChanges ? " *" : string.Empty)}";

    public string LabTitle => T("LabTitle");

    public string AppTitle => T("AppTitle");

    public string FileMenu => T("FileMenu");

    public string NewText => T("New");

    public string OpenText => T("Open");

    public string SaveText => T("Save");

    public string CloseText => T("Close");

    public string LanguageText => T("Language");

    public string UnsavedText => HasUnsavedChanges ? T("Unsaved") : T("Saved");

    public string FileName => string.IsNullOrWhiteSpace(FilePath) ? T("FileNotSaved") : Path.GetFileName(FilePath);

    public string FilePathText => string.IsNullOrWhiteSpace(FilePath) ? T("FileNotSaved") : FilePath;

    public string StatisticsText => string.Format(_culture, T("Stats"), LineCount, WordCount, CharacterCount);

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

        SetDocument(string.Empty, string.Empty, T("StatusNew"));
    }

    private void OpenDocument()
    {
        if (!AskToSaveIfNeeded())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SetDocument(File.ReadAllText(dialog.FileName, Encoding.UTF8), dialog.FileName, T("StatusOpened"));
        }
        catch (Exception exception)
        {
            ShowError(string.Format(_culture, T("OpenError"), exception.Message));
        }
    }

    private void SaveDocument()
    {
        var path = FilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "document.txt"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            path = dialog.FileName;
        }

        try
        {
            File.WriteAllText(path, DocumentText, Encoding.UTF8);
            FilePath = path;
            HasUnsavedChanges = false;
            StatusMessage = T("StatusSaved");
        }
        catch (Exception exception)
        {
            ShowError(string.Format(_culture, T("SaveError"), exception.Message));
        }
    }

    private bool AskToSaveIfNeeded()
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        var result = MessageBox.Show(T("ConfirmSave"), T("UnsavedDocument"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

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
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        CharacterCount = DocumentText.Length;
        WordCount = DocumentText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        LineCount = string.IsNullOrEmpty(DocumentText) ? 1 : DocumentText.Split('\n').Length;
        OnPropertyChanged(nameof(StatisticsText));
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        MessageBox.Show(message, T("ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private string T(string key)
    {
        return _resources.GetString(key, _culture) ?? key;
    }

    private void RefreshLocalizedProperties()
    {
        StatusMessage = T("StatusReady");
        foreach (var property in new[]
                 {
                     nameof(WindowTitle), nameof(LabTitle), nameof(AppTitle), nameof(FileMenu), nameof(NewText),
                     nameof(OpenText), nameof(SaveText), nameof(CloseText), nameof(LanguageText), nameof(UnsavedText),
                     nameof(FileName), nameof(FilePathText), nameof(StatisticsText)
                 })
        {
            OnPropertyChanged(property);
        }
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

public sealed record LanguageOption(string Name, string CultureName);
