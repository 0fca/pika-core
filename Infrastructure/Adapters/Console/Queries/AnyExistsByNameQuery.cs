using MediatR;

namespace PikaCore.Infrastructure.Adapters.Console.Queries;

public class AnyExistsByNameQuery : IRequest<bool>
{
    private readonly string _name;

    public AnyExistsByNameQuery(string name)
    {
        this._name = name;
    }

    public string Name()
    {
        return this._name;
    }
}