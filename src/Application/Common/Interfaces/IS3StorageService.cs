using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;
public interface IS3StorageService
{
    Task<string> GetPresignedUploadUrlAsync(string fileName, string folderName,long maxSizeBytes, int expiresMinutes = 15);
}
