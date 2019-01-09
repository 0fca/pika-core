using Microsoft.AspNetCore.Authentication;
using Google.Apis.Auth.OAuth2;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GoogleSingInExtension
    {
        public static AuthenticationBuilder AddGoogleSingIn(this AuthenticationBuilder builder) {
            
            return builder;
        }
    }
}
