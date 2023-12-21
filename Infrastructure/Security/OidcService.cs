using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JasperFx.Core;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
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
        var props = new Dictionary<string, OpenIddictParameter> { { "RegistrationId", new OpenIddictParameter("pika-core") } };

        var result = await _client.AuthenticateWithPasswordAsync(
            new OpenIddictClientModels.PasswordAuthenticationRequest()
            {
                Username = loginViewModel.Username,
                Password = loginViewModel.Password,
                Issuer = new Uri(_configuration.GetSection("Auth")["Authority"]),
                RegistrationId = "pika-core"
            });
        var token = result.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            throw new ApplicationException("Failed to retrieve token");
        }
        return token;
    }

    public async Task<string> VerifyRemoteClientWithClientId(string clientId)
    { 
        var result = await _client.AuthenticateWithClientCredentialsAsync(new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
        {
            RegistrationId = "noteapi-dev"
        });
        return result.AccessToken;
    }
}