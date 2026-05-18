using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Features.ReviewLogs.Commands;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;
using HazelNet.Services.Optimizer;
using HazelNet.Service.Optimizer;

namespace HazelNet_Application.CQRS.Features.Optimizer.Commands;

public class OptimizeWeightsCommandHandler : ICommandHandler<OptimizeWeightsCommand>
{
    private readonly ICardRepository _cardRepository;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;
    private readonly IReviewLogRepository _reviewLogRepository;
    private readonly IFSRSParametersRepository _fsrsParametersRepository;

    public OptimizeWeightsCommandHandler(
        ICardRepository cardRepository,
        IReviewHistoryRepository reviewHistoryRepository,
        IReviewLogRepository reviewLogRepository,
        IFSRSParametersRepository fsrsParametersRepository)
    {
        _cardRepository = cardRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _reviewLogRepository = reviewLogRepository;
        _fsrsParametersRepository = fsrsParametersRepository;
    }

    public async Task Handle(OptimizeWeightsCommand command)
    {
        // Resolve card IDs — scoped to deck or all user decks
        var cardIds = command.DeckId.HasValue
            ? await _cardRepository.GetCardIdsByDeckIdAsync(command.DeckId.Value)
            : await _cardRepository.GetCardIdsByUserIdAsync(command.UserId);

        if (!cardIds.Any())
            throw new InvalidOperationException("No cards found to optimize against.");

        var optimizer = new FsrsOptimizationService(
            _reviewHistoryRepository,
            _reviewLogRepository.GetReviewLogsByHistoryIdsAsync);

        var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(
            cardIds,
            epochs: command.Epochs,
            batchSize: command.BatchSize,
            progress: command.Progress);

        var fsrsParameters = await _fsrsParametersRepository.GetFSRSParametersByUserIdAsync(command.UserId);

        if (fsrsParameters == null)
        {
            fsrsParameters = new FSRSParameters
            {
                UserId = command.UserId,
                W = result.Weights
            };
            await _fsrsParametersRepository.CreateAsync(fsrsParameters);
        }
        else
        {
            fsrsParameters.W = result.Weights;
            await _fsrsParametersRepository.UpdateAsync(fsrsParameters);
        }
    }
}