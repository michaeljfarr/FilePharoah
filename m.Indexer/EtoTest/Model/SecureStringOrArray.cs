using System;
using System.Runtime.InteropServices;
using System.Security;

namespace EtoTest.Model
{
    public class SecureStringOrArray : IDisposable
    {
        public SecureString SecureString => _secureString;

        readonly SecureString _secureString;
        readonly byte[] _byteArray;

        public SecureStringOrArray(byte[] byteArray)
        {
            if (byteArray == null)
            {
                throw new ArgumentNullException("SecureStringOrArray(byte[])");
            }
            _byteArray = byteArray;
        }
        public SecureStringOrArray(SecureString secureString)
        {
            _secureString = secureString;
        }

        public byte[] ByteArray
        {
            get
            {
                if (_byteArray == null)
                {
                    return ConvertToByteArray(_secureString);
                }
                return _byteArray;
            }
        }

        public static byte[] ConvertToByteArray(SecureString password)
        {
            IntPtr ptr = IntPtr.Zero;
            int targetByte = 0;
            byte[] passwordBytes = new byte[password.Length];
            try
            {
                ptr = Marshal.SecureStringToBSTR(password);

                int sourcePair = 0;
                byte b = Marshal.ReadByte(ptr, sourcePair);
                while (((char)b) != '\0')
                {
                    passwordBytes[targetByte++] = b;
                    b = Marshal.ReadByte(ptr, sourcePair + 1);
                    if (((char)b) != '\0')
                    {
                        //if the second half isn't 0, lets read it too
                        passwordBytes[targetByte++] = b;
                    }

                    sourcePair = sourcePair + 2;  // BSTR is unicode and occupies 2 bytes
                    b = Marshal.ReadByte(ptr, sourcePair);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(ptr);
                }
            }
            //create array of the correct size
            byte[] passwordBytes2 = new byte[targetByte];
            for (int i = 0; i < targetByte; i++)
            {
                passwordBytes2[i] = passwordBytes[i];
                passwordBytes[i] = 0;
            }
            return passwordBytes2;
        }

        public void Dispose()
        {
            if (_secureString != null)
            {
                _secureString.Dispose();
            }
            if (_byteArray != null)
            {
                //we currently avoid wiping the byteArray because others have references
            }
        }

        internal void ZeroBytesIfRecreatable(byte[] byteArray)
        {
            if (_secureString != null)
            {
                ZeroBytes(byteArray);
            }
        }

        private static void ZeroBytes(byte[] byteArray)
        {
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = 0;
            }
        }
    }
}