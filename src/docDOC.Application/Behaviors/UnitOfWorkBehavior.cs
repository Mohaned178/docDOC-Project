using docDOC.Domain.Interfaces;
using MediatR;

namespace docDOC.Application.Behaviors;

public class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!typeof(TRequest).Name.EndsWith("Command"))
        {
            return await next();
        }

        var response = await next();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
