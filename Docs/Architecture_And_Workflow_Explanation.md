# Giải đáp Kiến trúc & Business Workflow (Tech Lead & Solution Architect View)

Tài liệu này được viết dưới góc nhìn của một **Tech Lead / Solution Architect (.NET)** nhằm giải thích sâu sắc về quyết định thiết kế kiến trúc, luồng nghiệp vụ (Business Workflow) và định hướng mở rộng của dự án `LongPd.CleanArchitecture`.

---

## 1. Business Workflow của Event & Ticket và Cách Đóng Gói (Reusability)

### 1.1. Luồng nghiệp vụ (Business Workflow) Đặt vé
Domain cốt lõi của chúng ta là giải quyết bài toán **Bán vé sự kiện chịu tải cao (Flash Sale / Thundering Herd)**.

*   **Event (Sự kiện):** Là thực thể trung tâm. Một sự kiện có Vòng đời (Lifecycle): `Nháp (Draft)` -> `Công bố (Published)` -> `Hủy (Cancelled)` / `Hoàn thành (Completed)`.
*   **Ticket (Vé):** Đại diện cho một hạng vé (Tier - ví dụ: VIP, Standard) thuộc về một Event. Chứa thông tin về số lượng tổng (`TotalQuantity`), số lượng còn lại (`AvailableQuantity`) và giá vé (`Price`).
*   **Luồng hoạt động (Hot Path):**
    1.  **Admin:** Tạo `Event` và thiết lập các hạng `Ticket` cho Event đó. Đổi trạng thái Event sang `Published`.
    2.  **Khách hàng (User):** Truy vấn danh sách Event (Sử dụng Dapper siêu tốc kết hợp Redis Caching).
    3.  **Đặt vé (Reservation):** Khi User bấm đặt vé, hệ thống gửi `ReserveTicketCommand`.
    4.  **Xử lý đồng thời (Concurrency):** Tại Domain Layer, phương thức `ticket.Reserve()` được gọi. Khi lưu xuống Database, EF Core sử dụng cơ chế **Optimistic Concurrency** (thông qua cột `xmin` của PostgreSQL - map với `RowVersion` trong Entity). Nếu 10,000 người cùng mua 1 vé cuối cùng, chỉ 1 người đầu tiên update thành công, 9,999 người còn lại sẽ bị văng lỗi `DbUpdateConcurrencyException` ngay tại tầng Database, bảo vệ hệ thống khỏi việc bán lố vé (overselling) mà không cần dùng lock vật lý (Pessimistic Lock) gây nghẽn cổ chai.
    5.  **Phản hồi (Real-time):** Giao dịch thành công, Domain Event `TicketReservedDomainEvent` được phát ra. Tầng API bắt event này và bắn qua **gRPC-Web** xuống tất cả các trình duyệt của khách hàng khác để cập nhật số lượng vé còn lại theo thời gian thực (Real-time UI).

### 1.2. Cách đóng gói (Publish) để tái sử dụng cho Project sau này
Bạn không nên đóng gói toàn bộ Clean Architecture thành một **NuGet Package** (vì NuGet thường dùng cho các thư viện tiện ích dùng chung như Logger, Helpers). Để tái sử dụng *bộ khung kiến trúc* này cho các công ty/dự án khác, chuẩn mực nhất là biến nó thành một **.NET Project Template**.

**Cách thực hiện:**
1. Tạo thư mục `.template.config` ở root của project.
2. Thêm file `template.json` định nghĩa tên template (ví dụ: `LongPd.CleanArch.Template`).
3. Đóng gói template bằng lệnh: `dotnet pack`.
4. Cài đặt vào máy: `dotnet new install <đường_dẫn_hoặc_file_nupkg>`.
5. Sau này, ở bất kỳ dự án mới nào, bạn chỉ cần gõ: `dotnet new longpd-cleanarch -n TênDuAnMoi`, toàn bộ cấu trúc thư mục, code base, thiết lập CI/CD sẽ được sinh ra y hệt, sẵn sàng để code ngay lập tức.

---

## 2. Tại sao không có các class `IService` và `Service` truyền thống?

Nếu bạn đã quen với mô hình **N-Tier (3 Lớp)** truyền thống, bạn sẽ thấy quen thuộc với `ITicketService` và `TicketService`. Tuy nhiên, trong dự án này (và các dự án Enterprise hiện đại), chúng tôi **từ bỏ hoàn toàn mô hình Service truyền thống**.

