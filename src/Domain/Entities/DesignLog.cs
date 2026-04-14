using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignLog : BaseAuditableEntity
{
    public Guid DesignWorkId { get; set; }
    
    // === THREADING (Dùng để reply tin nhắn cụ thể) ===
    public Guid? ParentLogId { get; set; }

    // === ACTORS ===
    public Guid? AccountId { get; set; } // Người thực hiện (Null nếu là System/AI)
    public bool IsAI { get; set; } = false;

    // === CONTENT ===
    public string? Content { get; set; }
    public string? Metadata { get; set; } // Lưu JSON (ví dụ: { "versionId": "...", "images": ["url1"] })

    public string LogType { get; set; } = "COMMUNICATION";
    // COMMUNICATION, INTERNAL_NOTE, SYSTEM, VERSION_UPDATE, AI_GEN, STATUS_CHANGE
    // === STATUS ===
    public bool IsRead { get; set; } = false;
    // === NAVIGATION ===
    public virtual DesignWork DesignWork { get; set; } = null!;
    public virtual Account? Account { get; set; }
    public virtual DesignLog? ParentLog { get; set; }
    public virtual ICollection<DesignLog> Replies { get; set; } = new List<DesignLog>();
    public virtual ICollection<DesignVersionHistory> VersionHistories { get; set; } = new List<DesignVersionHistory>();
}
