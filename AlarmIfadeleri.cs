using AlarmApp;

namespace AlarmApp
{
    public abstract class AlarmIfadesi
    {
        public abstract string Metin { get; }
    }


    public class ParametreIfadesi : AlarmIfadesi
    {
        public ParametreBase Parametre { get; set; }

        public override string Metin
        {
            get
            {
                return Parametre != null
                    ? Parametre.Ad
                    : "";
            }
        }
    }


    public class SabitDegerIfadesi : AlarmIfadesi
    {
        public string Deger { get; set; }

        public override string Metin
        {
            get
            {
                return Deger;
            }
        }
    }


    public class KarsilastirmaIfadesi : AlarmIfadesi
    {
        public AlarmIfadesi Sol { get; set; }

        public KarsilastirmaTuru Operator { get; set; }

        public AlarmIfadesi Sag { get; set; }


        public override string Metin
        {
            get
            {
                string op = "";

                switch (Operator)
                {
                    case KarsilastirmaTuru.Buyuktur:
                        op = ">";
                        break;

                    case KarsilastirmaTuru.Kucuktur:
                        op = "<";
                        break;

                    case KarsilastirmaTuru.Esittir:
                        op = "==";
                        break;

                    case KarsilastirmaTuru.BuyukEsittir:
                        op = ">=";
                        break;

                    case KarsilastirmaTuru.KucukEsittir:
                        op = "<=";
                        break;

                    case KarsilastirmaTuru.Arasindadir:
                        op = "Arasında";
                        break;

                    case KarsilastirmaTuru.Disindadir:
                        op = "Dışında";
                        break;

                    default:
                        op = "?";
                        break;
                }

                return string.Format(
                    "{0} {1} {2}",
                    Sol != null ? Sol.Metin : "",
                    op,
                    Sag != null ? Sag.Metin : "");
            }
        }
    }
}


public class MatematikIfadesi : AlarmIfadesi
{
    public AlarmIfadesi Sol { get; set; }

    public MatematikOperatoru Operator { get; set; }

    public AlarmIfadesi Sag { get; set; }


    public override string Metin
    {
        get
        {
            if (Operator == MatematikOperatoru.MutlakFark)
            {
                return string.Format(
                    "|{0} - {1}|",
                    Sol != null ? Sol.Metin : "",
                    Sag != null ? Sag.Metin : "");
            }

            string op = "";

            switch (Operator)
            {
                case MatematikOperatoru.Topla:
                    op = "+";
                    break;

                case MatematikOperatoru.Cikar:
                    op = "-";
                    break;

                case MatematikOperatoru.Carp:
                    op = "*";
                    break;

                case MatematikOperatoru.Bol:
                    op = "/";
                    break;
            }

            return string.Format(
                "({0} {1} {2})",
                Sol != null ? Sol.Metin : "",
                op,
                Sag != null ? Sag.Metin : "");
        }
    }
}

public enum MatematikOperatoru
{
    Topla,
    Cikar,
    Carp,
    Bol,
    MutlakFark
}