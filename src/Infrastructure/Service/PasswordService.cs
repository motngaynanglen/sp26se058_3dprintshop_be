using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class PasswordService : IPasswordService
{
    // Tạo mã Hash từ mật khẩu thuần
    public string HashPassword(string password)
    {
        // WorkFactor càng cao thì Hash càng chậm (an toàn hơn), mặc định là 11
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 11);
    }

    // Kiểm tra mật khẩu nhập vào có khớp với mã Hash trong DB không
    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword);
    }
}
