using AlarmApp;
using AlarmApp.Paketler;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class Uydu
{
    
    public const int TumuId = -1; // çakışma olmasın diye ??

    public int UyduId { get; }

    public bool TumunuTemsilEdiyorMu => UyduId == TumuId;

    
    public string GosterimAdi => TumunuTemsilEdiyorMu ? "Tümü" : UyduId.ToString();

    public Dictionary<string, PaketBase> Paketler { get; }

    public ObservableCollection<ParametreBase> Parametreler { get; }

    public Uydu(int uyduId)
    {
        UyduId = uyduId;

        Paketler = new Dictionary<string, PaketBase>();

        Parametreler = new ObservableCollection<ParametreBase>();
    }
}