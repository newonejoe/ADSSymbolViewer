using System;
using System.Collections.Generic;
using System.Text;

namespace ADSSymbolViewer.ViewModels
{
    public class SymbolValueViewModel : Caliburn.Micro.PropertyChangedBase
    {
        public string InstancePath { get; set; }

        public string Type { get; set; }



        private object value;

        public object Value
        {
            get { return value; }
            set
            {
                this.value = value;
                NotifyOfPropertyChange(nameof(Value));
            }
        }
    }
}
