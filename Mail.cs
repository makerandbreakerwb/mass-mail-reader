namespace MassMailReader;

public record Mail(string Subject, string Content, List<MailAttachement> Attachements, DateTimeOffset Date);

public record MailAttachement(string FileName, string Content, DateTimeOffset Date);
