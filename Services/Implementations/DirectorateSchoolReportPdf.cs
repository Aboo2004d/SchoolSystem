using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SchoolSystem.Services.Implementations;

public sealed record DirectorateSchoolPdfData(
    string Name, bool IsActive, string? Status, string? Gender, string? Stage,
    int? MinClass, int? MaxClass, int Managers, int Teachers, int Students,
    int ClassesCount, int SubjectsCount, int AttendanceTotal, int Present,
    int Absent, int Excused, double? AttendanceRate, double? AverageGrade,
    IReadOnlyList<DirectorateGradeBucket> GradeDistribution,
    IReadOnlyList<DirectorateClassRow> Classes,
    IReadOnlyList<DirectorateSubjectRow> Subjects,
    DateTimeOffset GeneratedAt);

public sealed record DirectorateGradeBucket(string Label, int Count, double Percentage);
public sealed record DirectorateClassRow(string Name, string? Stage, int? Number, int? Section, string? Branch, int Students);
public sealed record DirectorateSubjectRow(string Name, int Teachers, int Classes);

public sealed class DirectorateSchoolReportPdf : IDocument
{
    private readonly DirectorateSchoolPdfData _data;
    private const string Primary = "#003366";
    private const string Light = "#F5F8FB";
    private const string Border = "#D8E0E8";

