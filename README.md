# LongPd.CleanArchitecture - Enterprise Starter Kit & Ticketing Platform

🚀 **High-Performance .NET 9 Architecture Template designed for scale, low-latency, and zero-allocation.**

Dự án này là một bộ khung kiến trúc (Enterprise Starter Kit) áp dụng Clean Architecture, đồng thời được triển khai thực tế vào domain xử lý hệ thống Đặt vé sự kiện / Flash Sale chịu tải cao (Thundering Herd problem).

---

## 🌐 Public Scalar API Documentation
Tài liệu API Scalar sống động của dự án được cập nhật tự động và public miễn phí qua GitHub Pages:
👉 **[Xem Live API Documentation tại đây!](https://long1712578.github.io/LongPd.CleanArchitecture/)**

---

## 1. Tổng quan Kiến trúc (Overview)

Hệ thống được thiết kế theo mô hình **CQRS-lite (Command Query Responsibility Segregation)** kết hợp với **Clean Architecture**, phân tách rõ ràng trách nhiệm giữa việc Đọc (Read) và Ghi (Write) dữ liệu.

### Core Technologies:
* **API & Transport:** ASP.NET Core Minimal APIs, gRPC, gRPC-Web (dành cho React Client).
* **Application Layer:** MediatR (CQRS Pattern), FluentValidation.
* **Data Access (Write):** Entity Framework Core, Custom `UnitOfWork` (Quản lý Lifecycle, Audit Trails, Soft Delete).
* **Data Access (Read):** Dapper (Micro-ORM) tối ưu hóa tốc độ truy vấn.
* **Caching Strategy:** Hybrid Cache (L1: In-Memory, L2: Redis/Distributed Cache).
* **Performance:** Native AOT Readiness, sử dụng `ReadOnlySpan<T>` và `AggressiveInlining` cho các tác vụ xử lý memory nội bộ.

---

## 2. Cấu trúc Solution (Solution Structure)

Dự án tuân thủ nghiêm ngặt Dependency Rule: Lõi không phụ thuộc vào bất kỳ framework nào bên ngoài.

* `LongPd.CleanArchitecture.Domain` (Core)
    * Chứa Entities, Interfaces (IRepository, IUnitOfWork), Domain Events, Exceptions.
* `LongPd.CleanArchitecture.Application`
    * Chứa Use Cases: Commands (Ghi), Queries (Đọc), MediatR Handlers.
    * Chứa các Cross-cutting concerns (Caching Behavior, Validation Behavior).
* `LongPd.CleanArchitecture.Infrastructure`
    * Implementations: EF Core DbContext, Dapper Connection Factory.
    * Custom `UnitOfWork` xử lý tự động `CreatedBy`, `ModifiedOn`, Bulk Updates.
* `LongPd.CleanArchitecture.Api` (Presentation)
    * Entry point: Minimal APIs & gRPC Services.
    * Dependency Injection Registration.

---

## 3. Quyết định Kiến trúc (Architecture Decision Records - ADRs)

### 3.1. Phân tách Command & Query (CQRS)
* **Lý do:** Tối ưu hóa hiệu năng độc lập. EF Core sinh ra để tracking object state (hoàn hảo cho Insert/Update/Delete). Dapper map object cực nhanh và viết SQL thuần (hoàn hảo cho việc lấy danh sách, báo cáo phức tạp).
* **Nguyên tắc:** 
    * Commands làm thay đổi state -> Dùng `IUnitOfWork` + EF Core.
    * Queries chỉ đọc state -> Dùng `IQueryHandler` + Dapper (không tracking).

### 3.2. Quản lý Giao dịch (Unit of Work)
* **Cơ chế:** Gom nhóm các thay đổi dữ liệu vào một transaction duy nhất. 
* **Tính năng cốt lõi:**
    * Tự động xử lý Audit Logging (gán thời gian, người thao tác) thông qua `ChangeTracker`.
    * Hỗ trợ Soft Delete an toàn (`IsDeleted = true`) kết hợp Domain Events dispatching sau khi transaction được commit thành công.

### 3.3. Hybrid Caching (L1 + L2)
* **Vấn đề:** Các hệ thống Flash Sale (đặt vé) chịu áp lực đọc cực lớn để kiểm tra số lượng vé.
* **Giải pháp:** Áp dụng cache 2 tầng. 
    * L1 (MemoryCache): Phản hồi tính bằng microsecond.
    * L2 (Redis): Đảm bảo tính đồng nhất giữa các node microservices.
    * *Tích hợp:* Sử dụng Pipeline Behavior của MediatR để tự động can thiệp vào các Query có attribute `ICacheableQuery`.

### 3.4. Giao tiếp Thời gian thực (Real-time Communication)
* **Lý do chọn gRPC-Web:** Thay vì dùng RESTful API + WebSockets/SignalR truyền thống, hệ thống sử dụng gRPC-Web để stream trạng thái "Số lượng vé còn lại" trực tiếp xuống React frontend, đảm bảo độ trễ thấp nhất có thể (low-latency) và serialization nhị phân tối ưu.

---

## 4. Hướng dẫn Phát triển (Getting Started)

### Phase 1: Infrastructure Setup
- [x] Khởi tạo Solution và phân chia Layers (Domain, Application, Infrastructure, Api).
- [x] Cài đặt các NuGet packages cần thiết (MediatR, Dapper, EF Core, gRPC).
- [x] Cấu hình Dependency Injection cho `UnitOfWork` và Database Context.

### Phase 2: Core Behaviors & Handlers
- [x] Xây dựng base classes cho CQRS (ICommand, IQuery, Result, Error).
- [x] Thiết lập MediatR Pipeline (Logging, Validation, Caching).
- [x] Cấu hình interceptor/middleware cho gRPC-Web và Global Exception.

### Phase 3: Domain Implementation (Ticketing)
- [x] Thiết kế `Ticket` entity với Optimistic Concurrency Token (`xmin` PostgreSQL).
- [x] Xây dựng gRPC streaming broadcast live ticket availability khi Domain Events phát ra.
- [x] CI/CD tự động build, verify và xuất bản tài liệu API Scalar tĩnh lên GitHub Pages.

---
*Developed for high-performance and community contribution.*
