using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmApp.Paketler
{
    public abstract class PaketBase
    {
        public abstract string PaketAdi { get; }

        public int UyduId { get; set; }
    }
}
