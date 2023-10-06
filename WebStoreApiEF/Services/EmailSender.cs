using Microsoft.OpenApi.Models;
using RestSharp;
using RestSharp.Authenticators;


namespace WebStoreApiEF.Services
{
    public class EmailSender
    {
        private readonly string apiKey;
        private readonly string fromEmail;
        public EmailSender(IConfiguration configuration) 
        {
            apiKey = configuration["EmailSender:ApiKey"]!;
            fromEmail = configuration["EmailSender:FromEmail"]!;
        }
        public  RestClientOptions SendSimpleMessage()
        {
        //    RestClientOptions client = new RestClientOptions();
        //    client.BaseUrl = new Uri("https://api.mailgun.net/v3");
        //    client.Authenticator = new HttpBasicAuthenticator("api","YOUR_API_KEY");
        //    RestRequest request = new RestRequest();
        //    request.AddParameter("domain", "YOUR_DOMAIN_NAME", ParameterType.UrlSegment);
        //    request.Resource = "{domain}/messages";
        //    request.AddParameter("from", "Excited User <mailgun@YOUR_DOMAIN_NAME>");
        //    request.AddParameter("to", "bar@example.com");
        //    request.AddParameter("to", "YOU@YOUR_DOMAIN_NAME");
        //    request.AddParameter("subject", "Hello");
        //    request.AddParameter("text", "Testing some Mailgun awesomness!");
        //    request.Method = Method.Post;
        //   // return  client.Execute(request);
        //   return client.

            return null;
        }

    }
}
