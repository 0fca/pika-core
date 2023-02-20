using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Identity.Models.AccountViewModels;
using OpenIddict.Client;

namespace PikaCore.Infrastructure.Security;

public class OidcService : IOidcService
{
    private readonly OpenIddictClientService _client;
    private readonly IConfiguration _configuration;

    public OidcService(OpenIddictClientService client, IConfiguration configuration)
    {
        this._client = client;
        this._configuration = configuration;
    }
    public async Task<string> GetAccessToken(LoginViewModel loginViewModel)
    {
        var (response, info) = await _client.AuthenticateWithPasswordAsync(
            issuer: new Uri("http://192.168.1.252:5080/", UriKind.Absolute),
            username: loginViewModel.Username,
            password: loginViewModel.Password,
            scopes: new []{ "Base" }
            );
        var token = response.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            throw new ApplicationException("Failed to retrieve token");
        }
        return token;
    }
}