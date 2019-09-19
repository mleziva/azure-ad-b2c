using Newtonsoft.Json;

namespace B2CSample.Models
{
    public class TotpInputClaims
    {
        public string UserName { get; set; }
        public string SecretKey { get; set; }
        public string TotpCode { get; set; }
        public string TimeStepMatched { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static TotpInputClaims Parse(string JSON)
        {
            return JsonConvert.DeserializeObject(JSON, typeof(TotpInputClaims)) as TotpInputClaims;
        }
    }
}
