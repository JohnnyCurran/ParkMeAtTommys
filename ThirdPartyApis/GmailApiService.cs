using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Contracts;
using System.Net.Mail;
using MimeKit;

namespace ThirdPartyApis
{
    public class GmailApiService : IGmailApiService
    {
	private GmailService gmailService;

	private string[] Scopes = { GmailService.Scope.GmailModify };
	private const string ApplicationName = "Pass 2 Park at Camden";

	public GmailApiService()
	{
	    gmailService = CreateGmailService();
	}

	public EmailResult SendEmail(string attachmentLocation, EmailInformation emailInfo)
	{
	    MailMessage msg = new MailMessage(from: emailInfo.FromEmail, to: emailInfo.ToEmail, subject: "Your parking pass", body: null);
	    msg.Attachments.Add(new Attachment(attachmentLocation));
	    MimeMessage mimeMsg = MimeMessage.CreateFromMailMessage(msg);
	    var raw = Base64UrlEncode(mimeMsg.ToString());
	    var gmailMessage = new Message { Raw = raw };
	    var gmailSendRequest = gmailService.Users.Messages.Send(gmailMessage, "me");

	    try
	    {
		Message gmailMessageSent = gmailSendRequest.Execute();
		return new EmailResult { Success = true };
	    }
	    catch (Exception e)
	    {
		return new EmailResult
		{ 
		    Success = false,
		    ErrorMessage = e.Message
		};
	    }
	}

	private GmailService CreateGmailService()
	{
	    UserCredential credential;

	    using (var stream =
		    new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
	    {
		// The file token.json stores the user's access and refresh tokens, and is created
		// automatically when the authorization flow completes for the first time.
		string credPath = "token.json";
		credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
			GoogleClientSecrets.Load(stream).Secrets,
			Scopes,
			"user",
			CancellationToken.None,
			new FileDataStore(credPath, true)).Result;
		Console.WriteLine("Credential file saved to: " + credPath);
	    }

	    // Create Gmail API service.
	    var service = new GmailService(new BaseClientService.Initializer()
		    {
		    HttpClientInitializer = credential,
		    ApplicationName = ApplicationName,
		    });

	    return service;
	}

	private string Base64UrlEncode(string input)
	{
	    var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
	    return Convert.ToBase64String(inputBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
	}
    }
}
