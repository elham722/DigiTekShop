﻿namespace DigiTekShop.Contracts.Abstractions.CQRS.Commands;
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommand : IRequest<Result> { }