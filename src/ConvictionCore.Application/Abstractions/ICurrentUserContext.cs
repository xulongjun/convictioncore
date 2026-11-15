namespace ConvictionCore.Application.Abstractions;

public interface ICurrentUserContext
{
    string? PrincipalId { get; }

    bool IsAuthenticated { get; }
}
