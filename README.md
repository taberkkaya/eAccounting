# eAccounting

Çok kiracılı (multi-tenant) bir ön muhasebe uygulaması. Her firmanın verisi **kendi
veritabanında** tutulur; oturum açan kullanıcının firması JWT üzerinden çözülerek
bağlantı çalışma zamanında kurulur.

Uygulama aynı zamanda **herkese açık bir demo** olarak çalışabilir: her ziyaretçiye
kendi izole veritabanı verilir, oturum boyunca kayıt ekleyip silebilir, oturum bitince
her şey sıfırlanır.

## 🚀 Teknolojiler

**Sunucu** — .NET 9 Web API, Clean Architecture, CQRS (MediatR), Entity Framework Core,
SQL Server, ASP.NET Identity + JwtBearer, Mapster, FluentValidation, ResultKit,
GenericRepository, Scalar/OpenAPI, rate limiting, IMemoryCache (Redis opsiyonel).

**İstemci** — Angular 18 (standalone components + signals), AdminLTE, flexi-grid,
flexi-select, flexi-toast, form-validate-angular, jwt-decode, tr-currency.

## 🏛️ Mimari

```
eAccountingServer/src
├── eAccountingServer.Domain          entity'ler, value object'ler, repository sözleşmeleri
├── eAccountingServer.Application     CQRS handler'ları, validation, servis arayüzleri
├── eAccountingServer.Infrastructure  EF Core context'leri, repository'ler, JWT, cache, demo
└── eAccountingServer.WebApi          controller'lar, minimal API modülleri, middleware
```

İki veritabanı katmanı vardır:

- **ApplicationDbContext** → ana veritabanı. Kullanıcılar (Identity), firmalar ve
  firma-kullanıcı eşleşmeleri burada durur. Her `Company` kaydı kendi
  `Server / DatabaseName / Username / Password` bilgisini taşır.
- **CompanyDbContext** → firmaya ait veritabanı. Kasalar, bankalar ve hareketleri
  burada durur. Bağlantı dizesi, isteğin JWT'sindeki `CompanyId` claim'inden
  çözülerek çalışma zamanında üretilir.

`POST /api/Companies/MigrateAll` tüm firma veritabanlarını migrate eder.

## 🧪 Demo modu

`Demo:Enabled` açıkken uygulama başlangıçta sabit sayıda **sandbox veritabanı**
hazırlar (migrate eder ve örnek veriyle doldurur). Bir ziyaretçi `POST /api/demo/start`
çağırdığında bunlardan biri o oturuma kiralanır ve oturuma özel bir JWT üretilir.

- Sandbox, kiralanma anında sıfırlanır; her ziyaretçi aynı başlangıç verisiyle açar.
- Yazma işlemleri (`Create`, `Update`, `DeleteById`) sayılır ve `Demo:WriteLimit`
  aşıldığında reddedilir.
- Boşta kalma, mutlak süre ve **çalışma kümesi (working set) eşiği** için bir arka plan
  servisi oturumları geri alır; bellek eşiği aşılırsa en uzun süredir dokunulmayan
  oturumlar serbest bırakılır.
- Havuz dolduğunda en eski oturum geri alınır, yani eşzamanlı ziyaretçi sayısı
  `Demo:SlotCount` ile sınırlıdır.
- Firma ve kullanıcı yönetimi demo oturumunda salt okunurdur; listeler ziyaretçinin
  kendi sandbox'ına daraltılır ve bağlantı bilgileri yanıttan çıkarılır.

API, demo'ya özel retleri gövdede bir `demoCode` ile bildirir (`session_ended`,
`write_limit`, `action_blocked`). İstemci bunu yakalayıp iletişim pop-up'ını gösterir.

| Endpoint | Açıklama |
| --- | --- |
| `POST /api/demo/start` | Anonim. Sandbox kiralar, token döner. |
| `GET /api/demo/status` | Kalan işlem hakkı ve süre. |
| `POST /api/demo/reset` | Sandbox'ı sıfırlar, yeni oturum açar. |
| `POST /api/demo/end` | Oturumu kapatır, sandbox'ı iade eder. |

## ⚙️ Yapılandırma

