using eAccountingServer.Domain.Reporting;

namespace eAccountingServer.Application.Services;

/// <summary>
/// Ekstreyi indirilebilir bir dosyaya dönüştürür. Biçimlendirme ayrıntıları
/// altyapı katmanında kalır; uygulama katmanı yalnızca hangi biçimi istediğini
/// söyler.
/// </summary>
public interface IStatementReportBuilder
{
    ReportFile Build(Statement statement, ReportFormat format);
}
