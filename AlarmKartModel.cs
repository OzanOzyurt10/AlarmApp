using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AlarmApp
{
    public class AlarmKartModel : INotifyPropertyChanged
    {
        public bool MantiksalMi { get; set; }

        public string AlarmTuru { get; set; }

        public int UyduId { get; set; }

        public string PaketAdi { get; set; }

        public string ParametreAdi { get; set; }

        public string Baslik { get; set; }

        public string Aciklama { get; set; }

        public string AnlikDeger { get; set; }

        public BirlesikMantiksalKural MantiksalKural { get; set; }

        private AlarmKritiklik _kritiklik = AlarmKritiklik.Orta;

        public AlarmKritiklik Kritiklik
        {
            get => _kritiklik;

            set
            {
                _kritiklik = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(KritiklikMetni));

                OnPropertyChanged(nameof(KritiklikRengi));

                OnPropertyChanged(nameof(KritiklikIkon));
            }
        }

        public string KritiklikMetni
        {
            get
            {
                switch (Kritiklik)
                {
                    case AlarmKritiklik.Dusuk:
                        return "DÜŞÜK";

                    case AlarmKritiklik.Orta:
                        return "ORTA";

                    case AlarmKritiklik.Yuksek:
                        return "YÜKSEK";

                    case AlarmKritiklik.Kritik:
                        return "KRİTİK";

                    default:
                        return "ORTA";
                }
            }
        }

        public string KritiklikIkon
        {
            get
            {
                switch (Kritiklik)
                {
                    case AlarmKritiklik.Dusuk:
                        return "ℹ";

                    case AlarmKritiklik.Orta:
                        return "◆";

                    case AlarmKritiklik.Yuksek:
                        return "▲";

                    case AlarmKritiklik.Kritik:
                        return "⛔";

                    default:
                        return "◆";
                }
            }
        }

        public string KritiklikRengi
        {
            get
            {
                switch (Kritiklik)
                {
                    case AlarmKritiklik.Dusuk:
                        return "#0EA5E9";

                    case AlarmKritiklik.Orta:
                        return "#EAB308";

                    case AlarmKritiklik.Yuksek:
                        return "#F97316";

                    case AlarmKritiklik.Kritik:
                        return "#DC2626";

                    default:
                        return "#EAB308";
                }
            }
        }

        private bool _aktif;

        public bool Aktif
        {
            get => _aktif;

            set
            {
                _aktif = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(Durum));

                OnPropertyChanged(nameof(Renk));
            }
        }

        public string Durum =>
            Aktif ? "AKTİF" : "NORMAL";

        public string Renk =>
            Aktif ? "#DC2626" : "#16A34A";

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string p = "")
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(p));
        }
    }
}