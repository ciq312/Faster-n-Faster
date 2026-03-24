namespace FasterNFaster.Api.UseCases.Helpers;

public interface IHandler<TCommand, TResult>
{
    Task<TResult> Handle(TCommand command);
}

public interface IHandler<TCommand>
{
    Task Handle(TCommand command);
}
