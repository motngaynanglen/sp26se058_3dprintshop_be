using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Config;
public class BackblazeB2Settings
{
    public const string SectionName = "BackblazeB2"; // Tên của block trong appsettings.json
    public required string KeyId { get; set; }
    public required string ApplicationKey { get; set; }
    public required string BucketName { get; set; }
    public required string ServiceUrl { get; set; }

}
