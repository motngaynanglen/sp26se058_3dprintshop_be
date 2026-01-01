# 🚀 3D Printed Model Shop - Backend API

> **Hệ thống thương mại điện tử in ấn 3D thông minh, tích hợp AI chuyển đổi hình ảnh 2D thành mô hình 3D.**

---

## 📌 Tổng quan dự án
Dự án được xây dựng nhằm cung cấp giải pháp trọn gói từ việc tạo mô hình đến sản xuất vật lý. Hệ thống sử dụng công nghệ AI tiên tiến để hỗ trợ người dùng tạo ra các mẫu in 3D độc bản từ hình ảnh cá nhân.

* **Trình trạng:** Development (Fall 2025)
* **Nền tảng:** .NET 8 SDK
* **Kiến trúc:** Clean Architecture (Jason Taylor Template v8.0.6)

Dự án được khởi tạo dựa trên [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) phiên bản **8.0.6**, sử dụng **.NET 8 SDK** làm nền tảng cốt lõi.

---

## 🏗 Kiến trúc hệ thống (Clean Architecture)
Dự án được phân tách thành 4 lớp rõ rệt để đảm bảo khả năng mở rộng và kiểm thử độc lập:

1. **Domain**: Chứa các thực thể lõi (Entities), Enums, Value Objects và logic nghiệp vụ cơ bản.
2. **Application**: Xử lý logic nghiệp vụ chính thông qua các Use Cases (CQRS Pattern).
3. **Infrastructure**: Kết nối cơ sở dữ liệu (EF Core), lưu trữ Cloud, và tích hợp AI Service.
4. **Web**: Cung cấp các RESTful API endpoints và cấu hình Swagger UI.

---

## 🛠 Công nghệ & Kỹ thuật
* **Framework:** .NET 8 (LTS) & C# 12
* **Quản lý thư viện:** Central Package Management (CPM) qua `Directory.Packages.props`.
* **Pattern:** CQRS với MediatR, FluentValidation.
* **Database:** SQL Server với Entity Framework Core.
* **Định dạng Solution:** Standard `.sln` (Tương thích tối đa với VS 2022).

---

## 🚀 Hướng dẫn cài đặt nhanh

### 1. Yêu cầu hệ thống
* **.NET 8 SDK** (phiên bản 8.0.x).
* **Visual Studio 2022** (v17.8 trở lên).

### 2. Thiết lập dự án

# Clone dự án
```bash
git clone [https://github.com/your-username/sp26se058_3dprintshop_be.git](https://github.com/your-username/sp26se058_3dprintshop_be.git)
```
# Khôi phục các thư viện NuGet
```bash
dotnet restore
```
# Build Solution
```bash
dotnet build -tl
```
### 3. Chạy ứng dụng (Hot Reload)
Để chạy Web API và tự động cập nhật khi thay đổi code:
```bash
dotnet watch run --project src/Web
```
### 4. Kiểm thử (Testing)
Dự án bao gồm Unit Tests, Integration Tests và Functional Tests:
```bash
dotnet test
```
### 🎨 Quy chuẩn Code & Định dạng
Dự án tích hợp sẵn EditorConfig nhằm duy trì phong cách viết code đồng nhất cho toàn bộ thành viên.

❗ **LƯU Ý QUAN TRỌNG:** > Vui lòng không thay đổi file `.editorconfig` ở thư mục gốc để tránh xung đột khi Merge code giữa các thành viên.
