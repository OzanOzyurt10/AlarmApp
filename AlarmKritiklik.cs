namespace AlarmApp
{
    // Bir alarmın önem derecesini belirtir. Sıralama önemlidir: birden
    // fazla alarm birleştirildiğinde en yüksek (en kritik) seviye esas alınır.
    public enum AlarmKritiklik
    {
        Dusuk,
        Orta,
        Yuksek,
        Kritik
    }
}
