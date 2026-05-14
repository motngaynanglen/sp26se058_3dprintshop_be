using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Domain.Entities;
using AutoMapper;
using System.Text.Json;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Queries;

public class DesignLogDTO
{
    public Guid Id { get; set; }
    public Guid DesignWorkId { get; set; }
    public Guid? ParentLogId { get; set; }

    public Guid? AccountId { get; set; }
    public string? SenderName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsAI { get; set; }


    public string? Content { get; set; }
    public string LogType { get; set; } = null!;
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public string? Metadata { get; set; }


    public List<string> ImageUrls { get; set; } = new();

    // Danh sách các phiên bản đính kèm trong Log này
    public List<DesignVersionDTO> Versions { get; set; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignLog, DesignLogDTO>()
                .ForMember(d => d.SenderName, opt => opt.MapFrom(s => s.Account != null
                    ? s.Account.Fullname
                    : (s.IsAI ? "AI Assistant" : "SYSTEM")))
                .ForMember(d => d.AvatarUrl, opt => opt.MapFrom(s => s.Account != null ? s.Account.ProfileImageURL : null))

                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.Metadata)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(src.Metadata, (JsonSerializerOptions?)null)))

                .ForMember(dest => dest.Versions, opt => opt.MapFrom(src => src.VersionHistories))
                // Metadata map trực tiếp chuỗi JSON sang DTO
                .ForMember(d => d.Metadata, opt => opt.MapFrom(s => s.Metadata))
                .ForMember(dest => dest.LogType, opt => opt.MapFrom(src => src.LogType.ToString()));
        }
    }
}

public class DesignVersionDTO
{
    public Guid Id { get; set; }
    public string? Title { get; set; } // Đã sửa chính tả từ 'Tilte' thành 'Title'
    public string FileUrl { get; set; } = null!;
    public int VersionNumber { get; set; }
    public bool IsApproved { get; set; }
    public bool IsPrintable { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignVersionHistory, DesignVersionDTO>()
                .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => src.FileUrl));
        }
    }
}
