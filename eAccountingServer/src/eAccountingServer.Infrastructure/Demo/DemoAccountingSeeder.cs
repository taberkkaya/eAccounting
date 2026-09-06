using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Infrastructure.Context;

namespace eAccountingServer.Infrastructure.Demo;

/// <summary>
/// Sandbox'a cari, ürün ve fatura tarafını da doldurur.
///
/// Kasa/banka verisiyle yetinmek, ziyaretçiyi ön muhasebenin asıl ekranlarına
/// boş olarak sokuyordu: cari listesi, fatura listesi ve yaşlandırma raporu
/// gösterecek bir şey bulamıyordu. Buradaki kayıtlar elle yazılmış bakiyeler
/// değil; bakiyeler faturalardan ve tahsilatlardan hesaplanıyor, böylece demo
/// verisi de uygulamanın kendi kurallarına uyuyor.
/// </summary>
internal static class DemoAccountingSeeder
{
    private sealed record ContactSeed(
        string Name, ContactType Type, string? TaxNumber, string? TaxOffice,
        string? Phone, string? Email, decimal Opening);

    private sealed record ProductSeed(
        string Name, string? Code, string Unit, bool IsService,
        decimal Purchase, decimal Sale, int Vat, decimal Stock, decimal Critical);

    private sealed record LineSeed(string Product, decimal Quantity, decimal Discount = 0);

    /// <param name="PaidRatio">0 hiç ödenmedi, 1 tamamı; arası kısmi.</param>
    private sealed record InvoiceSeed(
        InvoiceType Type, string Contact, int DaysAgo, int TermDays,
        LineSeed[] Lines, decimal PaidRatio = 0, string? PaidInto = null);

