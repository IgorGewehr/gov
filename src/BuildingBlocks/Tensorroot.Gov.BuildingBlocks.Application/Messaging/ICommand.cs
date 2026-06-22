using MediatR;

namespace Tensorroot.Gov.BuildingBlocks.Application.Messaging;

/// <summary>Marcador comum a todos os comandos (usado por behaviors transacionais).</summary>
public interface IBaseCommand
{
}

/// <summary>Comando que altera o estado do sistema e não retorna valor.</summary>
public interface ICommand : IRequest, IBaseCommand
{
}

/// <summary>Comando que altera o estado do sistema e retorna um resultado.</summary>
/// <typeparam name="TResponse">Tipo do resultado.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand
{
}
