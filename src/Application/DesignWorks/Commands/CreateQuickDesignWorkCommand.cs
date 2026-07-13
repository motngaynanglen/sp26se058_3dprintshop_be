// REMOVED — CheckoutQuickPrintCommand has been merged into AddFilesToQuickPrintCommand.
//
// To create a new Quick Print request:  POST /api/design-work/quick-print
//   → AddFilesToQuickPrintCommand { DesignWorkId = null, ProjectName, FileUrls, ... }
//
// To re-upload files (SKETCHING → REVIEWING): POST /api/design-work/{id}/add-files
//   → AddFilesToQuickPrintCommand { DesignWorkId = <id>, FileUrls, Note, ... }
namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
