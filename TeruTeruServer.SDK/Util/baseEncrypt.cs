namespace TeruTeruServer.SDK.Util
{
    public abstract class BaseEncrypt
    {
        public abstract string EncryptString(string inputText, string password);
        public abstract string DecryptString(string inputText, string password);
    }
}
