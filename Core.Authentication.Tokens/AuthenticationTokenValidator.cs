using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Core.Authentication.Tokens
{
    public class AuthenticationTokenValidator : ITokenValidator
    {
        public bool ValidateToken(string token, out User user)
        {
            user = null;
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                return false;
            }

            var tokenValidationParameter = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidAudience = "https://github.com/vigneshmsft/Core",
                ValidIssuer = "https://github.com/vigneshmsft/Core",
                ValidateActor = false,
                ValidateLifetime = false,
                IssuerSigningKey = Encryption.PrivateKey
            };

            try
            {
                tokenHandler.ValidateToken(token, tokenValidationParameter, out SecurityToken _);
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return false;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);

            if (DateTime.UtcNow > jwtToken.ValidTo)
            {
                return false;
            }

            var actor = jwtToken.Actor;

            user = new User(actor);

            return true;
        }
    }
}