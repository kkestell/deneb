using Deneb.Commands;
using Deneb.Ebooks;
using Deneb.Models;
using Timer = System.Timers.Timer;

namespace Deneb.Services;

public class LibGenDownloader
{
    private const string SpinnerFrames = "⠁⠂⠄⡀⡈⡐⡠⣀⣁⣂⣄⣌⣔⣤⣥⣦⣮⣶⣷⣿⡿⠿⢟⠟⡛⠛⠫⢋⠋⠍⡉⠉⠑⠡⢁";
    private static int _spinnerFrameIndex;
    private static DateTime _lastProgressUpdate = DateTime.MinValue;
    private int _progress;
    private Book _book;
    private Timer _progressTimer;

    private readonly LibGenClient _client = new();

    public LibGenDownloader(Book book)
    {
        _book = book;
        _progressTimer = new Timer(100);
        _progressTimer.Elapsed += (sender, e) => UpdateProgress();
    }
    
    public async Task<FileInfo?> Download()
    {
        var sources = _book.Type switch
        {
            BookType.Fiction => await _client.SearchFiction(_book.Title, _book.Author.Name),
            BookType.NonFiction => await _client.SearchNonFiction(_book.Title, _book.Author.Name),
            _ => throw new NotImplementedException()
        };
        
        if (sources.Count == 0)
        {
            Log.Error($"No sources found for {_book.Author.Name} - {_book.Title}");
            _progressTimer.Stop();
            return null;
        }

        foreach (var source in sources)
        {
            _progressTimer.Start();

            try
            {
                var fileInfo = await _client.DownloadResult(source, progress => _progress = (int)progress);

                if (fileInfo is null)
                {
                    Log.Error("Failed to download book");
                    _progressTimer.Stop();
                    return null;
                }
            
                _progressTimer.Stop();

                _progress = 100;
                UpdateProgress();
        
                Console.WriteLine();
        
                return fileInfo;
            }
            catch
            {
                _progressTimer.Stop();
                Console.WriteLine();
            }
        }
        
        Log.Information($"No sources found for {_book.Author.Name} - {_book.Title}");
        _progressTimer.Stop();
        return null;
    }
    
    private void UpdateProgress()
    {
        var spinnerFrame = SpinnerFrames[_spinnerFrameIndex];
        if (DateTime.Now - _lastProgressUpdate >= TimeSpan.FromMilliseconds(80))
        {
            _spinnerFrameIndex = (_spinnerFrameIndex + 1) % SpinnerFrames.Length;
            _lastProgressUpdate = DateTime.Now;
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"{spinnerFrame} ");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{_book.Author.Name} - {_book.Title} {_progress}%");

        Console.CursorLeft = 0;
        Console.ResetColor();
    }
}
