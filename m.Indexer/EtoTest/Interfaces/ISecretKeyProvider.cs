using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using EtoTest.Model;

namespace EtoTest.Interfaces
{
    internal interface ISecretKeyProvider
    {
        void Initialize(IEnumerable<Credential> credentials);
        string GetSecret(string key);

    }
}
