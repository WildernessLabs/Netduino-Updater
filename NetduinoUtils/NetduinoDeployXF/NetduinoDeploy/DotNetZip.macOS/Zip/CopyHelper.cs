using System;

namespace Ionic.Zip
{
    internal static class CopyHelper
    {
        private static System.Text.RegularExpressions.Regex re =
                new System.Text.RegularExpressions.Regex(" \\(copy (\\d+)\\)$");

        private static int callCount = 0;

        internal static void Reset()
        {
            callCount = 0;
        }

        internal static string AppendCopyToFileName(string f)
        {
            callCount++;
            if (callCount > 25)
                throw new OverflowException("overflow while creating filename");

            int n = 1;
            int r = f.LastIndexOf(".");

            if (r == -1)
            {
                // there is no extension
                System.Text.RegularExpressions.Match m = re.Match(f);
                if (m.Success)
                {
                    n = Int32.Parse(m.Groups[1].Value) + 1;
                    string copy = String.Format(" (copy {0})", n);
                    f = f.Substring(0, m.Index) + copy;
                }
                else
                {
                    string copy = String.Format(" (copy {0})", n);
                    f = f + copy;
                }
            }
            else
            {
                //System.Console.WriteLine("HasExtension");
                System.Text.RegularExpressions.Match m = re.Match(f.Substring(0, r));
                if (m.Success)
                {
                    n = Int32.Parse(m.Groups[1].Value) + 1;
                    string copy = String.Format(" (copy {0})", n);
                    f = f.Substring(0, m.Index) + copy + f.Substring(r);
                }
                else
                {
                    string copy = String.Format(" (copy {0})", n);
                    f = f.Substring(0, r) + copy + f.Substring(r);
                }

                //System.Console.WriteLine("returning f({0})", f);
            }
            return f;
        }
    }
}
