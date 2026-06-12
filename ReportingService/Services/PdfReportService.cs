using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReportingService.Interfaces;
using Shared.DTOs;

// Explicitly resolve ambiguous types
using Document = QuestPDF.Fluent.Document;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace ReportingService.Services;

public class PdfReportService : IPdfReportService
{
    public PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateDashboardPdf(
        DashboardSummaryDto dashboard,
        IEnumerable<KpiReportDto> reports)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("LifeSci360 — KPI Dashboard Report")
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);

                    col.Item().Text($"Generated: {dashboard.LastRefreshed:dd MMM yyyy HH:mm} UTC")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);

                    col.Item().LineHorizontal(1).LineColor(Colors.Blue.Lighten3);

                    // ── KPI Cards ─────────────────────────────────────────
                    col.Item().Text("Current KPI Summary").FontSize(13).Bold();
                    col.Item().Row(row =>
                    {
                        row.Spacing(8);
                        KpiCard(row.RelativeItem(), "Enrollment Rate",
                            dashboard.CurrentKpis.EnrollmentRate,
                            dashboard.Trends.EnrollmentRateChange);
                        KpiCard(row.RelativeItem(), "Sample Processing",
                            dashboard.CurrentKpis.SampleProcessingRate,
                            dashboard.Trends.SampleProcessingRateChange);
                        KpiCard(row.RelativeItem(), "Compliance Score",
                            dashboard.CurrentKpis.ComplianceScore,
                            dashboard.Trends.ComplianceScoreChange);
                        KpiCard(row.RelativeItem(), "Site Performance",
                            dashboard.CurrentKpis.SitePerformanceScore,
                            dashboard.Trends.SitePerformanceScoreChange);
                    });

                    // ── KPI Bar Chart ─────────────────────────────────────
                    col.Item().Text("KPI Comparison").FontSize(13).Bold();
                    col.Item().Element(c => KpiBarChart(c, dashboard.CurrentKpis));

                    // ── Alerts ────────────────────────────────────────────
                    if (dashboard.Alerts.Count > 0)
                    {
                        col.Item().Text("Alerts").FontSize(13).Bold();
                        foreach (var alert in dashboard.Alerts)
                        {
                            var color = alert.Level == KpiAlertLevel.Critical
                                ? Colors.Red.Lighten4
                                : Colors.Orange.Lighten4;
                            col.Item().Background(color).Padding(8)
                                .Text($"⚠ {alert.Message}").FontSize(9);
                        }
                    }

                    // ── Stats ─────────────────────────────────────────────
                    col.Item().Text("Overview").FontSize(13).Bold();
                    col.Item().Row(row =>
                    {
                        row.Spacing(8);
                        StatBox(row.RelativeItem(), "Total Reports",
                            dashboard.TotalReports.ToString());
                        StatBox(row.RelativeItem(), "Active Protocols",
                            dashboard.ActiveProtocols.ToString());
                        StatBox(row.RelativeItem(), "Enrolled Patients",
                            dashboard.TotalEnrolledPatients.ToString());
                        StatBox(row.RelativeItem(), "Samples Processed",
                            dashboard.TotalSamplesProcessed.ToString());
                    });

                    // ── Reports Table ─────────────────────────────────────
                    col.Item().Text("Recent KPI Reports").FontSize(13).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[]
                                { "Scope", "Protocol", "Generated By", "Date" })
                            {
                                h.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(6)
                                    .Text(title)
                                    .FontColor(Colors.White).Bold().FontSize(9);
                            }
                        });

                        var recentReports = reports.Take(20).ToList();
                        for (int i = 0; i < recentReports.Count; i++)
                        {
                            var r = recentReports[i];
                            var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                            table.Cell().Background(bg).Padding(5).Text(r.Scope).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(r.ProtocolTitle ?? "Global").FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(r.GeneratedByUserName).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(r.GeneratedAt.ToString("dd MMM yyyy")).FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("LifeSci360 — Confidential — Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateSingleReportPdf(KpiReportDto report)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"KPI Report — {report.Scope}")
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text($"Report ID: {report.ReportId}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm} UTC")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().LineHorizontal(1).LineColor(Colors.Blue.Lighten3);

                    col.Item().Text("Details").FontSize(13).Bold();

                    col.Item().Row(r =>
                    {
                        r.ConstantItem(160).Text("Protocol:").Bold();
                        r.RelativeItem().Text(report.ProtocolTitle ?? "Global");
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(160).Text("Scope:").Bold();
                        r.RelativeItem().Text(report.Scope);
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(160).Text("Generated By:").Bold();
                        r.RelativeItem().Text(report.GeneratedByUserName);
                    });

                    if (report.ParsedMetrics is not null)
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().Text("KPI Values").FontSize(13).Bold();

                        col.Item().Row(r =>
                        {
                            r.ConstantItem(200).Text("Enrollment Rate:").Bold();
                            r.RelativeItem().Text($"{report.ParsedMetrics.EnrollmentRate:F2}%");
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(200).Text("Sample Processing Rate:").Bold();
                            r.RelativeItem().Text($"{report.ParsedMetrics.SampleProcessingRate:F2}%");
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(200).Text("Compliance Score:").Bold();
                            r.RelativeItem().Text($"{report.ParsedMetrics.ComplianceScore:F2}%");
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(200).Text("Site Performance:").Bold();
                            r.RelativeItem().Text($"{report.ParsedMetrics.SitePerformanceScore:F2}%");
                        });

                        col.Item().PaddingTop(6).Text("KPI Comparison").FontSize(13).Bold();
                        col.Item().Element(c => KpiBarChart(c, report.ParsedMetrics));
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("LifeSci360 — Confidential — Page ");
                    t.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ComposeHeader(IContainer container) =>
        container.Row(row =>
        {
            row.RelativeItem().Text("LifeSci360")
                .FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
            row.RelativeItem().AlignRight()
                .Text("Clinical Research & Laboratory Management")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
        });

    private static void KpiCard(IContainer container, string label,
        double value, double change)
    {
        var bg = value < 50 ? Colors.Red.Lighten4
               : value < 70 ? Colors.Orange.Lighten4
               : Colors.Green.Lighten4;
        var arrow = change >= 0 ? "▲" : "▼";
        var changeColor = change >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2;

        container.Background(bg).Padding(10).Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
            c.Item().Text($"{value:F1}%").FontSize(16).Bold();
            c.Item().Text($"{arrow} {Math.Abs(change):F1}% vs prev period")
                .FontSize(7).FontColor(changeColor);
        });
    }

    private static void StatBox(IContainer container, string label, string value) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(10).Column(c =>
            {
                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                c.Item().Text(value).FontSize(14).Bold();
            });

    // ── Bar chart ───────────────────────────────────────────────────────────────
    // Draws four vertical bars (0–100 scale) using native QuestPDF layout —
    // no image library needed. Bar height is proportional to the KPI value and
    // colored with the same thresholds as the KPI cards.
    private const float ChartPlotHeight = 150f;

    private static void KpiBarChart(IContainer container, KpiMetrics kpis) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Row(row =>
        {
            row.Spacing(16);
            Bar(row.RelativeItem(), "Enrollment",   kpis.EnrollmentRate);
            Bar(row.RelativeItem(), "Sample Proc.", kpis.SampleProcessingRate);
            Bar(row.RelativeItem(), "Compliance",   kpis.ComplianceScore);
            Bar(row.RelativeItem(), "Site Perf.",   kpis.SitePerformanceScore);
        });

    private static void Bar(IContainer container, string label, double value)
    {
        var clamped   = Math.Clamp(value, 0, 100);
        var barHeight = (float)(clamped / 100.0 * ChartPlotHeight);
        var color = value < 50 ? Colors.Red.Medium
                  : value < 70 ? Colors.Orange.Medium
                  : Colors.Green.Medium;

        container.Column(c =>
        {
            // value label above the bar
            c.Item().AlignCenter().Text($"{value:F0}%").FontSize(9).Bold();

            // plot area (grey track = full 0–100 scale) with the bar pinned to the bottom
            c.Item().Height(ChartPlotHeight).Background(Colors.Grey.Lighten3).AlignBottom()
                .Height(barHeight).Background(color);

            // category label under the bar
            c.Item().PaddingTop(5).AlignCenter()
                .Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }
}