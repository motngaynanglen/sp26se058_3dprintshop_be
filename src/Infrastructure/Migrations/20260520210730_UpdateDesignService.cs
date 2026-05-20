using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDesignService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op.
            // MySQL requires the DesignWorkId index for FK_ServiceSelections_DesignWorks_DesignWorkId.
            // Dropping it breaks migration, and the service flow now allows multiple ServiceSelections
            // per DesignWork, so this index must stay non-unique.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op.
        }
    }
}
