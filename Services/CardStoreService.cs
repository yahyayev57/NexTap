using System.Text.Json;
using NexTap.Models;

namespace NexTap.Services;

/// <summary>
/// Stores cards as a single JSON file in the app's private data directory.
/// Simple on purpose: no SQLite/NuGet dependency, easy to inspect while
/// debugging, and a wallet with a handful of cards doesn't need a database.
/// Swap this out for a SQLite-backed implementation later if the wallet
/// grows a sync feature - it's all behind ICardStoreService already.
/// </summary>
public class CardStoreService : ICardStoreService
{
	private static readonly string FilePath =
		Path.Combine(FileSystem.AppDataDirectory, "nextap_cards.json");

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true
	};

	private readonly SemaphoreSlim _lock = new(1, 1);
	private List<NfcCardModel>? _cache;

	public async Task<List<NfcCardModel>> GetCardsAsync()
	{
		await EnsureLoadedAsync();
		// Newest additions last is confusing for a wallet - show most
		// recently used/added first.
		return _cache!
			.OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
			.ToList();
	}

	public async Task<NfcCardModel?> GetCardAsync(string id)
	{
		await EnsureLoadedAsync();
		return _cache!.FirstOrDefault(c => c.Id == id);
	}

	public async Task SaveCardAsync(NfcCardModel card)
	{
		await EnsureLoadedAsync();

		var existingIndex = _cache!.FindIndex(c => c.Id == card.Id);
		if (existingIndex >= 0)
			_cache[existingIndex] = card;
		else
			_cache.Add(card);

		await PersistAsync();
	}

	public async Task DeleteCardAsync(string id)
	{
		await EnsureLoadedAsync();
		_cache!.RemoveAll(c => c.Id == id);
		await PersistAsync();
	}

	public async Task TouchLastUsedAsync(string id)
	{
		await EnsureLoadedAsync();
		var card = _cache!.FirstOrDefault(c => c.Id == id);
		if (card is null)
			return;

		card.LastUsedAt = DateTime.UtcNow;
		await PersistAsync();
	}

	private async Task EnsureLoadedAsync()
	{
		if (_cache is not null)
			return;

		await _lock.WaitAsync();
		try
		{
			if (_cache is not null)
				return;

			if (!File.Exists(FilePath))
			{
				_cache = new List<NfcCardModel>();
				return;
			}

			var json = await File.ReadAllTextAsync(FilePath);
			_cache = string.IsNullOrWhiteSpace(json)
				? new List<NfcCardModel>()
				: JsonSerializer.Deserialize<List<NfcCardModel>>(json, JsonOptions) ?? new List<NfcCardModel>();
		}
		finally
		{
			_lock.Release();
		}
	}

	private async Task PersistAsync()
	{
		await _lock.WaitAsync();
		try
		{
			var json = JsonSerializer.Serialize(_cache, JsonOptions);
			await File.WriteAllTextAsync(FilePath, json);
		}
		finally
		{
			_lock.Release();
		}
	}
}
