using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Config;
public class EmailSettings
{
    public const string SectionName = "EmailSettings"; // Tên của block trong appsettings.json
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
