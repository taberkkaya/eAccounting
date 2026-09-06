using ClosedXML.Excel;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eAccountingServer.Infrastructure.Reporting;

/// <summary>
/// Ekstreyi Excel ve PDF olarak üretir. İki çıktı da uygulamanın arayüzüyle aynı
/// renk ve düzeni kullanır, böylece indirilen dosya ekranın devamı gibi durur.
/// </summary>
internal sealed class StatementReportBuilder : IStatementReportBuilder
{
    static StatementReportBuilder() => ReportTheme.EnsureLicense();

    public ReportFile Build(Statement statement, ReportFormat format) => format switch
    {
        ReportFormat.Pdf => new ReportFile(
            $"{statement.FileNameStem()}.pdf",
            "application/pdf",
            BuildPdf(statement)),

        _ => new ReportFile(
            $"{statement.FileNameStem()}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            BuildExcel(statement))
    };

    // --- Excel --------------------------------------------------------------

    private static byte[] BuildExcel(Statement statement)
    {
        // Excel'de para biçimi sembolü tırnak içinde ister; aksi halde biçim
        // dizesi geçersiz sayılıp hücre metne düşer.
        string money = $"#,##0.00 \"{statement.CurrencySymbol}\"";

        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.AddWorksheet("Ekstre");

        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 10;

        // başlık bloğu
        sheet.Range("A1:F1").Merge();
        sheet.Cell("A1").Value = $"{statement.AccountKind} Ekstresi";
        sheet.Cell("A1").Style
            .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Navy))
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetIndent(1);
        sheet.Row(1).Height = 30;

