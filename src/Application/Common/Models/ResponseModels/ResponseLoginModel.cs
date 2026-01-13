using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
public class ResponseLoginModel
{
    public string AccountId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Image {  get; set; } = string.Empty; 
    //public IList<string>? Role { get; set; }
    public string Role {  get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    //public string RefreshToken { get; set; } = string.Empty;
}
