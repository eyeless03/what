using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace Lab4GuessNumber;

public sealed class GameViewModel : INotifyPropertyChanged
{
    private readonly Random _random = new();
    private readonly string _recordFilePath = Path.Combine(AppContext.BaseDirectory, "record.json");

    private DifficultyOption _selectedDifficulty;
    private int _secretNumber;
    private int _attempts;
    private int _bestAttempts;
    private int _lastDistance = -1;
    private string _currentGuessText = string.Empty;
    private string _message = string.Empty;
    private string _hint = string.Empty;
    private string _temperature = "Начните игру";
    private string _statusBrush = "#38BDF8";
    private bool _isGameWon;

    public GameViewModel()
    {
        Difficulties =
        [
            new DifficultyOption("Лёгкая", 1, 50, 12),
            new DifficultyOption("Стандарт", 1, 100, 10),
            new DifficultyOption("Эксперт", 1, 250, 9)
        ];

        _selectedDifficulty = Difficulties[1];

        GuessCommand = new RelayCommand(_ => MakeGuess(), _ => !IsGameWon);
        NewGameCommand = new RelayCommand(_ => StartNewGame());
        SelectDifficultyCommand = new RelayCommand(SelectDifficulty);

        LoadRecord();
        StartNewGame();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DifficultyOption> Difficulties { get; }

    public ObservableCollection<GuessItem> GuessHistory { get; } = new();

    public ICommand GuessCommand { get; }

    public ICommand NewGameCommand { get; }

    public ICommand SelectDifficultyCommand { get; }

    public DifficultyOption SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            if (SetField(ref _selectedDifficulty, value))
            {
                StartNewGame();
            }
        }
    }

    public string CurrentGuessText
    {
        get => _currentGuessText;
        set => SetField(ref _currentGuessText, value);
    }

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public string Hint
    {
        get => _hint;
        private set => SetField(ref _hint, value);
    }

    public string Temperature
    {
        get => _temperature;
        private set => SetField(ref _temperature, value);
    }

    public string StatusBrush
    {
        get => _statusBrush;
        private set => SetField(ref _statusBrush, value);
    }

    public int Attempts
    {
        get => _attempts;
        private set
        {
            if (SetField(ref _attempts, value))
            {
                OnPropertyChanged(nameof(AttemptsText));
            }
        }
    }

    public int BestAttempts
    {
        get => _bestAttempts;
        private set
        {
            if (SetField(ref _bestAttempts, value))
            {
                OnPropertyChanged(nameof(BestAttemptsText));
            }
        }
    }

    public bool IsGameWon
    {
        get => _isGameWon;
        private set
        {
            if (SetField(ref _isGameWon, value) && GuessCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public int MinNumber => SelectedDifficulty.Min;

    public int MaxNumber => SelectedDifficulty.Max;

    public int MaxAttempts => SelectedDifficulty.MaxAttempts;

    public string RangeText => $"Угадайте число от {MinNumber} до {MaxNumber}";

    public string AttemptsText => $"Попытки: {Attempts} из {MaxAttempts}";

    public string BestAttemptsText => BestAttempts > 0 ? $"Рекорд: {BestAttempts} попыток" : "Рекорд ещё не установлен";

    public double AttemptProgress => MaxAttempts == 0 ? 0 : Attempts * 100.0 / MaxAttempts;

    public void StartNewGame()
    {
        _secretNumber = _random.Next(MinNumber, MaxNumber + 1);
        _lastDistance = -1;
        Attempts = 0;
        CurrentGuessText = string.Empty;
        Message = "Введите число и нажмите «Проверить».";
        Hint = RangeText;
        Temperature = "Жду первую попытку";
        StatusBrush = "#38BDF8";
        IsGameWon = false;
        GuessHistory.Clear();
        NotifyGameMeta();
    }

    private void MakeGuess()
    {
        if (!int.TryParse(CurrentGuessText.Trim(), out var guess))
        {
            SetValidationMessage("Введите целое число.");
            return;
        }

        if (guess < MinNumber || guess > MaxNumber)
        {
            SetValidationMessage($"Число должно быть в диапазоне от {MinNumber} до {MaxNumber}.");
            return;
        }

        Attempts++;
        OnPropertyChanged(nameof(AttemptProgress));

        var distance = Math.Abs(_secretNumber - guess);
        var direction = guess > _secretNumber ? "слишком большое" : "слишком маленькое";

        if (guess == _secretNumber)
        {
            WinGame(guess);
            return;
        }

        var warmth = GetWarmth(distance);
        var trend = GetTrend(distance);
        Temperature = $"{warmth}. {trend}";
        Message = guess > _secretNumber ? "Загаданное число меньше." : "Загаданное число больше.";
        Hint = Attempts >= MaxAttempts
            ? $"Лимит попыток закончился. Было загадано число {_secretNumber}."
            : $"Попробуйте ещё раз: {direction}.";
        StatusBrush = Attempts >= MaxAttempts ? "#F87171" : "#FBBF24";

        GuessHistory.Insert(0, new GuessItem(Attempts, guess, $"{direction}, {warmth.ToLowerInvariant()}"));
        _lastDistance = distance;
        CurrentGuessText = string.Empty;

        if (Attempts >= MaxAttempts)
        {
            IsGameWon = true;
            Message = "Попытки закончились. Начните новую игру.";
        }
    }

    private void WinGame(int guess)
    {
        IsGameWon = true;
        Message = $"Поздравляем! Число {guess} угадано.";
        Hint = $"Вы справились за {Attempts} попыток.";
        Temperature = "Точно в цель";
        StatusBrush = "#35C48B";
        GuessHistory.Insert(0, new GuessItem(Attempts, guess, "угадано"));

        if (BestAttempts == 0 || Attempts < BestAttempts)
        {
            BestAttempts = Attempts;
            SaveRecord();
            Hint += " Это новый рекорд!";
        }
    }

    private void SelectDifficulty(object? parameter)
    {
        if (parameter is DifficultyOption difficulty)
        {
            SelectedDifficulty = difficulty;
        }
    }

    private void SetValidationMessage(string text)
    {
        Message = text;
        Hint = RangeText;
        Temperature = "Ошибка ввода";
        StatusBrush = "#F87171";
    }

    private string GetWarmth(int distance)
    {
        var range = MaxNumber - MinNumber;

        if (distance <= Math.Max(1, range / 100))
        {
            return "Очень горячо";
        }

        if (distance <= Math.Max(2, range / 20))
        {
            return "Горячо";
        }

        if (distance <= Math.Max(5, range / 10))
        {
            return "Тепло";
        }

        return "Холодно";
    }

    private string GetTrend(int distance)
    {
        if (_lastDistance < 0)
        {
            return "Смотрим, куда двигаться дальше";
        }

        if (distance < _lastDistance)
        {
            return "Вы ближе";
        }

        if (distance > _lastDistance)
        {
            return "Вы дальше";
        }

        return "Расстояние не изменилось";
    }

    private void LoadRecord()
    {
        if (!File.Exists(_recordFilePath))
        {
            return;
        }

        try
        {
            var record = JsonSerializer.Deserialize<GameRecord>(File.ReadAllText(_recordFilePath));
            BestAttempts = record?.BestAttempts ?? 0;
        }
        catch
        {
            BestAttempts = 0;
        }
    }

    private void SaveRecord()
    {
        var record = new GameRecord(BestAttempts);
        File.WriteAllText(_recordFilePath, JsonSerializer.Serialize(record));
    }

    private void NotifyGameMeta()
    {
        OnPropertyChanged(nameof(MinNumber));
        OnPropertyChanged(nameof(MaxNumber));
        OnPropertyChanged(nameof(MaxAttempts));
        OnPropertyChanged(nameof(RangeText));
        OnPropertyChanged(nameof(AttemptsText));
        OnPropertyChanged(nameof(AttemptProgress));
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

public sealed record DifficultyOption(string Name, int Min, int Max, int MaxAttempts)
{
    public string Description => $"{Min}-{Max}, {MaxAttempts} попыток";
}

public sealed record GuessItem(int Attempt, int Value, string Result)
{
    public string Text => $"#{Attempt}: {Value} — {Result}";
}

public sealed record GameRecord(int BestAttempts);
