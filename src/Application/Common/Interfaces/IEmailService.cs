using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;
public interface IEmailService
{
    /// <summary>
    /// Gửi email cơ bản với nội dung văn bản hoặc HTML
    /// </summary>
    /// <param name="to">Địa chỉ người nhận</param>
    /// <param name="subject">Tiêu đề thư</param>
    /// <param name="body">Nội dung thư</param>
    Task SendEmailAsync(string to, string subject, string body);

    /// <summary>
    /// Gửi email theo mẫu (Template) - Rất hữu ích cho đồ án chuyên nghiệp
    /// </summary>
    Task SendPasswordResetEmailAsync(string to, string userName, string resetToken);
}
