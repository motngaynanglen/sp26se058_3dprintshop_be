🚀 3D Printed Model Shop - Backend API
Dự án được khởi tạo dựa trên Clean.Architecture.Solution.Template phiên bản 8.0.6, sử dụng .NET 8 SDK làm nền tảng cốt lõi.

🏗 Cấu trúc Solution
Dự án tuân thủ mô hình Clean Architecture để đảm bảo tính dễ bảo trì và mở rộng:

src/Domain: Chứa các thực thể (Entities), Enums và Logic cốt lõi.

src/Application: Chứa Logic nghiệp vụ (Use Cases), CQRS (MediatR), Mapping và Validation.

src/Infrastructure: Kết nối cơ sở dữ liệu (EF Core), AI Services và các dịch vụ ngoại vi khác.

src/Web: Cổng giao tiếp API (REST) và cấu hình Middleware.

🛠 Lệnh vận hành cơ bản
1. Build dự án
Sử dụng Terminal tại thư mục gốc để biên dịch toàn bộ Solution:

Bash

dotnet build -tl
2. Chạy ứng dụng (Hot Reload)
Để chạy Web API với tính năng tự động tải lại khi thay đổi code:

Bash

cd .\src\Web\
dotnet watch run
🔗 Swagger UI: Truy cập https://localhost:5001 (hoặc cổng được cấu hình) để xem tài liệu API.

3. Kiểm thử (Testing)
Hệ thống bao gồm Unit Tests, Integration Tests và Functional Tests:

Bash

dotnet test
🎨 Quy chuẩn Code & Định dạng
Dự án tích hợp sẵn EditorConfig nhằm duy trì phong cách viết code đồng nhất cho toàn bộ thành viên (Kiên, Bách, Hải, Tuấn).

Lưu ý: Vui lòng không thay đổi file .editorconfig ở thư mục gốc để tránh xung đột khi Merge code.

⚡ Code Scaffolding (Tạo nhanh Use Case)
Template hỗ trợ tạo nhanh các Command và Query theo chuẩn CQRS. Di chuyển vào thư mục .\src\Application\ và sử dụng:

Tạo Command mới:

Bash

dotnet new ca-usecase -n Create3DModel -fn Models -ut command -rt int
Tạo Query mới:

Bash

dotnet new ca-usecase -n Get3DModels -fn Models -ut query -rt ModelsVm
Nếu gặp lỗi không tìm thấy lệnh ca-usecase, hãy cài đặt lại template:

Bash

dotnet new install Clean.Architecture.Solution.Template::8.0.6
📦 Quản lý thư viện (CPM)
Dự án sử dụng Central Package Management. Để thêm hoặc cập nhật thư viện NuGet, vui lòng chỉnh sửa tại file: 👉 Directory.Packages.props

🤝 Hỗ trợ & Tài liệu
Để tìm hiểu sâu hơn về cách vận hành template này, bạn có thể tham khảo tại Clean Architecture Project Website.
