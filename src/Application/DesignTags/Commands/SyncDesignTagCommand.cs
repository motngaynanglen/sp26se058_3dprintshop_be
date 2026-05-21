using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Commands;

public class SyncDesignTagItem
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid ConceptTagId { get; set; }
    [DefaultValue(false)]
    public bool IsMainTag { get; set; }
}

[Authorize(Roles = Roles.MANAGER)]
public class SyncDesignTagCommand : IRequest<Guid>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid DesignTemplateId { get; set; }
    public List<SyncDesignTagItem> Tags { get; set; } = new();
}

public class SyncDesignTagCommandHandler
    : IRequestHandler<SyncDesignTagCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public SyncDesignTagCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(
        SyncDesignTagCommand request,
        CancellationToken cancellationToken)
    {
        // =========================
        // VALIDATION — chỉ 1 main tag
        // =========================
        if (request.Tags.Count(x => x.IsMainTag) > 1)
            throw new BusinessException("Chỉ được phép có đúng 1 tag chính.");

        var duplicateConceptIds = request.Tags
            .GroupBy(x => x.ConceptTagId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateConceptIds.Any())
        {
            throw new DuplicateException("Danh sách tag gửi lên có tag bị trùng.");
        }

        var templateExists = await _context.DesignTemplates
            .AnyAsync(x => x.Id == request.DesignTemplateId, cancellationToken);
        if (!templateExists)
        {
            throw new DataNotFoundException(nameof(DesignTemplate), request.DesignTemplateId);
        }

        var requestConceptIds = request.Tags
            .Select(x => x.ConceptTagId)
            .ToHashSet();

        var validConceptIds = await _context.ConceptTags
            .Where(x => requestConceptIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var invalidConceptIds = requestConceptIds.Except(validConceptIds).ToList();
        if (invalidConceptIds.Any())
        {
            throw new BusinessException("Một hoặc nhiều tag ý tưởng không tồn tại hoặc đã ngưng hoạt động.");
        }

        // =========================
        // LẤY TAG CŨ
        // =========================
        var existingTags = await _context.DesignTags
            .Where(x => x.DesignTemplateId == request.DesignTemplateId)
            .ToListAsync(cancellationToken);

        var existingConceptIds = existingTags
            .Select(x => x.ConceptTagId)
            .ToHashSet();

        // =========================
        // DELETE — có trong DB nhưng không có trong request
        // =========================
        var toDelete = existingTags
            .Where(x => !requestConceptIds.Contains(x.ConceptTagId))
            .ToList();

        _context.DesignTags.RemoveRange(toDelete);

        // =========================
        // INSERT — có trong request nhưng chưa có trong DB
        // =========================
        var toInsert = request.Tags
            .Where(x => !existingConceptIds.Contains(x.ConceptTagId))
            .Select(x => new DesignTag
            {
                Id = Guid.NewGuid(),
                DesignTemplateId = request.DesignTemplateId,
                ConceptTagId = x.ConceptTagId,
                IsMainTag = x.IsMainTag
            })
            .ToList();

        await _context.DesignTags.AddRangeAsync(toInsert, cancellationToken);

        // =========================
        // UPDATE — có ở cả hai nơi
        // =========================
        var toUpdate = existingTags
            .Where(x => requestConceptIds.Contains(x.ConceptTagId))
            .ToList();

        foreach (var entity in toUpdate)
        {
            var req = request.Tags
                .First(x => x.ConceptTagId == entity.ConceptTagId);

            entity.IsMainTag = req.IsMainTag;
        }

        // =========================
        // SAFETY CHECK — nếu có tag mà không có main → auto set tag đầu tiên làm main
        // =========================
        var finalMainCount =
            toInsert.Count(x => x.IsMainTag) +
            toUpdate.Count(x => x.IsMainTag);

        if (request.Tags.Count > 0 && finalMainCount == 0)
        {
            var first = toInsert.FirstOrDefault();
            if (first != null)
                first.IsMainTag = true;
            else
                toUpdate.First().IsMainTag = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return request.DesignTemplateId;
    }
}

