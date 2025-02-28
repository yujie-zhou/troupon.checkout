﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Infra.oAuthService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Troupon.Checkout.Api.Authentication
{
  public class GenericAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
  {
    public GenericAuthenticationHandler(
      IOptionsMonitor<TokenAuthenticationOptions> options,
      ILoggerFactory logger,
      UrlEncoder encoder,
      ISystemClock clock)
      : base(
        options,
        logger,
        encoder,
        clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      var headers = Request.Headers;
      var rawToken =
        headers.FirstOrDefault(
          x => x.Key == "Authorization"); //TODO: was OAuthSettings.HeaderName, to inject once tested
      var token = rawToken.Value;

      if (string.IsNullOrEmpty(token))
      {
        return Task.FromResult(AuthenticateResult.Fail("The provided token is null"));
      }

      try
      {
        var validationParameters = new TokenValidationParameters();
        validationParameters.ValidAudiences = this.Options.ValidAudiences;
        validationParameters.ValidIssuer = this.Options.ValidIssuer;

        var jwt = token.ToString()
          .Replace(
            "Bearer ",
            string.Empty);
        var securityToken = new JwtSecurityToken(jwt);

        //var handler = new JwtSecurityTokenHandler();

        if (securityToken.Issuer != validationParameters.ValidIssuer ||
            securityToken.Audiences.All(c => !validationParameters.ValidAudiences.Contains(c)))
        {
          return Task.FromResult(AuthenticateResult.Fail($"Could not validate the token : for token={securityToken}"));
        }

        var claims = new[]
        {
          new Claim(
            "token",
            token),
          new Claim(
            ClaimTypes.Role,
            "crm-api-backend"),
        };
        var identity = new ClaimsIdentity(claims);
        var ticket = new AuthenticationTicket(
          new ClaimsPrincipal(identity),
          new AuthenticationProperties(),
          this.Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
      }
      catch (Exception ex)
      {
        return Task.FromResult(AuthenticateResult.Fail($"Authentication Failed {ex.Message}"));
      }
    }
  }
}
