using ClosedXML.Excel;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eAccountingServer.Infrastructure.Reporting;

/// <summary>
/// Hareket listesini Excel ve PDF olarak üretir. Ekstre ile aynı görünümü
/// kullanır; ayrıldığı yer, listenin birden çok hesabı kapsayabilmesi.
/// </summary>
internal sealed class MovementReportBuilder : IMovementReportBuilder
{
    private const int Columns = 8;

    static MovementReportBuilder() => ReportTheme.EnsureLicense();

    public ReportFile Build(MovementReport report, ReportFormat format) => format switch
    {
        ReportFormat.Pdf => new ReportFile(
            $"{report.FileNameStem()}.pdf",
            "application/pdf",
            BuildPdf(report)),

        _ => new ReportFile(
            $"{report.FileNameStem()}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            BuildExcel(report))
    };

    /// <summary>Uygulanan filtreler tek satırda; hiç filtre yoksa boş döner.</summary>
    private static string FilterText(MovementReport report) =>
        string.Join("   ·   ", report.Filters.Select(f => $"{f.Label}: {f.Value}"));

    // --- Excel --------------------------------------------------------------

    private static byte[] BuildExcel(MovementReport report)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.AddWorksheet("Hareketler");

        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 10;

        // başlık bloğu
        sheet.Range(1, 1, 1, Columns).Merge();
        sheet.Cell("A1").Value = "Hareket Listesi";
        sheet.Cell("A1").Style
            .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Navy))
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetIndent(1);
        sheet.Row(1).Height = 30;

        sheet.Range(2, 1, 2, Columns).Merge();
        sheet.Cell("A2").Value = report.PeriodText();
        sheet.Cell("A2").Style
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Blue))
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetIndent(1);
        sheet.Row(2).Height = 20;

        int row = 3;
        string filters = FilterText(report);

        if (filters.Length > 0)
        {
            sheet.Range(row, 1, row, Columns).Merge();
            sheet.Cell(row, 1).Value = filters;
            sheet.Cell(row, 1).Style
                .Font.SetFontColor(XLColor.FromHtml(ReportTheme.Muted))
                .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Zebra))
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetIndent(1);
            sheet.Row(row).Height = 18;
            row++;
        }

        // tablo başlığı
        int headerRow = row + 1;
        string[] headers =
            ["#", "Tarih", "Açıklama", "Kalem", "Hesap", "Tür", "Giren", "Çıkan"];

        for (int i = 0; i < headers.Length; i++)
        {
            IXLCell cell = sheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Navy))
                .Alignment.SetHorizontal(i >= 6
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        sheet.Row(headerRow).Height = 22;

        // satırlar
        row = headerRow + 1;

        foreach ((MovementReportLine line, int index) in report.Lines.Select((l, i) => (l, i)))
        {
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = line.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 3).Value = line.IsTransfer
                ? $"{line.Description} (virman)"
                : line.Description;
            sheet.Cell(row, 4).Value = line.CategoryName ?? string.Empty;
            sheet.Cell(row, 5).Value = line.AccountName;
            sheet.Cell(row, 6).Value = line.AccountKind;
            sheet.Cell(row, 7).Value = line.Deposit;
            sheet.Cell(row, 8).Value = line.Withdrawal;

            sheet.Cell(row, 2).Style.DateFormat.Format = "dd.MM.yyyy";

            // Para birimi satırdan satıra değişebildiği için biçim tek tek
            // veriliyor; hücreler yine sayı, böylece Excel'de toplanabiliyor.
            sheet.Range(row, 7, row, 8).Style.NumberFormat.Format =
                MoneyFormat(line.CurrencySymbol);
            sheet.Cell(row, 7).Style.Font.SetFontColor(XLColor.FromHtml(ReportTheme.Green));
            sheet.Cell(row, 8).Style.Font.SetFontColor(XLColor.FromHtml(ReportTheme.Red));

            if (index % 2 == 1)
                sheet.Range(row, 1, row, Columns).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Zebra));

            row++;
        }

        if (report.Lines.Count == 0)
        {
            sheet.Range(row, 1, row, Columns).Merge();
            sheet.Cell(row, 1).Value = "Seçili filtrelerle hareket bulunamadı.";
            sheet.Cell(row, 1).Style
                .Font.SetItalic().Font.SetFontColor(XLColor.FromHtml(ReportTheme.Muted))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;
        }

        int firstTotalRow = row;

        // Toplamlar para birimi başına ayrı: farklı birimleri toplamak yanlış
        // olurdu, tek bir "genel toplam" satırı da onu yapmış olurdu.
        foreach (MovementReportTotal total in report.Totals)
        {
            sheet.Range(row, 1, row, 6).Merge();
            sheet.Cell(row, 1).Value = $"TOPLAM · {total.CurrencyName}";
            sheet.Cell(row, 7).Value = total.Deposit;
            sheet.Cell(row, 8).Value = total.Withdrawal;

            sheet.Range(row, 1, row, Columns).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Wash));
            sheet.Range(row, 7, row, 8).Style.NumberFormat.Format =
                MoneyFormat(total.CurrencySymbol);
            sheet.Cell(row, 1).Style.Alignment.SetIndent(1);

            if (row == firstTotalRow)
                sheet.Range(row, 1, row, Columns).Style
                    .Border.SetTopBorder(XLBorderStyleValues.Medium)
                    .Border.SetTopBorderColor(XLColor.FromHtml(ReportTheme.Navy));

            row++;

            // net, toplamın hemen altında
            sheet.Range(row, 1, row, 6).Merge();
            sheet.Cell(row, 1).Value = $"NET · {total.CurrencyName}";
            sheet.Range(row, 7, row, 8).Merge();
            sheet.Cell(row, 7).Value = total.Net;

            sheet.Range(row, 1, row, Columns).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Wash));
            sheet.Cell(row, 7).Style.NumberFormat.Format = MoneyFormat(total.CurrencySymbol);
            sheet.Cell(row, 7).Style.Font.SetFontColor(
                XLColor.FromHtml(total.Net < 0 ? ReportTheme.Red : ReportTheme.Navy));
            sheet.Cell(row, 1).Style.Alignment.SetIndent(1);

            row++;
        }

        // çerçeve ve ölçüler
        IXLRange table = sheet.Range(headerRow, 1, row - 1, Columns);
        table.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorderColor(XLColor.FromHtml(ReportTheme.Line))
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetOutsideBorderColor(XLColor.FromHtml(ReportTheme.Line));

        sheet.Column(1).Width = 6;
        sheet.Column(2).Width = 13;
        sheet.Column(3).Width = 40;
        sheet.Column(4).Width = 18;
        sheet.Column(5).Width = 22;
        sheet.Column(6).Width = 9;
        sheet.Column(7).Width = 16;
        sheet.Column(8).Width = 16;

        // Başlık her sayfada tekrarlansın; uzun listelerde okumayı kolaylaştırır.
        sheet.SheetView.FreezeRows(headerRow);
        sheet.PageSetup.PrintAreas.Add(
            sheet.Range(1, 1, row - 1, Columns).RangeAddress.ToString()!);
        sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Excel para biçiminde sembolü tırnak içinde ister; aksi halde biçim dizesi
    /// geçersiz sayılıp hücre metne düşer.
    /// </summary>
    private static string MoneyFormat(string symbol) =>
        symbol.Length == 0 ? "#,##0.00" : $"#,##0.00 \"{symbol}\"";

    // --- PDF ----------------------------------------------------------------

    private static byte[] BuildPdf(MovementReport report)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                // Yatay: ekstrenin altı sütununa karşılık burada hesap ve kalem
                // de var, dikey sayfada açıklama okunmaz hale geliyordu.
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontSize(9).FontColor(ReportTheme.Navy));

                page.Header().Element(container => ComposeHeader(container, report));
                page.Content().PaddingVertical(14)
                    .Element(container => ComposeBody(container, report));
                page.Footer().Element(ReportTheme.ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, MovementReport report)
    {
        container.Background(ReportTheme.Navy).Padding(18).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Hareket Listesi")
                    .FontSize(17).SemiBold().FontColor(Colors.White);

                column.Item().PaddingTop(3).Text($"{report.Lines.Count} hareket")
                    .FontSize(11).FontColor("#93C5FD");
            });

            row.ConstantItem(240).AlignRight().Column(column =>
            {
                column.Item().Text("DÖNEM")
                    .FontSize(7).SemiBold().FontColor(ReportTheme.Muted).LetterSpacing(0.12f);

                column.Item().PaddingTop(2).Text(report.PeriodText())
                    .FontSize(10).FontColor(Colors.White);
            });
        });
    }

    private static void ComposeBody(IContainer container, MovementReport report)
    {
        container.Column(column =>
        {
            string filters = FilterText(report);

            if (filters.Length > 0)
                column.Item().PaddingBottom(10)
                    .Border(1).BorderColor(ReportTheme.Line).Background(ReportTheme.Zebra)
                    .PaddingVertical(6).PaddingHorizontal(10)
                    .Text(text =>
                    {
                        text.Span("Filtreler   ").FontSize(7).SemiBold()
                            .FontColor(ReportTheme.Muted);
                        text.Span(filters).FontSize(8.5f);
                    });

            foreach (MovementReportTotal total in report.Totals)
                column.Item().PaddingBottom(6).Element(c => SummaryRow(c, total));

            column.Item().PaddingTop(8).Element(c => ComposeTable(c, report));
        });
    }

    /// <summary>
    /// Para birimi başına bir toplam şeridi. Kartları yan yana dizmek yerine alt
    /// alta koymak, birim sayısı arttıkça taşmayı engelliyor.
    /// </summary>
    private static void SummaryRow(IContainer container, MovementReportTotal total)
    {
        container
            .Border(1).BorderColor(ReportTheme.Line).Background(ReportTheme.Zebra)
            .PaddingVertical(8).PaddingHorizontal(11)
            .Row(row =>
            {
                row.ConstantItem(70).AlignMiddle().Text(total.CurrencyName)
                    .FontSize(10).SemiBold().FontColor(ReportTheme.Blue);

                row.RelativeItem().Element(c => SummaryValue(
                    c, "GİREN", ReportTheme.Money(total.Deposit, total.CurrencySymbol),
                    ReportTheme.Green));

                row.RelativeItem().Element(c => SummaryValue(
                    c, "ÇIKAN", ReportTheme.Money(total.Withdrawal, total.CurrencySymbol),
                    ReportTheme.Red));

                row.RelativeItem().Element(c => SummaryValue(
                    c, "NET", ReportTheme.Money(total.Net, total.CurrencySymbol),
                    total.Net < 0 ? ReportTheme.Red : ReportTheme.Navy));
            });
    }

    private static void SummaryValue(IContainer container, string label, string value, string color)
    {
        container.Row(row =>
        {
            row.ConstantItem(40).AlignMiddle()
                .Text(label).FontSize(7).SemiBold().FontColor(ReportTheme.Muted);

            row.RelativeItem().AlignMiddle()
                .Text(value).FontSize(10).SemiBold().FontColor(color);
        });
    }

    private static void ComposeTable(IContainer container, MovementReport report)
    {
        if (report.Lines.Count == 0)
        {
            container.Border(1).BorderColor(ReportTheme.Line).Padding(30).AlignCenter()
                .Text("Seçili filtrelerle hareket bulunamadı.")
                .FontSize(9).Italic().FontColor(ReportTheme.Muted);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(58);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(92);
                columns.ConstantColumn(92);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");
                HeaderCell(header.Cell(), "Tarih");
                HeaderCell(header.Cell(), "Açıklama");
                HeaderCell(header.Cell(), "Kalem");
                HeaderCell(header.Cell(), "Hesap");
                HeaderCell(header.Cell(), "Giren", right: true);
                HeaderCell(header.Cell(), "Çıkan", right: true);
            });

            int index = 0;

            foreach (MovementReportLine line in report.Lines)
            {
                string background = index % 2 == 1 ? ReportTheme.Zebra : Colors.White;

                BodyCell(table.Cell(), background)
                    .Text((index + 1).ToString()).FontColor(ReportTheme.Muted);

                BodyCell(table.Cell(), background).Text(line.Date.ToString("dd.MM.yyyy"));

                BodyCell(table.Cell(), background).Text(text =>
                {
                    text.Span(line.Description);

                    if (line.IsTransfer)
                        text.Span("  · virman").FontSize(7).FontColor(ReportTheme.Muted);
                });

                BodyCell(table.Cell(), background)
                    .Text(line.CategoryName ?? "—")
                    .FontColor(line.CategoryName is null ? ReportTheme.Muted : ReportTheme.Navy);

                BodyCell(table.Cell(), background).Text(text =>
                {
                    text.Span(line.AccountName);
                    text.Span($"  · {line.AccountKind}").FontSize(7)
                        .FontColor(ReportTheme.Muted);
                });

                MoneyCell(table.Cell(), background, line.Deposit, line.CurrencySymbol,
                    line.Deposit > 0 ? ReportTheme.Green : ReportTheme.Muted);

                MoneyCell(table.Cell(), background, line.Withdrawal, line.CurrencySymbol,
                    line.Withdrawal > 0 ? ReportTheme.Red : ReportTheme.Muted);

                index++;
            }
        });
    }

    private static void HeaderCell(IContainer container, string text, bool right = false)
    {
        IContainer cell = container
            .Background(ReportTheme.Navy)
            .PaddingVertical(7).PaddingHorizontal(6);

        if (right) cell = cell.AlignRight();

        cell.Text(text).FontSize(7.5f).SemiBold().FontColor(Colors.White);
    }

    private static IContainer BodyCell(IContainer container, string background) =>
        container
            .Background(background)
            .BorderBottom(0.5f).BorderColor(ReportTheme.Line)
            .PaddingVertical(5).PaddingHorizontal(6);

    private static void MoneyCell(
        IContainer container, string background, decimal amount, string symbol, string color)
    {
        BodyCell(container, background).AlignRight()
            .Text(amount == 0 ? "—" : ReportTheme.Money(amount, symbol))
            .FontColor(color);
    }
}
