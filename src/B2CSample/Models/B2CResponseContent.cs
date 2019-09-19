using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace B2CSample.Models
{
    public class B2CResponseContent
    {
        public string Version { get; set; }
        public int Status { get; set; }
        public string UserMessage { get; set; }

        // Optional claims
        public string QrCodeBitmap { get; set; }
        public string SecretKey { get; set; }
        public string TimeStepMatched { get; set; }

        public B2CResponseContent(string message, HttpStatusCode status)
        {
            this.UserMessage = message;
            this.Status = (int)status;
            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
