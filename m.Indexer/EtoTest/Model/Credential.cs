using System;
using System.Runtime.Serialization;

namespace EtoTest.Model
{
    [DataContract(Namespace = "http://schemas.michaelfarr/PasswordManagement/2010/08/22")]
    [Serializable]
    public class Credential 
    {
        [DataMember]
        public String Url { get; set; }
        [DataMember]
        public String UserId { get; set; }
        [DataMember]
        public Char[] Password { get; set; }
        [DataMember]
        public String UserInputElementName { get; set; }
        [DataMember]
        public String PasswordInputElementName { get; set; }
        [DataMember]
        public int Usage { get; set; }
        [DataMember]
        public bool Hidden { get; set; }
        [DataMember]
        public DateTime LastUsed { get; set; }
        //public ICommand LaunchUrl { get; set; }


        public string GetPasswordAsString()
        {
            return new String(Password);
        }

        public void SetPassword(string password)
        {
            Password = password.ToCharArray();
        }

        public void SetPassword(char[] password)
        {
            Password = password;
        }


        public char[] GetPasswordAsCharArray()
        {
            return Password;
        }
    }
}