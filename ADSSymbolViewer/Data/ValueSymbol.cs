using System;
using System.Collections.Generic;
using System.Text;

namespace ADSSymbolViewer.Data
{
    public class ValueSymbol
    {
        public string InstancePath { get; set; }

        public string Type { get; set; }

        public int CycleTime { get; set; }

        public int MaxDelay { get; set; }
    }
}
