using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Areas.Identity.Models.AccountViewModels;

namespace PikaCore.Infrastructure.Security;

public interface IOidcService
{
   public Task<string> GetAccessToken(LoginViewModel loginViewModel);
   public Task<string> VerifyRemoteClientWithClientId(string clientId);
}