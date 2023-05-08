using Escorp.Printing;
using Ghostscript.NET.Processor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrinterApp
{
    public class PrinterUtils
    {
        public static bool IsGhostScriptNotInstalled()
        {
            try
            {
                new GhostscriptProcessor();
                return false;
            }
            catch(Exception e) { return true; }
        }

        public static bool IsPrinterInstalled(string name)
        {
            List<string> printers = new List<string>();
            foreach(string p in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                printers.Add(p);
            }
            return printers.Any(p => p.Equals(name));
        }

        public static bool IsMonitorInstalled(string name)
        {
            Monitor[] monitors = Monitor.All;
            return monitors.ToList().Any(monitor => monitor.Name.Equals(name));
        }

        public static bool IsPortInstalled(string name)
        {
            Port[] ports = Port.All;
            return ports.ToList().Any(port => port.Name.Equals(name));
        }

        public static bool IsDriverInstalled(string name)
        {
            Driver[] drivers = Driver.All;
            return drivers.ToList().Any(driver => driver.Name.Equals(name));
        }
    }
}
