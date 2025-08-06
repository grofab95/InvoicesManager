namespace IM.Core.Interfaces;

public interface IInit
{
    Task Init(CancellationToken cancellationToken);
}