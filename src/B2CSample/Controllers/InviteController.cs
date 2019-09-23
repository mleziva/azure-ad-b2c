using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Mail;
using System.Net;
using B2CSample.Configuration;
using B2CSample.Models;
using B2CSample.Models.Invite;

namespace B2CSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InviteController : Controller
    {
        private static Lazy<X509SigningCredentials> SigningCredentials;
        private readonly InviteSettings AppSettings;
        private readonly IHostingEnvironment HostingEnvironment;

        // Sample: Inject an instance of an AppSettingsModel class into the constructor of the consuming class, 
        // and let dependency injection handle the rest
        public InviteController(IOptions<InviteSettings> appSettings, IHostingEnvironment hostingEnvironment)
        {
            this.AppSettings = appSettings.Value;
            this.HostingEnvironment = hostingEnvironment;

            // Sample: Load the certificate with a private key (must be pfx file)
            SigningCredentials = new Lazy<X509SigningCredentials>(() =>
            {

                X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(
                                            X509FindType.FindByThumbprint,
                                            this.AppSettings.SigningCertThumbprint,
                                            false);
                // Get the first cert with the thumb-print
                if (certCollection.Count > 0)
                {
                    return new X509SigningCredentials(certCollection[0]);
                }

                throw new Exception("Certificate not found");
            });
        }

        [HttpGet]
        public ActionResult<InviteResponseContent> GenerateInvite(string Name, string email, string phone)
        {

            if (string.IsNullOrEmpty(email))
            {
                ViewData["Message"] = "";
                return View();
            }
            string token = BuildIdToken(Name, email);
            string link = BuildUrl(token);
            var inviteResponse = new InviteResponseContent()
            {
                InviteLink = link
            };
            return Ok(inviteResponse);
        }
        private void CreateAndSendEmail(string email, string link)
        {
            try
            {
                string htmlTemplate = System.IO.File.ReadAllText(Path.Combine(this.HostingEnvironment.ContentRootPath, "App_Data\\Template.html"));
                string Body = string.Empty;
                MailMessage mailMessage = new MailMessage();
                mailMessage.To.Add(email);
                mailMessage.From = new MailAddress(AppSettings.SMTPFromAddress);
                mailMessage.Subject = AppSettings.SMTPSubject;
                mailMessage.Body = string.Format(htmlTemplate, email, link);
                mailMessage.IsBodyHtml = true;
                SmtpClient smtpClient = new SmtpClient(AppSettings.SMTPServer, AppSettings.SMTPPort);
                smtpClient.Credentials = new NetworkCredential(AppSettings.SMTPUsername, AppSettings.SMTPPassword);
                smtpClient.EnableSsl = AppSettings.SMTPUseSSL;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.Send(mailMessage);

                ViewData["Message"] = $"Email sent to {email}";

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string BuildIdToken(string Name, string ClubId)
        {
            string issuer = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase.Value}/";

            // All parameters send to Azure AD B2C needs to be sent as claims
            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>();
            claims.Add(new System.Security.Claims.Claim("name", Name, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("email", ClubId, System.Security.Claims.ClaimValueTypes.String, issuer));

            // Create the token
            JwtSecurityToken token = new JwtSecurityToken(
                    issuer,
                    this.AppSettings.B2CClientId,
                    claims,
                    DateTime.Now,
                    DateTime.Now.AddDays(7),
                    InviteController.SigningCredentials.Value);

            // Get the representation of the signed token
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

            return jwtHandler.WriteToken(token);
        }

        private string BuildUrl(string token)
        {
            string nonce = Guid.NewGuid().ToString("n");

            return string.Format(this.AppSettings.B2CSignUpUrl,
                    this.AppSettings.B2CTenant,
                    this.AppSettings.B2CPolicy,
                    this.AppSettings.B2CClientId,
                    Uri.EscapeDataString(this.AppSettings.B2CRedirectUri),
                    nonce) + "&id_token_hint=" + token;
        }

    }
}