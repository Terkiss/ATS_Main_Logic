
namespace TeruTeruServer.ManageLogic.Util
{
    public abstract class baseEncrypt
    {
        public abstract string EncryptString(string inputText, string password);
        public abstract string DecryptString(string inputText, string password);
    }

}
