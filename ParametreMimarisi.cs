using AlarmApp.Paketler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AlarmApp
{
    public abstract class ParametreBase : INotifyPropertyChanged
    {
        public string Ad { get; set; }

        public string PaketAdi { get; set; }

        public string PropertyAdi { get; set; }

        public string TipAdi { get; set; }

        public PaketBase Paket { get; set; }

        public PropertyInfo Property { get; set; }

        private bool _esikGirildiMi;

        public bool EsikGirildiMi
        {
            get => _esikGirildiMi;
            set
            {
                _esikGirildiMi = value;
                OnPropertyChanged();
            }
        }

        public int UyduId { get; set; }
        public bool AlarmAktif { get; set; }

        public AlarmKritiklik Kritiklik { get; set; } = AlarmKritiklik.Orta;

  


        private bool _uyariTetiklendi;
        public bool UyariTetiklendi
        {
            get => _uyariTetiklendi;
            set { _uyariTetiklendi = value; OnPropertyChanged(); }
        }

        private KarsilastirmaTuru _karsilastirma;
        public KarsilastirmaTuru Karsilastirma
        {
            get => _karsilastirma;
            set
            {
                _karsilastirma = TipAdi == "Boolean" ? KarsilastirmaTuru.Esittir : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaxEsikGerekliMi));
                OnPropertyChanged(nameof(AlarmKurali));
            }
        }
        public bool MaxEsikGerekliMi =>
            Karsilastirma == KarsilastirmaTuru.Arasindadir || Karsilastirma == KarsilastirmaTuru.Disindadir;

        public string AlarmKurali
        {
            get
            {
                if (EsikDegerObje == null)
                    return "-";

               
                if (TipAdi == "Boolean")
                    return $"= {EsikDegerObje}";

                switch (Karsilastirma)
                {
                    case KarsilastirmaTuru.Buyuktur:
                        return $"> {EsikDegerObje}";

                    case KarsilastirmaTuru.Kucuktur:
                        return $"< {EsikDegerObje}";

                    case KarsilastirmaTuru.Esittir:
                        return $"= {EsikDegerObje}";

                    case KarsilastirmaTuru.BuyukEsittir:
                        return $">= {EsikDegerObje}";

                    case KarsilastirmaTuru.KucukEsittir:
                        return $"<= {EsikDegerObje}";

                    case KarsilastirmaTuru.Arasindadir:
                        return $"{EsikDegerObje} - {MaxEsikDegerObje}";

                    case KarsilastirmaTuru.Disindadir:
                        return $"Dışında ({EsikDegerObje} - {MaxEsikDegerObje})";

                    default:
                        return "-";
                }
            }
        }


        public IEnumerable<KarsilastirmaTuru> KarsilastirmaSecenekleri
        {
            get
            {
                if (TipAdi == "Boolean")
                    return new[] { KarsilastirmaTuru.Esittir };

                return System.Enum.GetValues(typeof(KarsilastirmaTuru)).Cast<KarsilastirmaTuru>();
            }
        }


        private string _hataMesaji;
        public string HataMesaji
        {
            get => _hataMesaji;
            set { _hataMesaji = value; OnPropertyChanged(); OnPropertyChanged(nameof(HatalıMi)); }
        }
        public bool HatalıMi => !string.IsNullOrEmpty(HataMesaji);

        

        public abstract object AnlikDegerObje { get; set; }
        public abstract object EsikDegerObje { get; set; }
        public abstract object MaxEsikDegerObje { get; set; }

        public event Action<string, string> OnLogUretildi;
        protected void LogFirlat(string mesaj) => OnLogUretildi?.Invoke(Ad, mesaj);

        public abstract void ControlEtMetodu();
        public abstract ParametreBase Clone();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public class Parametre<T> : ParametreBase where T : IComparable
    {
        private T _anlikDeger;
        private T _esikDeger;
        private T _maxEsikDeger;
        private bool _loglandi = false;

        public T AnlikDeger
        {
            get
            {
                if (Paket != null && Property != null)
                {
                    object value = Property.GetValue(Paket);
                    if (value != null)
                        return (T)value;
                }

                return _anlikDeger;
            }
            set
            {
                _anlikDeger = value;

                if (Paket != null && Property != null)
                {
                    Property.SetValue(Paket, value);
                }

                OnPropertyChanged();
                ControlEtMetodu();
            }
        }
        public T EsikDeger
        {
            get => _esikDeger;
            set
            {
                _esikDeger = value;

                
                EsikGirildiMi = value != null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(AlarmKurali));
            }
        }
        public T MaxEsikDeger
        {
            get => _maxEsikDeger;
            set
            {
                _maxEsikDeger = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AlarmKurali));
            }
        }

        public override object AnlikDegerObje
        {
            get => AnlikDeger;
            set => AnlikDeger = SafeConvert(value, "Anlık Değer");
        }
        public override object EsikDegerObje
        {
            get => EsikDeger;
            set => EsikDeger = SafeConvert(value, "Eşik Değer");
        }
        public override object MaxEsikDegerObje
        {
            get => MaxEsikDeger;
            set => MaxEsikDeger = SafeConvert(value, "Maksimum Eşik Değer");
        }

        private T SafeConvert(object value, string alanAdi)
        {
            try
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {

                    HataMesaji = null;
                    return default;
                }

                string inputStr = value.ToString().Trim();


                if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(inputStr, out bool boolSonuc))
                    {
                        HataMesaji = null;
                        return (T)(object)boolSonuc;
                    }
                    throw new FormatException($"'{inputStr}' geçerli bir Doğru/Yanlış (Boolean) değeri değil.");
                }


                T sonuc = (T)Convert.ChangeType(inputStr, typeof(T), System.Globalization.CultureInfo.InvariantCulture);


                HataMesaji = null;
                return sonuc;
            }
            catch (Exception ex)
            {

                HataMesaji = $"{alanAdi} hatası: {ex.Message}";
                LogFirlat($"GEÇERSİZ GİRİŞ! {alanAdi} alanına '{value}' yazıldı. Sistem bu parametre için eski kararlı değerleri koruyor.");


                if (alanAdi == "Anlık Değer") return _anlikDeger;
                if (alanAdi == "Eşik Değer") return _esikDeger;
                return _maxEsikDeger;
            }
        }



        public override void ControlEtMetodu()
        {

            if (HatalıMi) return;

            if (!AlarmAktif || !EsikGirildiMi)
            {
                UyariTetiklendi = false;
                return;
            }


            if (AnlikDeger == null || EsikDeger == null) return;


            bool kosulSaglandi = false;

            if (typeof(T) == typeof(bool))
            {
                bool anlik = Convert.ToBoolean(AnlikDeger);
                bool esik = Convert.ToBoolean(EsikDeger);

                UyariTetiklendi = (anlik == esik);

                if (UyariTetiklendi)
                {
                    if (!_loglandi)
                    {
                        LogFirlat($"Boolean alarm oluştu. Değer={anlik}");
                        _loglandi = true;
                    }
                }
                else
                {
                    if (_loglandi)
                        LogFirlat("Boolean alarm normale döndü.");

                    _loglandi = false;
                }

                return;
            }

            switch (Karsilastirma)
            {
                case KarsilastirmaTuru.Buyuktur:
                    kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) > 0;
                    break;
                case KarsilastirmaTuru.Kucuktur:
                    kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) < 0;
                    break;
                case KarsilastirmaTuru.Esittir:
                    kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) == 0;
                    break;
                case KarsilastirmaTuru.BuyukEsittir:
                    kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) >= 0;
                    break;
                case KarsilastirmaTuru.KucukEsittir:
                    kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) <= 0;
                    break;
                case KarsilastirmaTuru.Arasindadir:
                    if (MaxEsikDeger != null)
                        kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) >= 0 && AnlikDeger.CompareTo(MaxEsikDeger) <= 0;
                    break;
                case KarsilastirmaTuru.Disindadir:
                    if (MaxEsikDeger != null)
                        kosulSaglandi = AnlikDeger.CompareTo(EsikDeger) < 0 || AnlikDeger.CompareTo(MaxEsikDeger) > 0;
                    break;
            }
            
                
        
            UyariTetiklendi = kosulSaglandi;

            if (kosulSaglandi)
            {
                if (!_loglandi)
                {
                    LogFirlat($"Eşik ihlali gerçekleşti! Değer: {AnlikDeger} (Kriter: {Karsilastirma})");
                    _loglandi = true;
                }
            }
            else
            {
                if (_loglandi)
                {
                    LogFirlat($"Sistem normale döndü. Değer: {AnlikDeger}");
                }
                _loglandi = false;
            }
        }

        public override ParametreBase Clone()
        {
            return new Parametre<T>
            {
                Ad = this.Ad,
                TipAdi = this.TipAdi,
                Karsilastirma = this.Karsilastirma,
                AnlikDeger = this._anlikDeger,
                EsikDeger = this._esikDeger,
                MaxEsikDeger = this._maxEsikDeger,
                UyariTetiklendi = this.UyariTetiklendi,
                HataMesaji = this.HataMesaji,
                Kritiklik = this.Kritiklik,
                _loglandi = this._loglandi,
                Paket = this.Paket,
                Property = this.Property,
                PaketAdi = this.PaketAdi,
                PropertyAdi = this.PropertyAdi,
                UyduId = this.UyduId,
            };
        }
    }
}