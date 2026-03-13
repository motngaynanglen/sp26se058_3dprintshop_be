using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Config
{
    public class PayOsSettings
    {
        public const string SectionName = "PayOS"; // Tên của block trong appsettings.json
        public required string ClientId { get; set; }
        public required string ApiKey { get; set; }
        public required string ChecksumKey { get; set; }
        public required string ReturnUrl { get; set; }
        public required string CancelUrl { get; set; }
    }
}