    public DirectorateSchoolReportPdf(DirectorateSchoolPdfData data) => _data = data;
    public DocumentMetadata GetMetadata() => new() { Title = $"تقرير المدرسة - {_data.Name}", Author = "نظام مدرستي", Subject = "تقرير تشغيلي وتعليمي مجمع" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.ContentFromRightToLeft();
            page.DefaultTextStyle(x => x.FontFamily("Amiri").FontSize(11).FontColor("#203247"));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(ComposeSchoolProfile);
                column.Item().Element(ComposeSummary);
                column.Item().Element(ComposeAttendanceAndAcademic);
                column.Item().Element(c => ComposeGradeTable(c, _data.GradeDistribution));
                column.Item().Element(c => ComposeClassesTable(c, _data.Classes));
                column.Item().Element(c => ComposeSubjectsTable(c, _data.Subjects));
            });
            page.Footer().AlignCenter().Row(row =>
            {
                row.RelativeItem().AlignLeft().Text($"أُنشئ: {_data.GeneratedAt.ToLocalTime():yyyy/MM/dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1)).AlignRight().Text(text => { text.Span("صفحة "); text.CurrentPageNumber(); text.Span(" من "); text.TotalPages(); });
            });
        });
    }

    private void ComposeHeader(IContainer container) => container.Background(Primary).Padding(14).Row(row =>
    {
        row.RelativeItem().AlignRight().Column(column => { column.Item().Text(_data.Name).Bold().FontSize(19).FontColor(Colors.White); column.Item().Text("تقرير تشغيلي وتعليمي مجمع").FontSize(10).FontColor("#DCEBFA"); });
        row.ConstantItem(90).AlignLeft().AlignMiddle().Text("مدرستي").Bold().FontSize(16).FontColor(Colors.White);
    });

    private void ComposeSchoolProfile(IContainer container) => Section(container, "بطاقة المدرسة", body =>
    {
        var items = new[] { ("الحالة", _data.IsActive ? "فعالة" : "معطلة"), ("التصنيف الرسمي", Value(_data.Status)), ("النوع", Value(_data.Gender)), ("المرحلة", Value(_data.Stage)), ("أدنى صف", _data.MinClass?.ToString() ?? "غير محدد"), ("أعلى صف", _data.MaxClass?.ToString() ?? "غير محدد") };
        body.Table(table =>
        {
            table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
            foreach (var item in items) table.Cell().BorderBottom(1).BorderColor(Border).Padding(8).AlignRight().Column(c => { c.Item().Text(item.Item1).FontSize(9).FontColor(Colors.Grey.Darken1); c.Item().Text(item.Item2).Bold(); });
        });
    });

    private void ComposeSummary(IContainer container) => Section(container, "المؤشرات الرئيسية", body =>
    {
        var items = new[] { ("المديرون", _data.Managers), ("المعلمون", _data.Teachers), ("الطلاب", _data.Students), ("الصفوف", _data.ClassesCount), ("المواد", _data.SubjectsCount) };
        body.Row(row => { foreach (var item in items) row.RelativeItem().PaddingHorizontal(3).Background(Light).Border(1).BorderColor(Border).Padding(8).AlignCenter().Column(c => { c.Item().Text(item.Item1).FontSize(9).FontColor(Colors.Grey.Darken1); c.Item().Text(item.Item2.ToString()).Bold().FontSize(18).FontColor(Primary); }); });
    });

    private void ComposeAttendanceAndAcademic(IContainer container) => container.Row(row =>
    {
        row.RelativeItem().PaddingRight(5).Element(c => Section(c, "الحضور والغياب", body => MetricGrid(body, new[] { ("إجمالي السجلات", _data.AttendanceTotal.ToString()), ("حضور", _data.Present.ToString()), ("غياب", _data.Absent.ToString()), ("غياب بعذر", _data.Excused.ToString()), ("نسبة الالتزام", _data.AttendanceRate.HasValue ? $"{_data.AttendanceRate}%" : "لا توجد بيانات") })));
        row.RelativeItem().PaddingLeft(5).Element(c => Section(c, "الأداء الأكاديمي", body => MetricGrid(body, new[] { ("سجلات العلامات", _data.GradeDistribution.Sum(x => x.Count).ToString()), ("متوسط العلامات", _data.AverageGrade.HasValue ? $"{_data.AverageGrade}%" : "لا توجد بيانات") })));
    });

    private static void MetricGrid(IContainer container, IEnumerable<(string Label, string Value)> items) => container.Column(column => { foreach (var item in items) column.Item().PaddingVertical(3).Row(row => { row.RelativeItem().AlignRight().Text(item.Label).FontColor(Colors.Grey.Darken1); row.ConstantItem(90).AlignLeft().Text(item.Value).Bold(); }); });

    private static void ComposeGradeTable(IContainer container, IReadOnlyList<DirectorateGradeBucket> rows) => Section(container, "توزيع العلامات", body => body.Table(table =>
    {
        table.ColumnsDefinition(c => { c.ConstantColumn(38); c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); });
        Header(table, "الرقم", "فئة العلامة", "عدد السجلات", "النسبة");
        if (rows.Count == 0) EmptyRow(table, 4, "لا توجد علامات مسجلة"); else for (var i = 0; i < rows.Count; i++) Row(table, (i + 1).ToString(), rows[i].Label, rows[i].Count.ToString(), $"{rows[i].Percentage}%");
    }));

    private static void ComposeClassesTable(IContainer container, IReadOnlyList<DirectorateClassRow> rows) => Section(container, "توزيع الصفوف", body => body.Table(table =>
    {
        table.ColumnsDefinition(c => { c.ConstantColumn(34); c.RelativeColumn(2.2f); c.RelativeColumn(1.5f); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(1.4f); c.RelativeColumn(); });
        Header(table, "الرقم", "اسم الصف", "المرحلة", "رقم الصف", "الشعبة", "الفرع", "الطلاب");
        if (rows.Count == 0) EmptyRow(table, 7, "لا توجد صفوف مسجلة"); else for (var i = 0; i < rows.Count; i++) Row(table, (i + 1).ToString(), rows[i].Name, Value(rows[i].Stage, "-"), rows[i].Number?.ToString() ?? "-", rows[i].Section?.ToString() ?? "-", Value(rows[i].Branch, "-"), rows[i].Students.ToString());
    }));

    private static void ComposeSubjectsTable(IContainer container, IReadOnlyList<DirectorateSubjectRow> rows) => Section(container, "المواد التعليمية", body => body.Table(table =>
    {
        table.ColumnsDefinition(c => { c.ConstantColumn(34); c.RelativeColumn(2.5f); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(1.5f); });
        Header(table, "الرقم", "المادة", "المعلمون", "الصفوف", "حالة التغطية");
        if (rows.Count == 0) EmptyRow(table, 5, "لا توجد مواد مسجلة"); else for (var i = 0; i < rows.Count; i++) Row(table, (i + 1).ToString(), rows[i].Name, rows[i].Teachers.ToString(), rows[i].Classes.ToString(), rows[i].Teachers > 0 && rows[i].Classes > 0 ? "مغطاة" : "تحتاج متابعة");
    }));

    private static void Section(IContainer container, string title, Action<IContainer> content) => container.EnsureSpace(95).Border(1).BorderColor(Border).Column(column => { column.Item().Background(Light).Padding(8).AlignRight().Text(title).Bold().FontSize(14).FontColor(Primary); column.Item().Padding(8).Element(content); });
    private static void Header(TableDescriptor table, params string[] cells) => table.Header(header => { foreach (var cell in cells) header.Cell().Background(Primary).Border(1).BorderColor(Colors.White).Padding(6).AlignCenter().Text(cell).Bold().FontColor(Colors.White); });
    private static void Row(TableDescriptor table, params string[] cells) { foreach (var cell in cells) table.Cell().BorderBottom(1).BorderColor(Border).Padding(6).AlignCenter().Text(cell); }
    private static void EmptyRow(TableDescriptor table, uint columns, string message) => table.Cell().ColumnSpan(columns).Padding(12).AlignCenter().Text(message).FontColor(Colors.Grey.Darken1);
    private static string Value(string? value, string fallback = "غير محدد") => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
