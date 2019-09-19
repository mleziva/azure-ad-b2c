using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using B2CSample.Configuration;
using B2CSample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OtpSharp;
using QRCoder;

namespace AADB2C.RestoreUsername.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TotpController : ControllerBase
    {
        private readonly TotpSettings AppSettings;

        // Demo: Inject an instance of an AppSettingsModel class into the constructor of the consuming class, 
        // and let dependency injection handle the rest
        public TotpController(IOptions<TotpSettings> appSettings)
        {
            this.AppSettings = appSettings.Value;
        }

        [Route("Generate")]
        [HttpPost]
        public async Task<ActionResult> Generate()
        {
            string input = null;

            // If not data came in, then return
            if (this.Request.Body == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Request content is null", HttpStatusCode.Conflict));
            }

            // Read the input claims from the request body
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                input = await reader.ReadToEndAsync();
            }

            // Check input content value
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Request content is empty", HttpStatusCode.Conflict));
            }

            // Convert the input string into InputClaimsModel object
            TotpInputClaims inputClaims = TotpInputClaims.Parse(input);

            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Can not deserialize input claims", HttpStatusCode.Conflict));
            }

            try
            {

                // Define the URL for the QR code. When user scan this URL, it opens one of the 
                // authentication apps running on the mobile device
                byte[] secretKey = KeyGeneration.GenerateRandomKey(20);

                string TOTPUrl = KeyUrl.GetTotpUrl(secretKey, $"{AppSettings.TOTPAccountPrefix}:{inputClaims.UserName}",
                    AppSettings.TOTPTimestep);

                TOTPUrl = $"{TOTPUrl}&issuer={AppSettings.TOTPIssuer.Replace(" ", "%20")}";

                // Generate QR code for the above URL
                var qrCodeGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrCodeGenerator.CreateQrCode(TOTPUrl, QRCodeGenerator.ECCLevel.L);
                BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData);
                byte[] qrCodeBitmap = qrCode.GetGraphic(4);

                var output = new B2CResponseContent(string.Empty, HttpStatusCode.OK)
                {
                    QrCodeBitmap = Convert.ToBase64String(qrCodeBitmap),
                    SecretKey = this.EncryptAndBase64(Convert.ToBase64String(secretKey))
                };

                return Ok(output);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent($"General error (REST API): {ex.Message}", HttpStatusCode.Conflict));
            }
        }

        [Route("Verify")]
        [HttpPost]
        public async Task<ActionResult> Verify()
        {
            string input = null;

            // If not data came in, then return
            if (this.Request.Body == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Request content is null", HttpStatusCode.Conflict));
            }

            // Read the input claims from the request body
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                input = await reader.ReadToEndAsync();
            }

            // Check input content value
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Request content is empty", HttpStatusCode.Conflict));
            }

            // Convert the input string into InputClaimsModel object
            var inputClaims = TotpInputClaims.Parse(input);

            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("Can not deserialize input claims", HttpStatusCode.Conflict));
            }

            try
            {
                byte[] secretKey = Convert.FromBase64String(this.DecryptAndBase64(inputClaims.SecretKey));

                Totp totp = new Totp(secretKey);
                long timeStepMatched;

                // Verify the TOTP code provided by the users
                bool verificationResult = totp.VerifyTotp(
                    inputClaims.TotpCode,
                    out timeStepMatched,
                    VerificationWindow.RfcSpecifiedNetworkDelay);

                if (!verificationResult)
                {
                    return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("The verification code is invalid.", HttpStatusCode.Conflict));
                }

                // Using the input claim 'timeStepMatched', we check whether the verification code has already been used.
                // For sign-up, the 'timeStepMatched' input claim is null and should not be evaluated 
                // For sign-in, the 'timeStepMatched' input claim contains the last time last matched (from the user profile), and evaluated with 
                // the value of the result of the TOTP out 'timeStepMatched' variable
                if ((string.IsNullOrEmpty(inputClaims.TimeStepMatched) == false) &&
                    !(inputClaims.TimeStepMatched).Equals(timeStepMatched.ToString()))
                {
                    return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent("The verification code has already been used.", HttpStatusCode.Conflict));

                }

                var output = new B2CResponseContent(string.Empty, HttpStatusCode.OK)
                {
                    TimeStepMatched = timeStepMatched.ToString()
                };

                return Ok(output);

            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseContent($"General error (REST API): {ex.Message}", HttpStatusCode.Conflict));
            }
        }

        private string EncryptAndBase64(string encryptString)
        {
            string EncryptionKey = this.AppSettings.EncryptionKey;
            byte[] clearBytes = Encoding.Unicode.GetBytes(encryptString);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encryptString = Convert.ToBase64String(ms.ToArray());
                }
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptString));
        }

        private string DecryptAndBase64(string cipherText)
        {
            // Base64 decode
            cipherText = Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));

            string EncryptionKey = this.AppSettings.EncryptionKey;
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
}