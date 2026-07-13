using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public interface IBackblazeB2Service
{
    Task<string> UploadGlbAsync(byte[] glbBase64, string folder = "models");
}
