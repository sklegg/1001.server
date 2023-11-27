namespace Server1001.Services;

using Server1001.Models;

public interface ISongService
{
    Task<Song?> GetSongByIdAsync(Guid songId);
    Task<int> GetSongCountAsync();
    Task<List<Song>> GetAllSongsAsync();
    Task<List<Guid>> GetAllSongIdsAsync();
}

public class SongService : ISongService
{
    private IDynamoRepository _repository;

    public SongService (IDynamoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Song?> GetSongByIdAsync(Guid songId)
    {
        return await _repository.GetSongByIdAsync(songId);
    }

    public async Task<List<Song>> GetAllSongsAsync()
    {
        Dictionary<Guid, Song> songsDictionary = await _repository.GetSongsAsync();
        var result = new List<Song>();
        foreach (var song in songsDictionary.Values)
        {
            result.Add(song);
        }

        return result;
    }

    public async Task<List<Guid>> GetAllSongIdsAsync()
    {
        var songs = await _repository.GetSongsAsync();
        var keys = songs.Keys;
        return songs.Keys.ToList<Guid>();
    }

    public async Task<int> GetSongCountAsync()
    {
        return await _repository.GetSongDbCountAsync();
    }
}