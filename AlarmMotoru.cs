using System;

namespace AlarmApp
{
    public class AlarmMotoru
    {
        public bool Degerlendir(AlarmKarti kart)
        {
            if (kart == null || kart.Ifade == null)
                return false;

            try
            {
                object sonuc = kart.Ifade.Hesapla();

                if (sonuc == null)
                    return false;

                return Convert.ToBoolean(sonuc);
            }
            catch
            {
                return false;
            }
        }
    }
}