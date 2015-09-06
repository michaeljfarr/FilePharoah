using System;
using System.Linq;
using EtoTest.Dialogs;

namespace EtoTest
{
    static class Startup
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Eto.Forms.Application().Run(new SecurePartsLayout());
        }
    }
}