Gizli değerler depoda tutulmaz. Ortam değişkeni ile geçilir (`__` iç içe anahtarları
ayırır):

| Anahtar | Açıklama |
| --- | --- |
| `ConnectionStrings__SqlServer` | Ana veritabanı bağlantısı. Sandbox'lar da varsayılan olarak aynı sunucu ve kimlik bilgileriyle açılır. |
| `Jwt__SecretKey` | İmzalama anahtarı. **Production'da zorunlu**, yoksa uygulama açılmaz. Development'ta geçici bir anahtar üretilir. |
| `Cors__AllowedOrigins__0` | İzin verilen origin listesi. Boşsa kimlik bilgisi olmadan tüm origin'lere izin verilir. |
| `Database__MigrateOnStartup` | Açılışta ana veritabanı migration'larını uygular. |
| `Identity__RequireConfirmedEmail` | E-posta onayı zorunluluğu. |
| `Mail__SmtpHost` | Boşsa mailler sessizce düşürülür (SMTP sunucusu olmadan da çalışır). |
| `Seed__AdminPassword` | İlk admin kullanıcısının parolası. |
| `Demo__*` | `DemoOptions` alanları: `Enabled`, `SlotCount`, `WriteLimit`, `NudgeAfterWrites`, `IdleTimeoutMinutes`, `AbsoluteTimeoutMinutes`, `MemoryThresholdMegabytes`, `ContactUrl`. |

## 🐳 Docker ile çalıştırma

```bash
cp .env.example .env   # değerleri doldurun
docker compose up -d --build
```

Uygulama `http://localhost:8080` adresinde açılır. Caddy hem statik dosyaları sunar
hem de `/api` isteklerini API konteynerine yönlendirir, böylece tarayıcı tarafında
cross-origin çağrı olmaz.

**Sunucuya kurarken** `.env` içinde şunları değiştirin:

```
SITE_ADDRESS=demo.example.com   # alan adı yazılırsa Caddy Let's Encrypt sertifikası alır
HTTP_PORT=80
HTTPS_PORT=443
```

Alan adının A kaydı sunucunun IP'sine bakıyorsa Caddy sertifikayı ilk açılışta kendi
alır ve süresi dolmadan yeniler; 80 ve 443 portları doğrulama için dışarı açık olmalıdır.
Sertifikalar `caddy-data` volume'ünde saklanır, konteyner yeniden kurulunca kaybolmaz.

## 🪟 Plesk for Windows üzerine kurulum

Docker'sız, panel üzerinden kurulum için: **[deploy/plesk/README.md](deploy/plesk/README.md)**.
IIS altında ASP.NET Core uygulaması + Plesk'in kendi MSSQL'i ile çalışır.

İstemcinin API adresi çalışma zamanında `assets/config.json` dosyasından okunur, yani
adresi değiştirmek için yeniden derleme gerekmez:

```json
{ "apiUrl": "https://api.example.com/api" }
```

Dosya boş bırakılırsa derleme sırasındaki adres (`/api`) kullanılır.

## 💻 Lokal geliştirme

```bash
dotnet ef database update --context ApplicationDbContext \
  --project eAccountingServer/src/eAccountingServer.Infrastructure \
  --startup-project eAccountingServer/src/eAccountingServer.WebApi
```

```bash
dotnet run --project eAccountingServer/src/eAccountingServer.WebApi --launch-profile https
```

```bash
cd eAccountingClient && npm install && npm start
```

API `https://localhost:7222` (Scalar arayüzü `/scalar/v1`), istemci
`http://localhost:4200` adresinde çalışır. Development profilinde demo modu açıktır ve
ilk admin kullanıcısı `admin / 1` olarak oluşturulur.

## 📚 Kaynak

Projenin çekirdek eğitimi:
📺 _[Taner Saydam'ın Udemy profili](https://www.udemy.com/user/taner-saydam/?kw=taner+saydam&src=sac)_ ⭐⭐⭐⭐⭐ <br>
🐙 _[eMuhasebe.Udemy](https://github.com/TanerSaydam/eMuhasebe.Udemy)_

## 📬 İletişim

[ataberkkaya.com](https://ataberkkaya.com)
