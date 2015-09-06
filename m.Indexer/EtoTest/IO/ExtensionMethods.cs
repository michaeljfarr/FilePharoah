using System;

namespace EtoTest.IO
{
    public static class ExtensionMethods
    {

        public static Func<string, bool> MatchFilePath(string pathToMatch)
        {
            pathToMatch = System.IO.Path.GetFullPath(pathToMatch);
            return
                pathToTest =>
                    string.Equals(pathToMatch, System.IO.Path.GetFullPath(pathToTest),
                        StringComparison.OrdinalIgnoreCase);
        }

        public static string EnsureSlashSuffix(string p)
        {
            if (!p.EndsWith("/") && !p.EndsWith("\\"))
            {
                return p + '/';
            }
            else
            {
                return p;
            }
        }
    }
}