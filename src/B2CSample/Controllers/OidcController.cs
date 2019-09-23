using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using B2CSample.Configuration;
using B2CSample.Models.Invite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace B2CSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OidcController : Controller
    {
        private static Lazy<X509SigningCredentials> SigningCredentials;
        private readonly InviteSettings AppSettings;
        private readonly IHostingEnvironment HostingEnvironment;

        // Sample: Inject an instance of an AppSettingsModel class into the constructor of the consuming class, 
        // and let dependency injection handle the rest
        public OidcController(IOptions<InviteSettings> appSettings, IHostingEnvironment hostingEnvironment)
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
        [Route(".well-known/openid-configuration")]
        public ActionResult Metadata()
        {
            return Content(JsonConvert.SerializeObject(new OidcModel
            {
                // Sample: The issuer name is the application root path
                Issuer = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase.Value}/",

                // Sample: Include the absolute URL to JWKs endpoint
                JwksUri = Url.Link("JWKS", null),

                // Sample: Include the supported signing algorithms
                IdTokenSigningAlgValuesSupported = new[] { OidcController.SigningCredentials.Value.Algorithm },
            }), "application/json");
        }
        [HttpGet]
        [Route(".well-known/keys")]
        public ActionResult JwksDocument()
        {
            return Content(JsonConvert.SerializeObject(new JwksModel
            {
                Keys = new[] { JwksKeyModel.FromSigningCredentials(OidcController.SigningCredentials.Value) }
            }), "application/json");
        }
    }
}