using System.Threading.Tasks;

namespace PikaCore.Areas.Api.v1.Services;

public interface IAuthService
{
    Task<string?> Authenticate(string username, string password);

    Task<string?> SignOut();
}