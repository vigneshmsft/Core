namespace Core.Authentication.Tokens
{
    public class UserFromAuthenticationToken : IUserProvider
    {
        private readonly ITokenValidator _tokenValidator;

        private User _currentUser;
        private string _currentToken;

        public UserFromAuthenticationToken(ITokenValidator tokenValidator)
        {
            _tokenValidator = tokenValidator;
        }

        public bool ReadUserFromToken(string token)
        {
            _currentToken = token;
            _currentUser = null;
            var validToken = _tokenValidator.ValidateToken(token, out User user);

            if (validToken)
                _currentUser = user;

            return validToken;
        }

        public User GetUser()
        {
            return _currentUser;
        }

        public string GetToken()
        {
            return _currentToken;
        }
    }
}
