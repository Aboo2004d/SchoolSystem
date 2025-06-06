using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

public class TeacherEnrollmentCertificate : IDocument
{
    private readonly string TeacherName;
    private readonly int IdNumber;
    private readonly string SchoolName;
    private readonly string NameMenegar;
    private readonly List<string> SubjectName;

    public TeacherEnrollmentCertificate(string teacherName, int idNumber, string schoolName, string nameMenegar, List<string> subjectName)
    {
        TeacherName = teacherName;
        IdNumber = idNumber;
        SchoolName = schoolName;
        SubjectName = subjectName;
        NameMenegar = nameMenegar;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        foreach (string lectuer in SubjectName)
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
                        .FontSize(28).Bold().FontColor(Colors.Green.Darken2);

                    column.Item().AlignRight().Text($"اسم المعلم: {TeacherName}");
                    column.Item().AlignRight().Text($"رقم الهوية: {IdNumber}");
                    column.Item().AlignRight().Text($"اسم المدرسة: {SchoolName}");
                    column.Item().AlignRight().Text($"المادة: {lectuer}");
                    
                    column.Item().PaddingTop(20).AlignRight().Text(
                        $"تشهد إدارة مدرسة: {SchoolName}"
                    );
                    column.Item().PaddingTop(5).AlignRight().Text(
                        $"بأن المعلم: {TeacherName}، حامل الهوية رقم: {IdNumber} "
                    );
                    column.Item().PaddingTop(5).AlignRight().Text(
                        $". يعمل لدينا في تدريس مادة: {lectuer} خلال العام الدراسي: {academicYear}"
                    );

                    column.Item().PaddingTop(50).AlignLeft().Text("مدير المدرسة: ")
                        .Bold().FontSize(16);
                        column.Item().AlignLeft().Text(NameMenegar)
                    .FontSize(15);
                });
            });
        }
    }
}
