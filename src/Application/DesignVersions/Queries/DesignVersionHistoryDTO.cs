using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignVersions.Queries;

public class DesignVersionHistoryDTO
{
    public Guid Id { get; set; }
    public Guid DesignWorkId { get; set; }
    public Guid? DesignLogId { get; set; }
    public Guid UploaderId { get; set; }
    public string? UploaderName { get; set; }
    public string? Title { get; set; }
    public string FileUrl { get; set; } = null!;
    public int VersionNumber { get; set; }
    public bool IsPreviewable { get; set; }
    public bool IsApproved { get; set; }
    public bool IsPrintable { get; set; }
    public DateTimeOffset Created { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignVersionHistory, DesignVersionHistoryDTO>()
                .ForMember(d => d.Title, opt => opt.MapFrom(s => s.Tilte))
                .ForMember(d => d.UploaderName, opt => opt.MapFrom(s => s.Uploader.Fullname));
        }
    }
}