    public static async Task SeedAsync(
        CompanyDbContext context,
        Guid demoUserId,
        IReadOnlyList<CashRegister> cashRegisters,
        IReadOnlyList<Bank> banks,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateOnly today = DateOnly.FromDateTime(now.DateTime);

        Dictionary<string, Contact> contacts = BuildContacts(demoUserId, now, today);
        Dictionary<string, Product> products = BuildProducts(demoUserId, now);

        await context.Contacts.AddRangeAsync(contacts.Values, cancellationToken);
        await context.Products.AddRangeAsync(products.Values, cancellationToken);

        // Açılış bakiyeleri ve açılış stokları da birer hareket: demoda da
        // "bu rakam nereden geldi" sorusunun cevabı olsun.
        foreach (Contact contact in contacts.Values.Where(c => c.DebitAmount != 0 || c.CreditAmount != 0))
            await context.ContactTransactions.AddAsync(new ContactTransaction
            {
                ContactId = contact.Id,
                Date = today.AddDays(-95),
                Description = "Açılış bakiyesi",
                Kind = ContactTransactionKind.Opening,
                DebitAmount = contact.DebitAmount,
                CreditAmount = contact.CreditAmount,
                CreatedAt = now.AddDays(-95),
                CreatedBy = demoUserId
            }, cancellationToken);

        foreach (Product product in products.Values.Where(p => !p.IsService && p.StockQuantity > 0))
            await context.StockTransactions.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                Date = today.AddDays(-95),
                Direction = StockDirection.In,
                Quantity = product.StockQuantity,
                UnitPrice = product.PurchasePrice,
                Description = "Açılış stoğu",
                CreatedAt = now.AddDays(-95),
                CreatedBy = demoUserId
            }, cancellationToken);

        CashRegister tlCash = cashRegisters.First(c => c.CurrencyType.Value == CurrencyTypeEnum.TL.Value);
        Bank tlBank = banks.First(b => b.CurrencyType.Value == CurrencyTypeEnum.TL.Value);

        Numbering numbering = new();

        foreach (InvoiceSeed seed in Invoices())
            await PostInvoiceAsync(
                context, seed, contacts, products, tlCash, tlBank,
                numbering, demoUserId, now, today, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    // --- kayıtlar -----------------------------------------------------------

    private static Dictionary<string, Contact> BuildContacts(
        Guid demoUserId, DateTimeOffset now, DateOnly today)
    {
        ContactSeed[] seeds =
        [
            new("Marmara Gıda San. Tic. A.Ş.", ContactType.Customer, "3820574916", "Kadıköy",
                "0216 444 12 12", "muhasebe@marmaragida.com.tr", 0),
            new("Ege Lojistik Ltd. Şti.", ContactType.Customer, "6194037285", "Konak",
                "0232 333 55 44", "finans@egelojistik.com", 18_400m),
            new("Deniz Tekstil", ContactType.Customer, "27584019364", "Bakırköy",
                "0212 555 67 89", null, 0),
            new("Anadolu Yapı Malzemeleri", ContactType.Both, "7409263851", "Ümraniye",
                "0216 700 80 90", "info@anadoluyapi.com", 0),
            new("XYZ Ambalaj A.Ş.", ContactType.Supplier, "5063829174", "Şişli",
                "0212 210 30 40", "siparis@xyzambalaj.com", -24_600m),
            new("Nordic Supplies", ContactType.Supplier, null, null,
                "+46 8 123 4567", "orders@nordicsupplies.se", 0),
        ];

        return seeds.ToDictionary(
            seed => seed.Name,
            seed => new Contact
            {
                Name = seed.Name,
                Type = seed.Type,
                TaxNumber = seed.TaxNumber,
                TaxOffice = seed.TaxOffice,
                Phone = seed.Phone,
                Email = seed.Email,
                CurrencyType = CurrencyTypeEnum.TL,
                DebitAmount = seed.Opening > 0 ? seed.Opening : 0,
                CreditAmount = seed.Opening < 0 ? -seed.Opening : 0,
                CreatedAt = now.AddDays(-95),
                CreatedBy = demoUserId
            });
    }

    private static Dictionary<string, Product> BuildProducts(Guid demoUserId, DateTimeOffset now)
    {
        ProductSeed[] seeds =
        [
            new("Oluklu Mukavva Koli 40x30x30", "KOL-403030", "Adet", false, 18.50m, 32m, 20, 1_250m, 300m),
            new("Streç Film 50cm", "STR-50", "Rulo", false, 145m, 240m, 20, 84m, 40m),
            new("Paletli Ambalaj Bandı", "BND-48", "Koli", false, 320m, 520m, 20, 26m, 30m),
            new("Baskılı Etiket (1000'lik)", "ETK-1000", "Paket", false, 410m, 690m, 20, 62m, 25m),
            new("Depo Danışmanlığı", null, "Saat", true, 0m, 1_850m, 20, 0m, 0m),
            new("Kurulum ve Eğitim", null, "Gün", true, 0m, 6_500m, 20, 0m, 0m),
        ];

        return seeds.ToDictionary(
            seed => seed.Name,
            seed => new Product
            {
                Name = seed.Name,
                Code = seed.Code,
                Unit = seed.Unit,
                IsService = seed.IsService,
                PurchasePrice = seed.Purchase,
                SalePrice = seed.Sale,
                VatRate = seed.Vat,
                CurrencyType = CurrencyTypeEnum.TL,
                StockQuantity = seed.Stock,
                CriticalStock = seed.Critical,
                CreatedAt = now.AddDays(-95),
                CreatedBy = demoUserId
            });
    }

    /// <summary>
    /// Faturalar bilerek karışık: kapanmış, kısmi ödenmiş ve vadesi geçmiş
    /// olanlar bir arada. Yaşlandırma raporu ve "gecikmiş" filtresi ancak
    /// böyle bir tabloda anlamlı görünüyor.
    /// </summary>
    private static InvoiceSeed[] Invoices() =>
    [
        new(InvoiceType.Sales, "Marmara Gıda San. Tic. A.Ş.", 84, 30,
            [new("Oluklu Mukavva Koli 40x30x30", 400), new("Streç Film 50cm", 20)],
            PaidRatio: 1m, PaidInto: "banka"),

        new(InvoiceType.Sales, "Ege Lojistik Ltd. Şti.", 63, 30,
            [new("Paletli Ambalaj Bandı", 12, 5), new("Depo Danışmanlığı", 8)],
            PaidRatio: 0.4m, PaidInto: "banka"),

        new(InvoiceType.Sales, "Deniz Tekstil", 47, 15,
            [new("Baskılı Etiket (1000'lik)", 30)]),

        new(InvoiceType.Sales, "Marmara Gıda San. Tic. A.Ş.", 22, 45,
            [new("Oluklu Mukavva Koli 40x30x30", 250), new("Kurulum ve Eğitim", 1)]),

        new(InvoiceType.Sales, "Anadolu Yapı Malzemeleri", 9, 30,
            [new("Streç Film 50cm", 15), new("Paletli Ambalaj Bandı", 6)],
            PaidRatio: 1m, PaidInto: "kasa"),

        new(InvoiceType.Sales, "Deniz Tekstil", 3, 21,
            [new("Baskılı Etiket (1000'lik)", 12), new("Depo Danışmanlığı", 4)]),

        new(InvoiceType.Purchase, "XYZ Ambalaj A.Ş.", 71, 30,
            [new("Oluklu Mukavva Koli 40x30x30", 1_000), new("Streç Film 50cm", 60)],
            PaidRatio: 1m, PaidInto: "banka"),

        new(InvoiceType.Purchase, "Nordic Supplies", 34, 45,
            [new("Paletli Ambalaj Bandı", 40), new("Baskılı Etiket (1000'lik)", 80)]),

        new(InvoiceType.Purchase, "Anadolu Yapı Malzemeleri", 12, 30,
            [new("Streç Film 50cm", 25)]),
    ];

    // --- işleme -------------------------------------------------------------

    /// <summary>
    /// Bir faturayı ve varsa tahsilatını yazar: satırlar, cari hareketi, stok
    /// hareketi ve kasa/banka hareketi. Uygulamanın çalışma anındaki yolunun
    /// aynısı, yalnızca tek seferde ve bağlam üstünden.
    /// </summary>
    private static async Task PostInvoiceAsync(
        CompanyDbContext context,
        InvoiceSeed seed,
        Dictionary<string, Contact> contacts,
        Dictionary<string, Product> products,
        CashRegister cash,
        Bank bank,
        Numbering numbering,
        Guid demoUserId,
        DateTimeOffset now,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        Contact contact = contacts[seed.Contact];
        DateOnly date = today.AddDays(-seed.DaysAgo);
        DateTimeOffset stamp = now.AddDays(-seed.DaysAgo);
        bool isSales = seed.Type == InvoiceType.Sales;

        Invoice invoice = new()
        {
            Type = seed.Type,
            Number = numbering.Next(seed.Type, date),
            Date = date,
            DueDate = date.AddDays(seed.TermDays),
            ContactId = contact.Id,
            CurrencyType = CurrencyTypeEnum.TL,
            Status = InvoiceStatus.Approved,
            CreatedAt = stamp,
            CreatedBy = demoUserId,
            Lines = []
        };

        decimal subTotal = 0, vatTotal = 0, discountTotal = 0;

        foreach (LineSeed line in seed.Lines)
        {
            Product product = products[line.Product];
            decimal unitPrice = isSales ? product.SalePrice : product.PurchasePrice;

            decimal gross = Round(line.Quantity * unitPrice);
            decimal discount = Round(gross * line.Discount / 100m);
            decimal net = gross - discount;
            decimal vat = Round(net * product.VatRate / 100m);

            subTotal += net;
            vatTotal += vat;
            discountTotal += discount;

            invoice.Lines.Add(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                ProductId = product.Id,
                Description = product.Name,
                Unit = product.Unit,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                DiscountRate = line.Discount,
                VatRate = product.VatRate,
                LineTotal = net,
                VatAmount = vat,
                CreatedAt = stamp,
                CreatedBy = demoUserId
            });

            if (product.IsService) continue;

            product.StockQuantity += isSales ? -line.Quantity : line.Quantity;

            await context.StockTransactions.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                Date = date,
                Direction = isSales ? StockDirection.Out : StockDirection.In,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                Description = $"{(isSales ? "Satış" : "Alış")} Faturası {invoice.Number}",
                InvoiceId = invoice.Id,
                CreatedAt = stamp,
                CreatedBy = demoUserId
            }, cancellationToken);
        }

        invoice.SubTotal = Round(subTotal);
        invoice.DiscountTotal = Round(discountTotal);
        invoice.VatTotal = Round(vatTotal);
        invoice.GrandTotal = Round(subTotal + vatTotal);

        await context.Invoices.AddAsync(invoice, cancellationToken);

        contact.DebitAmount += isSales ? invoice.GrandTotal : 0;
        contact.CreditAmount += isSales ? 0 : invoice.GrandTotal;

        await context.ContactTransactions.AddAsync(new ContactTransaction
        {
            ContactId = contact.Id,
            Date = date,
            Description = $"{(isSales ? "Satış" : "Alış")} Faturası {invoice.Number}",
            Kind = ContactTransactionKind.Invoice,
            DebitAmount = isSales ? invoice.GrandTotal : 0,
            CreditAmount = isSales ? 0 : invoice.GrandTotal,
            InvoiceId = invoice.Id,
            CreatedAt = stamp,
            CreatedBy = demoUserId
        }, cancellationToken);

        if (seed.PaidRatio <= 0) return;

        decimal paid = Round(invoice.GrandTotal * seed.PaidRatio);
        DateOnly paidDate = date.AddDays(Math.Min(seed.TermDays, seed.DaysAgo));
        DateTimeOffset paidStamp = stamp.AddDays(Math.Min(seed.TermDays, seed.DaysAgo));

        Guid accountId;
        AccountKind accountKind;

        if (seed.PaidInto == "kasa")
        {
            accountKind = AccountKind.CashRegister;
            accountId = cash.Id;
            cash.DepositAmount += isSales ? paid : 0;
            cash.WithdrawalAmount += isSales ? 0 : paid;

            // DbSet üzerinden ekleniyor, gezinme özelliğinden değil: kimlikler
            // nesne kurulurken atandığı için EF, izlenen bir üstten ulaşılan
            // yeni satırı "zaten var" sanıp güncellemeye çalışıyor ve sıfır
            // satır etkileyen bir UPDATE'e düşüyor.
            await context.CashRegisterDetails.AddAsync(new CashRegisterDetail
            {
                CashRegisterId = cash.Id,
                Date = paidDate,
                Description = $"{(isSales ? "Tahsilat" : "Ödeme")} - {invoice.Number}",
                DepositAmount = isSales ? paid : 0,
                WithdrawalAmount = isSales ? 0 : paid,
                ContactId = contact.Id,
                CreatedAt = paidStamp,
                CreatedBy = demoUserId
            }, cancellationToken);
        }
        else
        {
            accountKind = AccountKind.Bank;
            accountId = bank.Id;
            bank.DepositAmount += isSales ? paid : 0;
            bank.WithdrawalAmount += isSales ? 0 : paid;

            await context.BankDetails.AddAsync(new BankDetail
            {
                BankId = bank.Id,
                Date = paidDate,
                Description = $"{(isSales ? "Tahsilat" : "Ödeme")} - {invoice.Number}",
                DepositAmount = isSales ? paid : 0,
                WithdrawalAmount = isSales ? 0 : paid,
                ContactId = contact.Id,
                CreatedAt = paidStamp,
                CreatedBy = demoUserId
            }, cancellationToken);
        }

        contact.DebitAmount += isSales ? 0 : paid;
        contact.CreditAmount += isSales ? paid : 0;

        await context.ContactTransactions.AddAsync(new ContactTransaction
        {
            ContactId = contact.Id,
            Date = paidDate,
            Description = $"{(isSales ? "Tahsilat" : "Ödeme")} - {invoice.Number}",
            Kind = isSales ? ContactTransactionKind.Collection : ContactTransactionKind.Payment,
            DebitAmount = isSales ? 0 : paid,
            CreditAmount = isSales ? paid : 0,
            InvoiceId = invoice.Id,
            AccountKind = accountKind,
            AccountId = accountId,
            CreatedAt = paidStamp,
            CreatedBy = demoUserId
        }, cancellationToken);

        invoice.PaidAmount = paid;
        invoice.Status = paid >= invoice.GrandTotal
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;
    }

    /// <summary>
    /// Fatura numarası sayacı. Statik olsaydı iki sandbox aynı anda
    /// sıfırlandığında numaralar birbirine karışırdı; her tohumlama kendi
    /// sayacını taşıyor.
    /// </summary>
    private sealed class Numbering
    {
        private int _sales;
        private int _purchase;

        public string Next(InvoiceType type, DateOnly date)
        {
            int next = type == InvoiceType.Sales ? ++_sales : ++_purchase;

            return $"{(type == InvoiceType.Sales ? "SF" : "AF")}{date.Year}{next:D6}";
        }
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