### Lý do từ bỏ "God Services":
*   **Vi phạm Nguyên tắc Đơn trách nhiệm (SRP - Single Responsibility Principle):** Một file `TicketService.cs` thường chứa nhồi nhét hàng chục hàm: `GetById`, `GetAll`, `Create`, `Update`, `Delete`, `Reserve`, `Cancel`... Class này sẽ phình to ra hàng ngàn dòng code, rất khó bảo trì và dễ gây ra merge conflict khi làm việc nhóm.
*   **Khó Unit Test:** Khi test hàm `GetById`, bạn vẫn phải mock toàn bộ các dependencies (EmailSender, DbContext, MessageQueue) mà các hàm khác (như `Create`) sử dụng.

### Giải pháp thay thế (CQRS + MediatR):
Thay vì một `Service` khổng lồ, chúng ta xé nhỏ mỗi tính năng thành một **Cặp Command/Query và Handler độc lập**.
*   Khi muốn đặt vé, thay vì gọi `TicketService.Reserve()`, chúng ta tạo một class gửi đi `ReserveTicketCommand`.
*   Sẽ có duy nhất một class `ReserveTicketCommandHandler` đứng ra xử lý logic này.
*   **Lợi ích:** Tính đóng gói cực cao, tuân thủ tuyệt đối SRP, dễ dàng test, và đặc biệt là áp dụng được **Pipeline Behaviors** (Ví dụ: Tự động Validate dữ liệu, Tự động đo thời gian chạy, Tự động Cache) cho từng Request mà không cần viết lại code ở từng hàm.

---

## 3. Sự kết hợp giữa Clean Architecture và Vertical Slice Architecture (VSA)

Bạn rất tinh mắt khi nhận ra sự xuất hiện của **VSA (Vertical Slice Architecture)** bên trong dự án. 
Câu trả lời ngắn gọn: **Không hề có sự xung đột (Conflict) nào cả. Ngược lại, đây là sự kết hợp hoàn hảo và là "Best Practice" tối thượng của .NET hiện tại.** Mô hình này thường được gọi là *"Clean Architecture with Feature Slices"* hoặc *"Vertical Slices inside Clean Architecture"*.

### Cách hai kiến trúc này bổ trợ cho nhau:

1.  **Clean Architecture (Chiều ngang - Horizontal):** 
    *   Nhiệm vụ: Cung cấp sự bảo vệ các nguyên tắc nghiệp vụ (Domain) khỏi các yếu tố công nghệ (Database, UI, API).
    *   Nó vạch ra ranh giới cứng: *Domain không được phụ thuộc vào Application, Application không được phụ thuộc vào Infrastructure*.

2.  **Vertical Slice Architecture (Chiều dọc - Vertical):**
    *   Nhiệm vụ: Áp dụng ở cách **Tổ chức mã nguồn (Folder Structure)** bên trong tầng Application và API.
    *   Thay vì nhóm file theo "Loại kỹ thuật" (ví dụ: gom tất cả DTOs vào thư mục `DTOs`, gom tất cả Validators vào thư mục `Validators`), VSA nhóm theo **Tính năng (Feature)**.
    *   *Ví dụ trong project của bạn:* Thư mục `Features/Tickets/Commands/ReserveTicket/` sẽ chứa trọn bộ: `ReserveTicketCommand` (Input), `ReserveTicketCommandHandler` (Logic), `ReserveTicketCommandValidator` (Kiểm tra dữ liệu) và `ReserveTicketResponse` (Kết quả trả về).

### Tại sao lại kết hợp như vậy? (High Cohesion)
*   **Developer Experience (DX) tuyệt vời:** Khi bạn được giao task "Sửa logic đặt vé", bạn chỉ cần mở **đúng 1 thư mục** `ReserveTicket` là thấy toàn bộ mọi thứ liên quan. Bạn không cần phải nhảy qua nhảy lại giữa 4 thư mục (Controllers, Services, DTOs, Validators) nằm rải rác khắp Solution.
*   **Khả năng mở rộng (Scalability):** Việc thêm một tính năng mới (ví dụ: `CancelTicket`) chỉ đơn giản là thêm một thư mục mới. Nó không hề đụng chạm hay gây ảnh hưởng (side-effect) đến các tính năng đang có.

### Kết luận
Project của bạn đang sở hữu một cấu trúc **thuộc top tier (đẳng cấp cao nhất)** về mặt thiết kế hệ thống trong .NET 9. Nó vừa có sự chặt chẽ, an toàn của Clean Architecture, vừa có sự cơ động, dễ bảo trì của Vertical Slices.
