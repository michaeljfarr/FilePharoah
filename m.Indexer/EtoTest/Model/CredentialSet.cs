using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EtoTest.Model
{
    [DataContract(Namespace = "http://schemas.michaelfarr/PasswordManagement/2010/08/22")]
    [Serializable]
    public class CredentialSet
    {
        public CredentialSet()
        {
            Credentials = new List<Credential>();
        }
        [DataMember]
        public List<Credential> Credentials { get; set; }

        public void AddCredential(String url, String userId, Char[] password)
        {
            var newCredential = new Credential { Url = url, UserId = userId, Password = password };
            Credentials.Add(newCredential);
        }

        public void AddCredential(
            String url, String userId, Char[] password, int usage, bool hidden)
        {
            var newCredential = new Credential { Url = url, UserId = userId, Password = password, Usage = usage, Hidden = hidden };
            Credentials.Add(newCredential);
        }


        public void AddCredential(
            String url, String userId, Char[] password, int usage, bool hidden, String userInputElementName, String passwordInputElementName)
        {
            var newCredential = new Credential
            {
                Url = url,
                UserId = userId,
                Password = password,
                Usage = usage,
                Hidden = hidden,
                UserInputElementName = userInputElementName,
                PasswordInputElementName = passwordInputElementName
            };
            //newCredential.LaunchUrl = new LaunchUrl(newCredential);
            Credentials.Add(newCredential);
        }
    }
}