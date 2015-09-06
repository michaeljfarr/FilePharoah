using System.Collections.Generic;
using EtoTest.Model;

namespace EtoTest.Interfaces
{
    class SecretKeyProvider : ISecretKeyProvider
    {
        readonly Dictionary<string, string> _secrets = new Dictionary<string, string>();
        public void Initialize(IEnumerable<Credential> credentials)
        {
            foreach (var credential in credentials)//.Where(a=>a.Url!=null)
            {
                _secrets[credential.Url] = credential.GetPasswordAsString();
            }
        }

        public string GetSecret(string key)
        {
            string secret;
            return _secrets.TryGetValue(key, out secret) ? secret : null;
        }
    }
}