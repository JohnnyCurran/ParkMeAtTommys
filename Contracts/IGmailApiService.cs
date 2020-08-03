using System;

namespace Contracts
{
    public interface IGmailApiService
    {
	EmailResult SendEmail(string attachmentLocation, EmailInformation emailInfo);
    }
}
