# Plesk for Windows üzerine kurulum

Bu yol Docker kullanmaz. Plesk for Windows'ta Docker eklentisi yerel çalışmaz (uzak bir
Linux node ister) ve kabuk erişimi olmadan Docker Engine kurulamaz. Buna gerek de yok:
Plesk for Windows zaten IIS ve MSSQL barındırıyor.

Kurulum iki alan adına yapılır:

| Alan adı | İçerik |
| --- | --- |
| `example.com` | Angular istemci — statik dosyalar |
| `api.example.com` | .NET API — IIS altında ASP.NET Core uygulaması |

İstemci ve API farklı origin'lerde olduğu için API'nin CORS listesine istemcinin adresi
yazılır. Aynı alan adı altında `/api` ile ilerlemek de mümkün ama bu, IIS'te ARR ile
reverse proxy kurmayı gerektirir ve panelden yönetmesi daha zahmetlidir.

## 0. Önce şunu doğrula

**ASP.NET Core Hosting Bundle sunucuda kurulu mu?** IIS'in .NET uygulamasını
çalıştırabilmesi için `AspNetCoreModuleV2` gerekir ve bu, Hosting Bundle ile gelir.
Kurulu değilse sunucuda yönetici hakkı gerektirir; sağlayıcına açtırman lazım.

Panelden bakmanın pratik yolu: bir alan adının **Websites & Domains → Hosting
Settings** ekranında .NET / ASP.NET Core ile ilgili bir seçenek görüyorsan iyi
işarettir. Kesin cevap için sağlayıcına "IIS'te AspNetCoreModuleV2 var mı, .NET
Hosting Bundle kurulu mu?" diye sor.

Aşağıdaki publish komutu **self-contained** çıktı üretir; yani sunucuda .NET 9
runtime'ı olmasa da çalışır. Ama `AspNetCoreModuleV2` yine de şart.

**Veritabanı kotası:** demo modu slot başına bir veritabanı ister. Ana veritabanıyla
birlikte 3 slot için toplam **4 MSSQL veritabanı** gerekir. Planın buna yetmiyorsa
`Demo__SlotCount` değerini düşür (slot sayısı aynı anda demoyu kullanabilecek ziyaretçi
sayısıdır).

## 1. Veritabanlarını oluştur

Plesk → **Databases** → Add Database, tür olarak **Microsoft SQL Server**:

- `eAccountingServerDb` — ana veritabanı (kullanıcılar, firmalar)
- `eAccounting_Demo_Slot01`
- `eAccounting_Demo_Slot02`
- `eAccounting_Demo_Slot03`

**Hepsine aynı veritabanı kullanıcısını ver.** İlkini oluştururken bir kullanıcı aç,
diğerlerinde "existing user" seçeneğiyle aynı kullanıcıyı ekle. Uygulama slot
veritabanlarına ana bağlantı dizesindeki kimlik bilgileriyle bağlanır.

Slot veritabanlarının **önceden var olması şart**: Plesk kullanıcısı kendi
veritabanının sahibidir ama sunucuda yeni veritabanı oluşturma yetkisi yoktur.
Uygulama var olan veritabanına tabloları kendisi kurar, oluşturmayı denemez.

> Plesk veritabanı adlarına ön ek ekliyorsa (bazı planlarda `kullanıcı_` gibi), gerçek
> adları not al ve `Demo__DatabaseNamePrefix` değerini ona göre ayarla. Uygulama slot
> adlarını `ön ek + 01`, `+ 02` şeklinde üretir.

## 2. API'yi yayına hazırla

Kendi bilgisayarında:

```bash
dotnet publish eAccountingServer/src/eAccountingServer.WebApi/eAccountingServer.WebApi.csproj -c Release -r win-x64 --self-contained true -o publish/api
```

`publish/api` klasöründe oluşan `web.config` dosyasını **bu klasördeki
`api.web.config` ile değiştir** (adını `web.config` yap). İçindeki `CHANGE ME` yazan
değerleri doldur:

