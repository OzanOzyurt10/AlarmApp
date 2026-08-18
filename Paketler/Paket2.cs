using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmApp.Paketler
{
    public class Paket2 : PaketBase
    {
        public override string PaketAdi => "Paket2";

        public double Sicaklik { get; set; }
        public double Hiz { get; set; }
        public double GPSX { get; set; }
    }
}
