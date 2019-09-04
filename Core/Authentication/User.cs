namespace Core.Authentication
{
    public class User
    {
        public User(string loginIdentity)
        {
            var indexOfSlash = loginIdentity.IndexOf('\\');
            LoginName = loginIdentity.Substring(indexOfSlash + 1);
            Domain = loginIdentity.Substring(0, indexOfSlash);
        }

        public string LoginName { get; }

        public string Domain { get; }
    }
}