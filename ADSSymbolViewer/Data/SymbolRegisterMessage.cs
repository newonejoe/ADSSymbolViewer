using System;
using System.Collections.Generic;
using System.Text;
using TwinCAT.TypeSystem;

namespace ADSSymbolViewer.Data
{
    public class SymbolRegisterMessage
    {
        public RegisterType RegisterType { get; set; }

        public string InstancePath {get;set;}
    }

    public enum RegisterType { 
        Subscribe = 0,
        UnSubscribe = 1
    }
}
