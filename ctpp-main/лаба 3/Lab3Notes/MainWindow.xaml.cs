using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lab3Notes;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Note> _notes = new();
    private readonly ObservableCollection<NoteListItem> _filteredNotes = new();
    private readonly string _notesFilePath = Path.Combine(AppContext.BaseDirectory, "notes.txt");

    private int _selectedNoteIndex = -1;
    private bool _isLoadingEditor;

    public MainWindow()
    {
        InitializeComponent();
        NotesListBox.ItemsSource = _filteredNotes;
        LoadNotes();
        RefreshFilteredNotes();
        UpdateEditorStats();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var noteText = NoteEditorTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(noteText))
        {
            SetStatus("Нельзя добавить пустую заметку.", Brushes.Firebrick);
            return;
        }

        var note = new Note
        {
            Text = noteText,
            IsPinned = PinCheckBox.IsChecked == true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _notes.Add(note);
        SaveNotes();
        SelectNote(_notes.Count - 1);
        SearchTextBox.Clear();
        RefreshFilteredNotes();
        SetStatus("Заметка добавлена и сохранена.", Brushes.LightGreen);
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        SaveCurrentNote();
    }

    private void NewButton_OnClick(object sender, RoutedEventArgs e)
    {
        NotesListBox.SelectedItem = null;
        _selectedNoteIndex = -1;
        _isLoadingEditor = true;
        NoteEditorTextBox.Clear();
        PinCheckBox.IsChecked = false;
        _isLoadingEditor = false;
        UpdateEditorStats();
        SetStatus("Новая заметка готова к вводу.", Brushes.LightSkyBlue);
    }

    private void DuplicateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNoteIndex < 0 || _selectedNoteIndex >= _notes.Count)
        {
            SetStatus("Выберите заметку, которую нужно продублировать.", Brushes.Firebrick);
            return;
        }

        var source = _notes[_selectedNoteIndex];
        var copy = new Note
        {
            Text = source.Text,
            IsPinned = source.IsPinned,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _notes.Add(copy);
        SaveNotes();
        SelectNote(_notes.Count - 1);
        RefreshFilteredNotes();
        SetStatus("Создана копия заметки.", Brushes.LightGreen);
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNoteIndex < 0 || _selectedNoteIndex >= _notes.Count)
        {
            SetStatus("Сначала выберите заметку для удаления.", Brushes.Firebrick);
            return;
        }

        _notes.RemoveAt(_selectedNoteIndex);
        SaveNotes();
        NewButton_OnClick(sender, e);
        RefreshFilteredNotes();
        SetStatus("Заметка удалена.", Brushes.LightGreen);
    }

    private void NotesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesListBox.SelectedItem is not NoteListItem selectedNote)
        {
            return;
        }

        SelectNote(selectedNote.Index);
        SetStatus("Заметка открыта для редактирования.", Brushes.LightSkyBlue);
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshFilteredNotes();
    }

    private void NoteEditorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateEditorStats();

        if (!_isLoadingEditor && _selectedNoteIndex >= 0)
        {
            SaveIndicatorTextBlock.Text = "Есть несохранённые изменения";
            SaveIndicatorTextBlock.Foreground = Brushes.Khaki;
        }
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            SaveCurrentNote();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            NewButton_OnClick(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && NoteEditorTextBox.IsKeyboardFocusWithin == false)
        {
            DeleteButton_OnClick(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SearchTextBox.Clear();
            e.Handled = true;
        }
    }

    private void SetWhiteBackground_OnClick(object sender, RoutedEventArgs e)
    {
        NoteEditorTextBox.Background = Brushes.White;
    }

    private void SetYellowBackground_OnClick(object sender, RoutedEventArgs e)
    {
        NoteEditorTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 247, 214));
    }

    private void SetBlueBackground_OnClick(object sender, RoutedEventArgs e)
    {
        NoteEditorTextBox.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254));
    }

    private void SetGreenBackground_OnClick(object sender, RoutedEventArgs e)
    {
        NoteEditorTextBox.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
    }

    private void Window_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_selectedNoteIndex >= 0)
        {
            SaveCurrentNote(showStatus: false);
        }

        SaveNotes();
    }

    private void SaveCurrentNote(bool showStatus = true)
    {
        if (_selectedNoteIndex < 0 || _selectedNoteIndex >= _notes.Count)
        {
            SetStatus("Для сохранения новой заметки нажмите «Добавить».", Brushes.Firebrick);
            return;
        }

        var noteText = NoteEditorTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(noteText))
        {
            SetStatus("Заметка не может быть пустой.", Brushes.Firebrick);
            return;
        }

        var note = _notes[_selectedNoteIndex];
        note.Text = noteText;
        note.IsPinned = PinCheckBox.IsChecked == true;
        note.UpdatedAt = DateTime.Now;

        SaveNotes();
        RefreshFilteredNotes();

        if (showStatus)
        {
            SetStatus("Заметка сохранена.", Brushes.LightGreen);
        }
    }

    private void SelectNote(int index)
    {
        if (index < 0 || index >= _notes.Count)
        {
            return;
        }

        _selectedNoteIndex = index;
        var note = _notes[index];

        _isLoadingEditor = true;
        NoteEditorTextBox.Text = note.Text;
        PinCheckBox.IsChecked = note.IsPinned;
        _isLoadingEditor = false;

        UpdateEditorStats();
    }

    private void RefreshFilteredNotes()
    {
        var selectedIndex = _selectedNoteIndex;
        var searchText = SearchTextBox.Text.Trim();

        _filteredNotes.Clear();

        var matches = _notes
            .Select((note, index) => new { note, index })
            .Where(item => string.IsNullOrWhiteSpace(searchText) ||
                           item.note.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.note.IsPinned)
            .ThenByDescending(item => item.note.UpdatedAt);

        foreach (var item in matches)
        {
            _filteredNotes.Add(NoteListItem.FromNote(item.note, item.index));
        }

        NotesListBox.SelectedItem = _filteredNotes.FirstOrDefault(item => item.Index == selectedIndex);
        SaveIndicatorTextBlock.Text = $"Сохранено заметок: {_notes.Count}";
        SaveIndicatorTextBlock.Foreground = Brushes.LightGreen;
    }

    private void UpdateEditorStats()
    {
        var text = NoteEditorTextBox.Text;
        var words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        StatsTextBlock.Text = $"{words} слов / {text.Length} символов";
    }

    private void LoadNotes()
    {
        if (!File.Exists(_notesFilePath))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(_notesFilePath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                _notes.Add(DecodeNote(line));
            }
            catch
            {
                _notes.Add(Note.FromText(line));
            }
        }
    }

    private void SaveNotes()
    {
        var lines = _notes
            .Select(EncodeNote)
            .ToArray();

        File.WriteAllLines(_notesFilePath, lines, Encoding.UTF8);
    }

    private static Note DecodeNote(string line)
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(line));

        if (decoded.TrimStart().StartsWith("{"))
        {
            return JsonSerializer.Deserialize<Note>(decoded) ?? Note.FromText(string.Empty);
        }

        return Note.FromText(decoded);
    }

    private static string EncodeNote(Note note)
    {
        var json = JsonSerializer.Serialize(note);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private void SetStatus(string message, Brush color)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = color;
    }
}

public sealed class Note
{
    public string Text { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public static Note FromText(string text)
    {
        return new Note
        {
            Text = text,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}

public sealed class NoteListItem
{
    public int Index { get; init; }

    public string Preview { get; init; } = string.Empty;

    public string Meta { get; init; } = string.Empty;

    public string PinMarker { get; init; } = string.Empty;

    public static NoteListItem FromNote(Note note, int index)
    {
        var oneLineText = note.Text.Replace(Environment.NewLine, " ");
        var preview = oneLineText.Length <= 54 ? oneLineText : $"{oneLineText[..54]}...";

        return new NoteListItem
        {
            Index = index,
            Preview = string.IsNullOrWhiteSpace(preview) ? "(без текста)" : preview,
            Meta = $"изменено {note.UpdatedAt:dd.MM.yyyy HH:mm}",
            PinMarker = note.IsPinned ? "★" : string.Empty
        };
    }
}
