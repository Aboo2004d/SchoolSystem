using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

public class EmailValidationService : IEmailValidationService
{
    public async Task<bool> IsEmailValidAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // التحقق من الصيغة
        if (!IsValidEmailSyntax(email))
            return false;

        // التحقق من وجود الدومين
        var domain = email.Split('@')[1];
        if (!await IsDomainExistsAsync(domain))
            return false;

        return true;
    }

    private bool IsValidEmailSyntax(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsDomainExistsAsync(string domain)
    {
        try
        {
            IPHostEntry entry = await Dns.GetHostEntryAsync(domain);
            return entry != null;
        }
        catch
        {
            return false;
        }
    }
}
