using MediatR;

namespace Tensorroot.Gov.BuildingBlocks.Application.Messaging;

/// <summary>Handler de um comando sem retorno.</summary>
/// <typeparam name="TCommand">Tipo do comando.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

/// <summary>Handler de um comando com retorno.</summary>
/// <typeparam name="TCommand">Tipo do comando.</typeparam>
/// <typeparam name="TResponse">Tipo do resultado.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}
