using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDesignWorkToServiceSelectionTo1N : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa Foreign Key an toàn (tự tìm tên thật)
            migrationBuilder.Sql(@"
        SET @fk_name = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                        WHERE TABLE_NAME = 'ServiceSelections' AND COLUMN_NAME = 'DesignWorkId' 
                        AND TABLE_SCHEMA = DATABASE() LIMIT 1);
        SET @sql_fk = IF(@fk_name IS NOT NULL, CONCAT('ALTER TABLE ServiceSelections DROP FOREIGN KEY ', @fk_name), 'SELECT 1');
        PREPARE stmt_fk FROM @sql_fk; EXECUTE stmt_fk; DEALLOCATE PREPARE stmt_fk;
    ");

            // 2. Xóa Index an toàn (kiểm tra trước khi xóa)
            migrationBuilder.Sql(@"
        SET @idx_name = (SELECT INDEX_NAME FROM INFORMATION_SCHEMA.STATISTICS 
                         WHERE TABLE_NAME = 'ServiceSelections' AND COLUMN_NAME = 'DesignWorkId' 
                         AND TABLE_SCHEMA = DATABASE() LIMIT 1);
        SET @sql_idx = IF(@idx_name IS NOT NULL, CONCAT('ALTER TABLE ServiceSelections DROP INDEX ', @idx_name), 'SELECT 1');
        PREPARE stmt_idx FROM @sql_idx; EXECUTE stmt_idx; DEALLOCATE PREPARE stmt_idx;
    ");

            // 3. Xóa cột ServiceSelectionId an toàn
            migrationBuilder.Sql(@"
        SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'DesignWorks' AND COLUMN_NAME = 'ServiceSelectionId' 
                           AND TABLE_SCHEMA = DATABASE());
        SET @sql_col = IF(@col_exists > 0, 'ALTER TABLE DesignWorks DROP COLUMN ServiceSelectionId', 'SELECT 1');
        PREPARE stmt_col FROM @sql_col; EXECUTE stmt_col; DEALLOCATE PREPARE stmt_col;
    ");

            // 4. Thêm cột Note (Nếu chưa có)
            migrationBuilder.Sql(@"
        SET @note_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = 'ServiceSelections' AND COLUMN_NAME = 'Note' 
                            AND TABLE_SCHEMA = DATABASE());
        SET @sql_note = IF(@note_exists = 0, 'ALTER TABLE ServiceSelections ADD Note LONGTEXT NULL', 'SELECT 1');
        PREPARE stmt_note FROM @sql_note; EXECUTE stmt_note; DEALLOCATE PREPARE stmt_note;
    ");

            // 5. Tạo lại Index mới (Lúc này là Non-Unique cho quan hệ 1-N)
            // Dùng SQL để tránh trùng lặp Index name
            migrationBuilder.Sql(@"
        SET @idx_new_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                               WHERE TABLE_NAME = 'ServiceSelections' AND INDEX_NAME = 'IX_ServiceSelections_DesignWorkId' 
                               AND TABLE_SCHEMA = DATABASE());
        SET @sql_new_idx = IF(@idx_new_exists = 0, 'CREATE INDEX IX_ServiceSelections_DesignWorkId ON ServiceSelections(DesignWorkId)', 'SELECT 1');
        PREPARE stmt_new_idx FROM @sql_new_idx; EXECUTE stmt_new_idx; DEALLOCATE PREPARE stmt_new_idx;
    ");

            // 6. Gắn lại Foreign Key chuẩn (Dùng SQL cho đồng bộ)
            migrationBuilder.Sql(@"
        SET @fk_new_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                               WHERE TABLE_NAME = 'ServiceSelections' AND CONSTRAINT_NAME = 'FK_ServiceSelections_DesignWorks_DesignWorkId' 
                               AND TABLE_SCHEMA = DATABASE());
        SET @sql_new_fk = IF(@fk_new_exists = 0, 'ALTER TABLE ServiceSelections ADD CONSTRAINT FK_ServiceSelections_DesignWorks_DesignWorkId FOREIGN KEY (DesignWorkId) REFERENCES DesignWorks(Id) ON DELETE RESTRICT', 'SELECT 1');
        PREPARE stmt_new_fk FROM @sql_new_fk; EXECUTE stmt_new_fk; DEALLOCATE PREPARE stmt_new_fk;
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khôi phục lại trạng thái cũ nếu cần Rollback
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceSelections_DesignWorks_DesignWorkId",
                table: "ServiceSelections");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSelections_DesignWorkId",
                table: "ServiceSelections");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ServiceSelections");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceSelectionId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_DesignWorkId",
                table: "ServiceSelections",
                column: "DesignWorkId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceSelections_DesignWorks_DesignWorkId",
                table: "ServiceSelections",
                column: "DesignWorkId",
                principalTable: "DesignWorks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

    }
}
