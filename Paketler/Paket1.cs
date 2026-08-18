using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmApp.Paketler
{
    public class Paket1 : PaketBase
    {
        public override string PaketAdi => "Paket1";

        public double Sicaklik { get; set; }
        public double Basinc { get; set; }
        public double Nem { get; set; }

        public bool Isıtıcı { get; set; }



    }
}
