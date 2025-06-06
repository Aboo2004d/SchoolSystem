using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

public class StudentEnrollmentCertificate : IDocument
{
    private readonly string StudentName;
    private readonly int IdNumber;
    private readonly string ClassName;
    private readonly string SchoolName;
    private readonly string NameMenegar;

    public StudentEnrollmentCertificate(string studentName, int idNumber, string className, string schoolName, string nameMenegar)
    {
        StudentName = studentName;
        IdNumber = idNumber;
        ClassName = className;
        SchoolName = schoolName;
        NameMenegar = nameMenegar;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var year = DateTime.Now.Year;
        var academicYear = DateTime.Now.Month < 8
            ? $"{year - 1} - {year}"
            : $"{year} - {year + 1}";

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(50);
            page.DefaultTextStyle(x => x.FontSize(16));
            page.DefaultTextStyle(x => x.FontFamily("Amiri"));


            page.Content().Column(column =>
            {
                column.Spacing(20);
                column.Item().AlignCenter().Text("شهادة قيد")
                    .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);

                column.Item().AlignRight().Text($"اسم الطالب: {StudentName}");
                column.Item().AlignRight().Text($"رقم الهوية: {IdNumber}");
                column.Item().AlignRight().Text($"الصف: {ClassName}");
                column.Item().AlignRight().Text($"اسم المدرسة: {SchoolName}");

                column.Item().PaddingTop(20).AlignRight().Text(
                    $"تشهد إدارة مدرسة: {SchoolName}"
                );
                column.Item().PaddingTop(5).AlignRight().Text(
                    $"بأن الطالب: {StudentName}, يحمل الهوية رقم: {IdNumber}"
                );
                column.Item().PaddingTop(5).AlignRight().Text(
                    $". مسجل لدينا في الصف: {ClassName}, للعام الدراسي: {academicYear}"
                );

                column.Item().PaddingTop(50).AlignLeft().Text($":مدير المدرسة")
                    .Bold().FontSize(18);
                column.Item().AlignLeft().Text(NameMenegar)
                    .FontSize(15);
            });
        });
    }
}
