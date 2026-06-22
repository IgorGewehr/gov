using MediatR;

namespace Tensorroot.Gov.BuildingBlocks.Application.Messaging;

/// <summary>Consulta (read-only) que não altera o estado do sistema.</summary>
/// <typeparam name="TResponse">Tipo do resultado da consulta.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
