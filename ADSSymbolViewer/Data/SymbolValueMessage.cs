using System;
using System.Collections.Generic;
using System.Text;

namespace ADSSymbolViewer.Data
{
    public class SymbolValueMessage : Caliburn.Micro.PropertyChangedBase
    {
        public string InstancePath { get; set; }

        public string Type { get; set; }

        public object Value { get; set; }
    }
}
