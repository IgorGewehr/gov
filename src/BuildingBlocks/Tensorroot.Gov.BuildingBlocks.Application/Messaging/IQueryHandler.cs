using MediatR;

namespace Tensorroot.Gov.BuildingBlocks.Application.Messaging;

/// <summary>Handler de uma consulta.</summary>
/// <typeparam name="TQuery">Tipo da consulta.</typeparam>
/// <typeparam name="TResponse">Tipo do resultado.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
