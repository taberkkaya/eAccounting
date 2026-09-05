namespace eAccountingServer.Domain.Abstractions;

/// <summary>
/// Kullanıcı hiçbir firmaya bağlı değilken firma verisine erişilmeye çalışıldı.
/// Veriler firma başına ayrı veritabanlarında durduğu için bağlanacak bir yer yok;
/// bu durumda sürücüden gelen "ConnectionString ilklendirilmedi" hatası kullanıcıya
/// hiçbir şey anlatmıyordu.
/// </summary>
public sealed class CompanyNotSelectedException()
    : Exception("Hesabınız henüz bir firmaya bağlı değil. Yönetici bir firma tanımlayıp sizi ona eklemeli.");
