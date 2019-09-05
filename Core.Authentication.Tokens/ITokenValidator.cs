namespace Core.Authentication.Tokens
{
    public interface ITokenValidator
    {
        bool ValidateToken(string token, out User user);
    }
}