- `ConnectionStrings__SqlServer` — Plesk'in Databases ekranında gösterdiği sunucu adı,
  veritabanı adı, kullanıcı ve parola
- `Jwt__SecretKey` — uzun rastgele bir dize. Boş bırakılırsa uygulama açılmaz, bu
  kasıtlı. Üretmek için PowerShell'de:
  `-join ((1..64) | ForEach-Object { '{0:x2}' -f (Get-Random -Max 256) })`
- `Cors__AllowedOrigins__0` — istemcinin adresi, örn. `https://example.com`
- `Seed__AdminPassword` — ilk admin hesabının parolası

## 3. API'yi yükle

1. Plesk'te `api.example.com` alt alan adını oluştur.
2. `publish/api` içeriğini zip'le, **File Manager** ile `httpdocs` altına yükle, orada aç.
3. **Websites & Domains → Hosting Settings** → alan adına **Dedicated IIS Application
   Pool** ver ve .NET CLR sürümünü **No Managed Code** yap (ASP.NET Core kendi
   runtime'ını taşır, IIS'in CLR yüklemesine gerek yok).
4. Application Pool'u yeniden başlat.
5. `https://api.example.com/scalar/v1` adresini aç — API dokümantasyonu geliyorsa
   uygulama ayakta demektir.

İlk açılışta uygulama ana veritabanının migration'larını uygular, admin kullanıcısını
oluşturur ve demo slot'larını hazırlar. Bu birkaç saniye sürer.

## 4. İstemciyi yayına hazırla ve yükle

```bash
cd eAccountingClient
npm ci
npm run build -- --configuration production
```

`dist/erpclient/browser` içeriğini `example.com`'un `httpdocs` klasörüne yükle.

Sonra iki dosya:

- Bu klasördeki **`client.web.config`** dosyasını `httpdocs/web.config` olarak koy.
  Angular'ın kendi yönlendirmesi için gerekli; olmadan `/banks` gibi adresler
  doğrudan açıldığında 404 alırsın.
- **`httpdocs/assets/config.json`** dosyasını düzenle:

```json
{ "apiUrl": "https://api.example.com/api" }
```

Bu dosya her sayfa açılışında okunur, yani API adresini değiştirmek için istemciyi
yeniden derlemen gerekmez — File Manager'dan düzenleyip kaydetmen yeter.

## 5. Kontrol listesi

- `https://example.com` açılıyor, giriş ekranı geliyor
- **Demoyu Başlat** çalışıyor ve örnek verilerle dolu bir ekrana düşüyorsun
- Navbar'da kalan işlem ve süre görünüyor
- Kasa/banka kaydı ekleyip silebiliyorsun, sayaç düşüyor
- `admin` kullanıcısı ve `Seed__AdminPassword`'de yazdığın parola ile giriş yapılıyor

## Sorun giderme

**HTTP 500.30 / 500.31** — uygulama başlayamıyor. `web.config` içindeki
`stdoutLogEnabled` değerini geçici olarak `true` yap, `httpdocs\logs` klasörünü
oluştur, Application Pool'u yeniden başlat ve log dosyasını oku. En sık sebep: yanlış
bağlantı dizesi ya da boş `Jwt__SecretKey`.

**HTTP 502.5** — `AspNetCoreModuleV2` yok ya da uygulama çalıştırılamıyor. Hosting
Bundle meselesi, sağlayıcına sor.

**Demo başlamıyor, "tüm oturumlar dolu" diyor** — slot veritabanları oluşturulmamış ya
da kullanıcının erişimi yok. Log'da `Demo slot N could not be provisioned` satırını ara.

**Tarayıcı konsolunda CORS hatası** — `Cors__AllowedOrigins__0` değeri istemcinin
adresiyle birebir aynı olmalı (şema ve alt alan adı dahil, sonda `/` olmadan).
