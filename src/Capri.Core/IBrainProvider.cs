namespace Capri.Core;

public interface IBrainProvider
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<string> ReplyAsync(IReadOnlyList<ChatTurn> turns, CancellationToken cancellationToken = default);
}
