namespace Core.Authentication
{
    public interface IUserProvider
    {
        /// <summary>
        /// Gets the authenticated <see cref="User"/> for this request.
        /// </summary>
        User GetUser();

        /// <summary>
        /// Gets the authentication token that was used for the current request.
        /// </summary>
        /// <remarks>This does not generate a new token, merely gets the current request's authenticated token.</remarks>
        string GetToken();
    }
}