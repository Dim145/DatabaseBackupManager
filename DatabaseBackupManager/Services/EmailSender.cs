using FluentEmail.Core;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DatabaseBackupManager.Services;

public class EmailSender: IEmailSender
{
    private IFluentEmail Mails { get; }
    
    public EmailSender(IFluentEmail mails)
    {
        Mails = mails;
    }
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await Mails.To(email)
            .Subject(subject)
            .Body(htmlMessage, true)
            .SendAsync();
    }
}