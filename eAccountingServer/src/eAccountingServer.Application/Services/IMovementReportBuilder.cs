using eAccountingServer.Domain.Reporting;

namespace eAccountingServer.Application.Services;

/// <summary>
/// Filtrelenmiş hareket listesini indirilebilir bir dosyaya dönüştürür.
/// Ekstreden ayrı duruyor çünkü rapor birden çok hesabı ve para birimini
/// kapsıyor; iki çıktının sütunları ve toplamları aynı değil.
/// </summary>
public interface IMovementReportBuilder
{
    ReportFile Build(MovementReport report, ReportFormat format);
}
