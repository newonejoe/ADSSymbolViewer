using ADSSymbolViewer.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADSSymbolViewer
{
    public class AppOptions
    {

        public string AMSNetId { get; set; }

        public int Port { get; set; }

        public List<ValueSymbol> ValueSymbols { get; set; }


        public AppOptions()
        {
            ValueSymbols = new List<ValueSymbol>();
        }
    }
}
