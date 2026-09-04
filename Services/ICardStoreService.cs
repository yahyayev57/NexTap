using NexTap.Models;

namespace NexTap.Services;

public interface ICardStoreService
{
	Task<List<NfcCardModel>> GetCardsAsync();

	Task<NfcCardModel?> GetCardAsync(string id);

	Task SaveCardAsync(NfcCardModel card);

	Task DeleteCardAsync(string id);

	Task TouchLastUsedAsync(string id);
}
