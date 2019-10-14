// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

namespace TSS
{
    public static class TSSPrefs
    {
        #region Properties

        private static bool prefsLoaded;

        public static int dynamicPathSampling = 25;

        public class Symbols
        {
            public static char percent = '%';
            public static char space = ' ';
            public static char dot = '.';
        }

        #endregion
    }

    public static class TSSInfo
    {
        #region Properties

        public static string version { get { return "1.6.4"; } }

        public static string author { get { return @"ObelardO"; } }
        public static string email { get { return @"obelardos@gmail.com"; } }

        #endregion
    }
}
