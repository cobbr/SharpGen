// Author: Ryan Cobb (@cobbr_io)
// Project: SharpGen (https://github.com/cobbr/SharpGen)
// License: BSD 3-Clause

using System.IO;
using System.Text;
using System.Reflection;

namespace SharpGen
{
    public static class Common
    {
        public static Encoding SharpGenEncoding = Encoding.UTF8;
        public static string SharpGenDirectory = SplitFirst(SplitFirst(Assembly.GetExecutingAssembly().Location, "bin"), "SharpGen.dll");
        public static string SharpGenSourceDirectory = SharpGenDirectory + "Source" + Path.DirectorySeparatorChar;
        public static string SharpGenReferencesDirectory = SharpGenDirectory + "References" + Path.DirectorySeparatorChar;
        public static string SharpGenReferencesConfig = SharpGenReferencesDirectory + "references.yml";
        public static string Net35Directory = SharpGenReferencesDirectory + "net35" + Path.DirectorySeparatorChar;
        public static string Net40Directory = SharpGenReferencesDirectory + "net40" + Path.DirectorySeparatorChar;
        public static string SharpGenResourcesDirectory = SharpGenDirectory + "Resources" + Path.DirectorySeparatorChar;
        public static string SharpGenResourcesConfig = SharpGenResourcesDirectory + "resources.yml";
        public static string SharpGenx86ResourcesDirectory = SharpGenResourcesDirectory + "x86" + Path.DirectorySeparatorChar;
        public static string SharpGenx64ResourcesDirectory = SharpGenResourcesDirectory + "x64" + Path.DirectorySeparatorChar;

        public static string SharpGenOutputDirectory = SharpGenDirectory + "Output" + Path.DirectorySeparatorChar;
        public static string SharpGenRefsDirectory = SharpGenDirectory + "refs" + Path.DirectorySeparatorChar;

        private static string SplitFirst(string FullString, string SubString)
        {
            return FullString.Contains(SubString) ? FullString.Substring(0, FullString.IndexOf(SubString)) : FullString;
        }
    }
}
