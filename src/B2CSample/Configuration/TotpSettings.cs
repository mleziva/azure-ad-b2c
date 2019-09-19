using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CSample.Configuration
{
    public class TotpSettings
    {
        public string TOTPIssuer { get; set; }
        public string TOTPAccountPrefix { get; set; }
        public int TOTPTimestep { get; set; }
        public string EncryptionKey { get; set; }
    }
}
