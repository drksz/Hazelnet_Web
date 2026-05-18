using HazelNet_Application.CQRS.Abstractions;
using HazelNet.Services.Optimizer;

namespace HazelNet_Application.CQRS.Features.ReviewLogs.Commands;

public record OptimizeWeightsCommand 
(
    int UserId,
    int? DeckId = null,
    int Epochs = 5,
    int BatchSize = 4096,
    IProgress<OptimizerProgress>? Progress = null) : ICommand;