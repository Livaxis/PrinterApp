using Ghostscript.NET.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrinterApp
{
    public class PrinterUtils
    {
        public static bool isGhostScriptNotInstalled()
        {
            try
            {
                new GhostscriptProcessor();
                return false;
            }
            catch(Exception e) { return true; }
        }
    }
}