        sheet.Range("A2:F2").Merge();
        sheet.Cell("A2").Value =
            $"{statement.AccountName}  ·  {statement.CurrencyName}  ·  "
            + $"{statement.StartDate:dd.MM.yyyy} - {statement.EndDate:dd.MM.yyyy}";
        sheet.Cell("A2").Style
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Blue))
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetIndent(1);
        sheet.Row(2).Height = 20;

        // tablo başlığı
        const int headerRow = 4;
        string[] headers = ["#", "Tarih", "Açıklama", "Giren", "Çıkan", "Dönem Bakiyesi"];

        for (int i = 0; i < headers.Length; i++)
        {
            IXLCell cell = sheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Navy))
                .Alignment.SetHorizontal(i >= 3
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        sheet.Row(headerRow).Height = 22;

        // satırlar
        int row = headerRow + 1;
        decimal running = 0;

        foreach ((StatementLine line, int index) in statement.Lines.Select((l, i) => (l, i)))
        {
            running += line.Deposit - line.Withdrawal;

            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = line.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 3).Value = line.IsTransfer
                ? $"{line.Description} (virman)"
                : line.Description;
            sheet.Cell(row, 4).Value = line.Deposit;
            sheet.Cell(row, 5).Value = line.Withdrawal;
            sheet.Cell(row, 6).Value = running;

            sheet.Cell(row, 2).Style.DateFormat.Format = "dd.MM.yyyy";
            sheet.Range(row, 4, row, 6).Style.NumberFormat.Format = money;
            sheet.Cell(row, 4).Style.Font.SetFontColor(XLColor.FromHtml(ReportTheme.Green));
            sheet.Cell(row, 5).Style.Font.SetFontColor(XLColor.FromHtml(ReportTheme.Red));
            sheet.Cell(row, 6).Style.Font.SetBold();

            if (index % 2 == 1)
                sheet.Range(row, 1, row, 6).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Zebra));

            row++;
        }

        if (statement.Lines.Count == 0)
        {
            sheet.Range(row, 1, row, 6).Merge();
            sheet.Cell(row, 1).Value = "Seçili tarih aralığında hareket bulunamadı.";
            sheet.Cell(row, 1).Style
                .Font.SetItalic().Font.SetFontColor(XLColor.FromHtml(ReportTheme.Muted))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;
        }

        // toplam satırı
        sheet.Range(row, 1, row, 3).Merge();
        sheet.Cell(row, 1).Value = "TOPLAM";
        sheet.Cell(row, 4).Value = statement.TotalDeposit;
        sheet.Cell(row, 5).Value = statement.TotalWithdrawal;
        sheet.Cell(row, 6).Value = statement.Net;

        sheet.Range(row, 1, row, 6).Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(ReportTheme.Wash))
            .Border.SetTopBorder(XLBorderStyleValues.Medium)
            .Border.SetTopBorderColor(XLColor.FromHtml(ReportTheme.Navy));
        sheet.Range(row, 4, row, 6).Style.NumberFormat.Format = money;
        sheet.Cell(row, 1).Style.Alignment.SetIndent(1);

        // çerçeve ve ölçüler
        IXLRange table = sheet.Range(headerRow, 1, row, 6);
        table.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorderColor(XLColor.FromHtml(ReportTheme.Line))
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetOutsideBorderColor(XLColor.FromHtml(ReportTheme.Line));

        sheet.Column(1).Width = 6;
        sheet.Column(2).Width = 13;
        sheet.Column(3).Width = 46;
        sheet.Column(4).Width = 17;
        sheet.Column(5).Width = 17;
        sheet.Column(6).Width = 18;

        // Başlık her sayfada tekrarlansın; uzun ekstrelerde okumayı kolaylaştırır.
        sheet.SheetView.FreezeRows(headerRow);
        sheet.PageSetup.PrintAreas.Add(sheet.Range(1, 1, row, 6).RangeAddress.ToString()!);
        sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        sheet.PageSetup.FitToPages(1, 0);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // --- PDF ----------------------------------------------------------------

    private static byte[] BuildPdf(Statement statement)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontSize(9).FontColor(ReportTheme.Navy));

                page.Header().Element(container => ComposeHeader(container, statement));
                page.Content().PaddingVertical(14).Element(container => ComposeBody(container, statement));
                page.Footer().Element(ReportTheme.ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Statement statement)
    {
        container.Background(ReportTheme.Navy).Padding(18).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"{statement.AccountKind} Ekstresi")
                    .FontSize(17).SemiBold().FontColor(Colors.White);

                column.Item().PaddingTop(3).Text(statement.AccountName)
                    .FontSize(11).FontColor("#93C5FD");
            });

            row.ConstantItem(190).AlignRight().Column(column =>
            {
                column.Item().Text("DÖNEM")
                    .FontSize(7).SemiBold().FontColor(ReportTheme.Muted).LetterSpacing(0.12f);

                column.Item().PaddingTop(2)
                    .Text($"{statement.StartDate:dd.MM.yyyy} — {statement.EndDate:dd.MM.yyyy}")
                    .FontSize(10).FontColor(Colors.White);

                column.Item().PaddingTop(6).Text($"Para birimi: {statement.CurrencyName}")
                    .FontSize(8).FontColor("#94A3B8");
            });
        });
    }

    private static void ComposeBody(IContainer container, Statement statement)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSummary(c, statement));
            column.Item().PaddingTop(16).Element(c => ComposeTable(c, statement));
        });
    }

    private static void ComposeSummary(IContainer container, Statement statement)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => SummaryCard(
                c, "GİREN", ReportTheme.Money(statement.TotalDeposit, statement.CurrencySymbol), ReportTheme.Green));

            row.ConstantItem(10);

            row.RelativeItem().Element(c => SummaryCard(
                c, "ÇIKAN", ReportTheme.Money(statement.TotalWithdrawal, statement.CurrencySymbol), ReportTheme.Red));

            row.ConstantItem(10);

            row.RelativeItem().Element(c => SummaryCard(
                c, "DÖNEM NETİ", ReportTheme.Money(statement.Net, statement.CurrencySymbol),
                statement.Net < 0 ? ReportTheme.Red : ReportTheme.Blue));

            row.ConstantItem(10);

            row.RelativeItem().Element(c => SummaryCard(
                c, "HAREKET", statement.Lines.Count.ToString(), ReportTheme.Navy));
        });
    }

    private static void SummaryCard(IContainer container, string label, string value, string color)
    {
        container
            .Border(1).BorderColor(ReportTheme.Line)
            .Background(ReportTheme.Zebra)
            .PaddingVertical(9).PaddingHorizontal(11)
            .Column(column =>
            {
                column.Item().Text(label).FontSize(7).SemiBold().FontColor(ReportTheme.Muted);
                column.Item().PaddingTop(3).Text(value).FontSize(11).SemiBold().FontColor(color);
            });
    }

    private static void ComposeTable(IContainer container, Statement statement)
    {
        if (statement.Lines.Count == 0)
        {
            container.Border(1).BorderColor(ReportTheme.Line).Padding(30).AlignCenter()
                .Text("Seçili tarih aralığında hareket bulunamadı.")
                .FontSize(9).Italic().FontColor(ReportTheme.Muted);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(58);
                columns.RelativeColumn();
                columns.ConstantColumn(78);
                columns.ConstantColumn(78);
                columns.ConstantColumn(84);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");
                HeaderCell(header.Cell(), "Tarih");
                HeaderCell(header.Cell(), "Açıklama");
                HeaderCell(header.Cell(), "Giren", right: true);
                HeaderCell(header.Cell(), "Çıkan", right: true);
                HeaderCell(header.Cell(), "Dönem Bakiyesi", right: true);
            });

            decimal running = 0;
            int index = 0;

            foreach (StatementLine line in statement.Lines)
            {
                running += line.Deposit - line.Withdrawal;
                string background = index % 2 == 1 ? ReportTheme.Zebra : Colors.White;

                BodyCell(table.Cell(), background).Text((index + 1).ToString()).FontColor(ReportTheme.Muted);
                BodyCell(table.Cell(), background).Text(line.Date.ToString("dd.MM.yyyy"));

                BodyCell(table.Cell(), background).Text(text =>
                {
                    text.Span(line.Description);

                    if (line.IsTransfer)
                        text.Span("  · virman").FontSize(7).FontColor(ReportTheme.Muted);
                });

                MoneyCell(table.Cell(), background, line.Deposit, statement.CurrencySymbol,
                    line.Deposit > 0 ? ReportTheme.Green : ReportTheme.Muted);

                MoneyCell(table.Cell(), background, line.Withdrawal, statement.CurrencySymbol,
                    line.Withdrawal > 0 ? ReportTheme.Red : ReportTheme.Muted);

                BodyCell(table.Cell(), background).AlignRight()
                    .Text(ReportTheme.Money(running, statement.CurrencySymbol))
                    .SemiBold().FontColor(running < 0 ? ReportTheme.Red : ReportTheme.Navy);

                index++;
            }

            // toplam satırı
            table.Cell().ColumnSpan(3).Background(ReportTheme.Wash)
                .BorderTop(1).BorderColor(ReportTheme.Navy)
                .PaddingVertical(7).PaddingHorizontal(6)
                .Text("TOPLAM").SemiBold();

            TotalCell(table.Cell(), statement.TotalDeposit, statement.CurrencySymbol, ReportTheme.Green);
            TotalCell(table.Cell(), statement.TotalWithdrawal, statement.CurrencySymbol, ReportTheme.Red);
            TotalCell(table.Cell(), statement.Net, statement.CurrencySymbol,
                statement.Net < 0 ? ReportTheme.Red : ReportTheme.Navy);
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

    private static void TotalCell(IContainer container, decimal amount, string symbol, string color)
    {
        container.Background(ReportTheme.Wash)
            .BorderTop(1).BorderColor(ReportTheme.Navy)
            .PaddingVertical(7).PaddingHorizontal(6)
            .AlignRight()
            .Text(ReportTheme.Money(amount, symbol)).SemiBold().FontColor(color);
    }
}
