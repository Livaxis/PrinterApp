using PrinterApp.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrinterApp
{
    public class SettingsUtils
    {
        public static void Logout() 
        {
            Settings.Default.AuthToken = null;
            Settings.Default.UserLogin = null;
            Settings.Default.PsFolder = null;
            Settings.Default.PdfFolder = null;
            Settings.Default.PrinterName = null;
            Settings.Default.PortName = null;
            Settings.Default.Save();
        }
    }
}
