using System;
using System.Collections.Generic;
using System.Windows;

namespace Aeon_AssetDecDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Respath = String.Empty;
        public static int TotalCount = 0;
        public static int glocount = 0;
        public static string ServerURL = "https://d2o6bt848m9g7d.cloudfront.net/";
        public static List<string> log = new List<string>();
        public static List<string> euablist = new List<string>();
        public static List<string> eeablist = new List<string>();
    }
}
