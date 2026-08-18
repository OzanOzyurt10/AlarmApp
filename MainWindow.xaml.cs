using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

// 1,Paket1,data,data,data -> veri formatı bu şekilde olcak

namespace AlarmApp
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        private UdpClient _udpClient;

        private CancellationTokenSource _cts;

        private bool _isConnected = false;
        private long _toplamPaket = 0;
        private int _sonSaniyePaket = 0;

        private DispatcherTimer _hzTimer;
        public MainWindow()
        {
            InitializeComponent();
            _hzTimer = new DispatcherTimer();
            _hzTimer.Interval = TimeSpan.FromSeconds(1);
            _hzTimer.Tick += HzTimer_Tick;
            _hzTimer.Start();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
        }
        private void HzTimer_Tick(object sender, EventArgs e)
        {
            txtToplamPaket.Text = _toplamPaket.ToString("N0");
            txtVeriHizi.Text = $"{_sonSaniyePaket} Hz";

            _sonSaniyePaket = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            this.DataContext = null;
            this.DataContext = _viewModel;

            chkSesliAlarm.SetBinding(CheckBox.IsCheckedProperty,
                new System.Windows.Data.Binding("SesliAlarmAktif") { Mode = System.Windows.Data.BindingMode.TwoWay });


        }

        private void btnMantikIslemleri_Click(object sender, RoutedEventArgs e)
        {
            var gercekUydular = new ObservableCollection<Uydu>(
                _viewModel.Uydular.Where(u => !u.TumunuTemsilEdiyorMu));

            MantikIslemleri islemlerPenceresi =
                new MantikIslemleri(
                    gercekUydular,
                    _viewModel.KombinasyonListesi,
                    _viewModel);

            islemlerPenceresi.Owner = this;

            bool? sonuc = islemlerPenceresi.ShowDialog();

            
            if (sonuc == true)
            {
                _viewModel.KombinasyonListesi.Clear();

                foreach (var kural in islemlerPenceresi.KombinasyonListesi)
                {
                    _viewModel.KombinasyonListesi.Add(kural);
                }

                VeritabaniYardimcisi.MantiksalKurallariKaydet(
                    _viewModel.KombinasyonListesi);

                _viewModel.MantiksalKurallariDenetle();

                _viewModel.IsDirty = true;
            }
        }



        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveSettings();
        }

        private void btnGeriAl_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UndoSettings();
        }

        private void BtnBaglan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isConnected)
                {
                    if (!int.TryParse(txtUdpPort.Text, out int port))
                    {
                        MessageBox.Show("Geçerli bir UDP portu giriniz.");
                        return;
                    }

                    _ = UdpDinlemeyeBasla(port);

                    _isConnected = true;

                    btnConnect.Content = "Dinlemeyi Durdur";
                    ledBaglanti.Fill = Brushes.LimeGreen;

                    _viewModel.SistemLoglari.Add(
                        $"[{DateTime.Now:HH:mm:ss}] UDP dinleniyor. (Port: {port})");
                }
                else
                {
                    _cts?.Cancel();
                    _udpClient?.Close();

                    _isConnected = false;

                    btnConnect.Content = "Dinlemeye Başla";
                    ledBaglanti.Fill = Brushes.Red;

                    _viewModel.SistemLoglari.Add(
                        $"[{DateTime.Now:HH:mm:ss}] UDP dinleme durduruldu.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void rbTumAlarm_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AlarmGorunumu = MainViewModel.AlarmGorunumTipi.TumunuGoster;


            }
        }

        private void rbTekAlarm_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AlarmGorunumu =
                    MainViewModel.AlarmGorunumTipi.TekParametreli;
            }
        }

        private void rbCokAlarm_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AlarmGorunumu =
                    MainViewModel.AlarmGorunumTipi.CokParametreli;
            }
        }



        private void TekAlarmSil_Click(object sender, RoutedEventArgs e)
        {
            var buton = sender as Button;

            var kart = buton?.Tag as AlarmKartModel;

            if (kart == null)
                return;

            var cevap = MessageBox.Show(
                $"{kart.Baslik} alarmını silmek istiyor musunuz?",
                "Alarm Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (cevap != MessageBoxResult.Yes)
                return;

            var parametre =
                _viewModel.TumParametreler.FirstOrDefault(p =>
                    p.UyduId == kart.UyduId &&
                    p.PaketAdi == kart.PaketAdi &&
                    p.PropertyAdi == kart.ParametreAdi);

            if (parametre == null)
                return;


            parametre.AlarmAktif = false;
            parametre.UyariTetiklendi = false;
            parametre.EsikDegerObje = null;
            parametre.MaxEsikDegerObje = null;
            parametre.EsikGirildiMi = false;


            VeritabaniYardimcisi.AlarmSil(
                parametre.UyduId,
                parametre.PaketAdi,
                parametre.PropertyAdi);



            _viewModel.AlarmKartlariniGuncelle();
            _viewModel.UpdateAlarmCount();
        }
        private void TekAlarmDuzenle_Click(object sender, RoutedEventArgs e)
        {
            var buton = sender as Button;

            var kart = buton?.Tag as AlarmKartModel;

            if (kart == null)
                return;

            var parametre =
                _viewModel.TumParametreler.FirstOrDefault(p =>
                    p.UyduId == kart.UyduId &&
                    p.PaketAdi == kart.PaketAdi &&
                    p.PropertyAdi == kart.ParametreAdi);

            if (parametre == null)
                return;

            var duzenlePenceresi = new AlarmDuzenle(
                $"{kart.Baslik}  ·  {kart.PaketAdi} · {kart.ParametreAdi}",
                parametre.Kritiklik,
                esikAlanlariGosterilsin: true,
                mevcutKarsilastirma: parametre.Karsilastirma,
                mevcutEsikDeger: parametre.EsikDegerObje?.ToString() ?? "",
                mevcutMaxEsikDeger: parametre.MaxEsikDegerObje?.ToString() ?? "",
                isBoolean: parametre.TipAdi == "Boolean");

            duzenlePenceresi.Owner = this;

            bool? sonuc = duzenlePenceresi.ShowDialog();

            if (sonuc != true)
                return;

            parametre.Karsilastirma = duzenlePenceresi.SeciliKarsilastirma;
            parametre.EsikDegerObje = duzenlePenceresi.GirilenEsikDeger;
            parametre.MaxEsikDegerObje = duzenlePenceresi.GirilenMaxEsikDeger;
            parametre.EsikGirildiMi = true;
            parametre.Kritiklik = duzenlePenceresi.SeciliKritiklik;

            parametre.ControlEtMetodu();

            VeritabaniYardimcisi.AyarlariKaydet(
                new System.Collections.Generic.List<ParametreBase> { parametre });

            _viewModel.AlarmKartlariniGuncelle();
            _viewModel.UpdateAlarmCount();
        }

        private void CokluAlarmDuzenle_Click(object sender, RoutedEventArgs e)
        {
            var buton = sender as Button;

            var kart = buton?.Tag as AlarmKartModel;

            if (kart == null)
                return;

            if (kart.MantiksalKural == null ||
                kart.MantiksalKural.IcerilenKosullar == null ||
                !kart.MantiksalKural.IcerilenKosullar.Any())
            {
                MessageBox.Show(
                    "Düzenlenecek mantıksal kural bulunamadı.",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var duzenlePenceresi = new AlarmDuzenle(
                kart.Aciklama,
                kart.MantiksalKural.Kritiklik,
                kart.MantiksalKural.IcerilenKosullar,
                kart.MantiksalKural.Baglac,
                kosul => kosul.SolTarafMetni,
                kosul => KosulBooleanMi(kosul));

            duzenlePenceresi.Owner = this;

            bool? sonuc = duzenlePenceresi.ShowDialog();

            if (sonuc != true)
                return;

            kart.MantiksalKural.Kritiklik = duzenlePenceresi.SeciliKritiklik;

            if (!string.IsNullOrEmpty(duzenlePenceresi.SeciliBaglac))
                kart.MantiksalKural.Baglac = duzenlePenceresi.SeciliBaglac;

            BirlesikMetinYenile(kart.MantiksalKural);

            VeritabaniYardimcisi.MantiksalKurallariKaydet(
                _viewModel.KombinasyonListesi);

            _viewModel.MantiksalKurallariDenetle();

            _viewModel.AlarmKartlariniGuncelle();

            _viewModel.UpdateAlarmCount();
        }

        
        private void BirlesikMetinYenile(BirlesikMantiksalKural kural)
        {
            if (kural.IcerilenKosullar == null || !kural.IcerilenKosullar.Any())
                return;

            kural.BirlesikMetin = kural.IcerilenKosullar.Count == 1
                ? kural.IcerilenKosullar[0].KosulMetni
                : "(" + string.Join(
                    $" {kural.Baglac} ",
                    kural.IcerilenKosullar.Select(k => k.KosulMetni)) + ")";
        }

       
        private bool KosulBooleanMi(MantiksalKosul kosul)
        {
            if (kosul.MatematikselMi)
                return false;

            var parametre = _viewModel.TumParametreler.FirstOrDefault(p =>
                p.UyduId == kosul.UyduId &&
                p.PaketAdi == kosul.PaketAdi &&
                p.Ad == kosul.ParametreAdi);

            return parametre?.TipAdi == "Boolean";
        }

        private void CokluAlarmSil_Click(object sender, RoutedEventArgs e)
        {
            var buton = sender as Button;

            var kart =
                buton?.Tag as AlarmKartModel;

            if (kart == null)
                return;

            var cevap = MessageBox.Show(
                "Bu çok parametreli alarmı silmek istiyor musunuz?",
                "Alarm Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (cevap != MessageBoxResult.Yes)
                return;

            if (kart.MantiksalKural == null)
            {
                MessageBox.Show(
                    "Silinecek mantıksal kural bulunamadı.",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            _viewModel.KombinasyonListesi.Remove( kart.MantiksalKural);


            VeritabaniYardimcisi.MantiksalKurallariKaydet(_viewModel.KombinasyonListesi);


            _viewModel.MantiksalKurallariDenetle();


            _viewModel.AlarmKartlariniGuncelle();


            _viewModel.UpdateAlarmCount();
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _udpClient?.Close();

            base.OnClosed(e);
        }

        private async Task UdpDinlemeyeBasla(int port)
        {
            _cts = new CancellationTokenSource();

            _udpClient = new UdpClient(port);

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult sonuc = await _udpClient.ReceiveAsync();

                    string veri = Encoding.UTF8.GetString(sonuc.Buffer).Trim();

                    if (string.IsNullOrWhiteSpace(veri))
                        continue;

                    string[] parcalar = veri.Split(',');

                    if (parcalar.Length < 3)
                        continue;

                    int uyduId = int.Parse(parcalar[0]);

                    string paketAdi = parcalar[1];

                    Dispatcher.Invoke(() =>
                    {
                        _toplamPaket++;
                        _sonSaniyePaket++;

                        _viewModel.VeriGeldi(uyduId, paketAdi, parcalar);
                    });
                }
                catch
                {
                    // Dinleme durdurulunca buraya düşebilir.
                }
            }
        }

    }
}