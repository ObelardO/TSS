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

        #region Save & Load

        public static void Load()
        {
            if (prefsLoaded) return;

            /*
            dynamicPathSampling = PlayerPrefs.GetInt("TSS_dynamicPathSampling", 25);

            Symbols.percent = PlayerPrefs.GetString("TSS_symbolPercent", "%")[0];
            Symbols.space = PlayerPrefs.GetString("TSS_symbolSpace", " ")[0];
            Symbols.dot = PlayerPrefs.GetString("TSS_symbolDot", ".")[0];
            */
            prefsLoaded = true;
        }

        public static void Save()
        {
            /*
            PlayerPrefs.SetInt("TSS_dynamicPathSampling", dynamicPathSampling);

            PlayerPrefs.SetString("TSS_symbolPercent", Symbols.percent.ToString());
            PlayerPrefs.SetString("TSS_symbolSpace", Symbols.space.ToString());
            PlayerPrefs.SetString("TSS_symbolDot", Symbols.dot.ToString());
            */
        }

        #endregion
    }

    public static class TSSInfo
    {
        #region Properties

        public static string version { get { return @"1.6.19"; } }

        public static string author { get { return @"ObelardO"; } }
        public static string email { get { return @"obelardos@gmail.com"; } }

        #endregion
    }
}
