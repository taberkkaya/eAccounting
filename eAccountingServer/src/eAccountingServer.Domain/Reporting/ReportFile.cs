namespace eAccountingServer.Domain.Reporting;

/// <summary>İndirilmeye hazır bir rapor dosyası.</summary>
public sealed record ReportFile(string FileName, string ContentType, byte[] Content);

/// <summary>İstenen çıktı biçimi.</summary>
public enum ReportFormat
{
    Excel = 0,
    Pdf = 1
}
