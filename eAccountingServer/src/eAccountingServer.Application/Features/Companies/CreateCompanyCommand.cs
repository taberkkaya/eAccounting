using System.Text;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.ValueObjects;
using GenericRepository;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record CreateCompanyCommand(
    string Name,
    string Address,
    Database Database,
    string TaxDepartment,
    string TaxNumber
    ) : IRequest<Result<string>>;

public sealed class CreateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ICompanyService companyService,
    ICacheService cacheService,
    IUnitOfWork unitOfWork,
    IOptions<CompanyDatabaseOptions> databaseOptions
    ) : IRequestHandler<CreateCompanyCommand, Result<string>>
{
    private readonly CompanyDatabaseOptions _database = databaseOptions.Value;

    public async Task<Result<string>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        // Vergi numarası zorunlu değil; boş bırakılmışsa tekillik aranmıyor, aksi
        // hâlde numarası olmayan ikinci firma "zaten kayıtlı" diye reddedilirdi.
        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            bool isTaxNumberExist = await companyRepository
                .AnyAsync(p => p.TaxNumber == request.TaxNumber, cancellationToken);

            if (isTaxNumberExist)
                return Result<string>.Failure("Bu vergi numarası zaten kayıtlı.");
        }

        Company company = request.Adapt<Company>();

        Result<Database> database = await ResolveDatabaseAsync(request.Database, request.Name, cancellationToken);
        if (!database.IsSuccessful)
            return Result<string>.Failure(database.ErrorMessages!);

        company.Database = database.Data!;

        try
        {
            // Kayıttan önce kuruluyor: kurulum başarısız olursa listede açılamayan
            // bir firma kalmasın.
            companyService.MigrateCompany(company);
        }
        catch (Exception exception)
        {
            return Result<string>.Failure(
                $"Firma veritabanı kurulamadı: {exception.Message}");
        }

        await companyRepository.AddAsync(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cacheService.Remove("companies");

        return "Firma eklendi ve veritabanı kuruldu.";
    }

    /// <summary>
    /// Sunucu bilgisi verilmemişse yapılandırılmış sunucuyu kullanır ve veritabanı
    /// adını firma adından türetir. Yöneticinin her firma için bağlantı bilgisi
    /// yazması gerekmiyor; başka bir sunucu kullanmak isterse alanları doldurması
    /// yeterli.
    /// </summary>
    private async Task<Result<Database>> ResolveDatabaseAsync(
        Database requested, string companyName, CancellationToken cancellationToken)
    {
        bool serverGiven = !string.IsNullOrWhiteSpace(requested.Server);

        if (!serverGiven && !_database.IsConfigured)
            return Result<Database>.Failure(
                "Veritabanı sunucusu belirtilmedi ve tanımlı bir sunucu yok. "
                + "Sunucu, veritabanı adı ve erişim bilgilerini girin.");

        string server = serverGiven ? requested.Server : _database.Server;

        string name = !string.IsNullOrWhiteSpace(requested.DatabaseName)
            ? requested.DatabaseName.Trim()
            : await UniqueNameAsync(companyName, cancellationToken);

        // Kullanıcı adı boşsa: kendi sunucumuzda tanımlı hesap, başka bir sunucuda
        // Windows kimlik doğrulaması.
        string username = !string.IsNullOrWhiteSpace(requested.Username)
            ? requested.Username
            : serverGiven ? string.Empty : _database.Username;

        string password = !string.IsNullOrWhiteSpace(requested.Password)
            ? requested.Password
            : serverGiven ? string.Empty : _database.Password;

        return new Database(server, name, username, password);
    }

    private async Task<string> UniqueNameAsync(string companyName, CancellationToken cancellationToken)
    {
        string stem = _database.NamePrefix + Slug(companyName);

        List<string> taken = await companyRepository
            .GetAll()
            .Select(p => p.Database.DatabaseName)
            .ToListAsync(cancellationToken);

        if (!taken.Contains(stem, StringComparer.OrdinalIgnoreCase)) return stem;

        for (int suffix = 2; ; suffix++)
        {
            string candidate = $"{stem}_{suffix}";
            if (!taken.Contains(candidate, StringComparer.OrdinalIgnoreCase)) return candidate;
        }
    }

    /// <summary>
    /// Firma adını veritabanı adı olarak kullanılabilir hâle getirir: Türkçe
    /// harfler karşılıklarına iner, kalan her şey alt çizgi olur.
    /// </summary>
    private static string Slug(string value)
    {
        StringBuilder builder = new();
        bool lastWasSeparator = false;

        foreach (char c in value.Trim())
        {
            // Büyük harfler büyük kalıyor: "Öz Çağrı" -> "Oz_Cagri", "oz_cagri" değil.
            char simplified = c switch
            {
                'ç' => 'c', 'Ç' => 'C',
                'ğ' => 'g', 'Ğ' => 'G',
                'ı' => 'i', 'I' => 'I',
                'i' => 'i', 'İ' => 'I',
                'ö' => 'o', 'Ö' => 'O',
                'ş' => 's', 'Ş' => 'S',
                'ü' => 'u', 'Ü' => 'U',
                _ => c
            };

            if (char.IsLetterOrDigit(simplified) && simplified < 128)
            {
                builder.Append(simplified);
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && builder.Length > 0)
            {
                builder.Append('_');
                lastWasSeparator = true;
            }
        }

        string slug = builder.ToString().Trim('_');

        return slug.Length == 0 ? "Firma" : slug[..Math.Min(slug.Length, 60)];
    }
}
