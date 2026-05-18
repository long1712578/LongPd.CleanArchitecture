# Giải đáp Chuyên sâu: Validation Pipeline & gRPC Real-time (Tech Lead View)

Dưới góc nhìn của một Tech Lead, đây là 2 câu hỏi cực kỳ đắt giá. Nó chạm đến những phần "ma thuật" (magic) và những kỹ thuật tối ưu hóa cao cấp nhất của dự án này.

---

## 1. Bí ẩn của `PublishEventCommandValidator`: Tại sao nó hoạt động mà không cần gọi lệnh `.Validate()`?

### 1.1. Cách Code cũ (Truyền thống)
Thông thường, để validate dữ liệu, bạn sẽ phải làm thế này trong Controller hoặc Service:
```csharp
// Code cũ bạn hay làm:
var validator = new PublishEventCommandValidator();
var validationResult = validator.Validate(command);

if (!validationResult.IsValid) {
    return BadRequest(validationResult.Errors);
}
// Sau đó mới xử lý logic...
```
*Vấn đề:* Nếu bạn có 100 API, bạn phải viết đoạn code lặp đi lặp lại (Boilerplate) này 100 lần ở 100 nơi khác nhau.

### 1.2. Cách Code trong dự án này (MediatR Pipeline Behaviors)
Dự án của chúng ta áp dụng mẫu thiết kế **AOP (Aspect-Oriented Programming)** thông qua **MediatR Pipeline**. 

Bạn hãy tưởng tượng ống nước dẫn nước từ API (Controller/Endpoint) đến `CommandHandler`. Thay vì để nước chảy tuột một mạch, chúng ta đặt các "Màng lọc" (Behaviors) ở giữa ống nước đó.

```text
[API Endpoint] ---> (Gửi Command) ---> [ MediatR Pipeline ]
                                            |
                                            |--> 1. [ LoggingBehavior ] (Ghi log bắt đầu)
                                            |
                                            |--> 2. [ ValidationBehavior ] (Quét Validator) ---> NẾU LỖI: Bật ngược lại API báo 422
                                            |
                                            |--> 3. [ CachingBehavior ] (Kiểm tra Cache)
                                            |
[CommandHandler] <--------------------------+ (Nước đã sạch 100%, Handler chỉ việc xử lý logic)
```

**Luồng hoạt động thực tế:**
1. Khi bạn tạo class `PublishEventCommandValidator`, thư viện FluentValidation tự động đăng ký nó vào hệ thống IoC Container (Dependency Injection).
2. Khi Endpoint gọi lệnh `await sender.Send(command, ct);`, MediatR không nhảy ngay vào `PublishEventCommandHandler`.
3. MediatR đi qua **`ValidationBehavior`** trước.
4. Tại đây, `ValidationBehavior` sẽ tự động tìm xem có class nào sinh ra để validate cho `PublishEventCommand` không. Nó tìm thấy `PublishEventCommandValidator` của bạn!
5. Nó tự động chạy kiểm tra. 
   * **Nếu hợp lệ:** Đi tiếp tới `PublishEventCommandHandler`.
   * **Nếu sai luật:** Nó lập tức "cắt cầu dao" (Short-circuit), ném ra lỗi `ValidationException` (chứa các thông báo lỗi).
6. File `GlobalExceptionHandler` (tầng API) chụp lấy lỗi này và trả về cho người dùng mã HTTP `422 Unprocessable Entity` cực kỳ chuẩn xác.

**Tóm lại:** Bạn chỉ việc tạo file Validator và định nghĩa luật. Hệ thống sẽ tự động bắt đầu "làm phép" dưới nền. Handler của bạn sẽ luôn luôn nhận được dữ liệu **sạch 100%**.

---

## 2. Giao thức gRPC: Tác dụng của nó trong dự án này là gì?

### 2.1. Vấn đề của REST API truyền thống
Hãy tưởng tượng hệ thống Bán vé Flash Sale của bạn đang có 10,000 người dùng truy cập web cùng một lúc. 
Làm sao để 10,000 người này thấy được **Số lượng vé đang giảm xuống** trên màn hình theo thời gian thực (Real-time)?
*   **Cách cũ (Polling):** Frontend (React/Vue) cứ mỗi 1 giây lại gọi API `GET /api/tickets/available`. -> *Hậu quả: 10,000 requests / 1 giây sẽ đập chết tươi Database của bạn ngay lập tức.*
*   **Cách WebSockets (SignalR):** Rất tốt, nhưng text payload (JSON) khá nặng.

### 2.2. Giải pháp Đẳng cấp cao: gRPC & gRPC-Web (Server-Streaming)
gRPC là giao thức do Google phát triển, chạy trên nền **HTTP/2** và sử dụng định dạng nhị phân **Protobuf**. Nó nhanh hơn REST API và JSON gấp rất nhiều lần.

Trong thư mục `LongPd.CleanArchitecture.Api/Grpc/`, bạn sẽ thấy file `TicketGrpcService.cs` (chứa class `TicketGrpcServiceImpl`).

**Cách nó hoạt động trong luồng Bán Vé:**
1. Khi User A đặt vé thành công, Database commit xong, UnitOfWork sẽ phát ra sự kiện `TicketReservedDomainEvent`.
2. Lớp `TicketDomainEventHandlers` (trong API layer) bắt được sự kiện này.
3. Nó lập tức gọi hàm `BroadcastTicketUpdate` của **gRPC Service**.
4. **Và đây là điều kỳ diệu:** Bất kỳ Frontend Client nào đang kết nối gRPC-Web tới Server, luồng gRPC sẽ "bắn" trực tiếp dữ liệu nhị phân (chứa số lượng vé mới nhất) thẳng vào trình duyệt của họ ngay lập tức (độ trễ tính bằng mili-giây).
5. Frontend của 10,000 người kia không cần F5 trình duyệt, số vé trên màn hình của họ sẽ tự động giật lùi xuống: 100 -> 99 -> 98... y như xem Live Stream.

**Tóm lại tác dụng của gRPC trong dự án:**
Chúng ta dùng gRPC ở dạng **Server-Streaming** (Server liên tục đẩy data xuống Client). Nó giải quyết bài toán cốt lõi của hệ thống Flash Sale: **Truyền tải trạng thái "Hết vé" đến hàng ngàn người dùng cùng lúc với tốc độ ánh sáng mà không cần họ phải gửi Request lên Server hỏi.**

---

### 2.3. [Deep Dive] Trình duyệt nhận Stream gRPC-Web kiểu gì khi HTTP bản chất là Request-Response?

Bạn đã đặt ra một câu hỏi mang tầm vóc của một Senior/Solution Architect! Rất nhiều lập trình viên nghĩ rằng muốn Real-time thì bắt buộc phải dùng WebSocket (SignalR) hoặc Long-Polling. Vậy gRPC-Web làm trò ảo thuật gì ở đây?

Dưới đây là lời giải thích "thông não" dưới góc độ giao thức mạng (Network Protocol):

#### A. Giới hạn của Trình duyệt (Browser Limitation)
Đúng như bạn nói, **gRPC gốc (Native gRPC)** chạy trên nền **HTTP/2**, sử dụng tính năng *HTTP/2 Streams* (truyền nhiều frame dữ liệu nhị phân trên 1 kết nối TCP). Tuy nhiên, **trình duyệt web (Chrome, Edge, Safari) KHÔNG cho phép Javascript can thiệp trực tiếp vào HTTP/2 Frames**.
Vì vậy, Frontend KHÔNG THỂ dùng client gRPC Native để gọi thẳng lên Server được. Đó là lý do thư viện **`gRPC-Web`** được Google sinh ra.

#### B. Trò ảo thuật của gRPC-Web: `ReadableStream` & `Chunked Transfer Encoding`
`gRPC-Web` không dùng WebSocket, cũng không dùng Long-Polling. Nó dùng chính nền tảng HTTP/1.1 hoặc HTTP/2 thông qua API `Fetch` hiện đại của Javascript bằng kỹ thuật **Server-Sent Chunks**.

Cách nó diễn ra:
1. **Mở kết nối (Handshake):** Frontend (React) gởi đúng **1 HTTP POST Request** lên Server.
2. **Giữ kết nối (Keep-Alive):** Khác với REST API thông thường là Server xử lý xong sẽ đóng kết nối (Return Response), gRPC Server sẽ **giữ nguyên kết nối HTTP này mở liên tục** (không gởi cờ `FIN` hoặc Header báo kết thúc).
3. **Đẩy dữ liệu (Flushing Chunks):** Mỗi khi có một vé được mua (Domain Event kích hoạt), gRPC Server sẽ đóng gói số lượng vé mới thành Protobuf (nhị phân), bọc nó trong một "Chunk" (Khối dữ liệu) và "Flush" (Bơm) xuống đường truyền HTTP đang mở.
4. **Hứng dữ liệu (Fetch ReadableStream):** Ở dưới Frontend, thư viện `gRPC-Web` sử dụng tính năng **`ReadableStream` của Fetch API** (hoặc XHR Streaming). Nó liên tục "nghe" các chunk bay xuống. Cứ mỗi lần Server bơm 1 chunk, Frontend nhận được, giải mã Protobuf ra Object, và gọi hàm callback `stream.on('data', (response) => { ... })` để update UI.

#### C. So sánh Tốc độ và Hiệu suất với SignalR/WebSocket

| Tiêu chí | WebSocket (SignalR) | gRPC-Web (Server-Streaming) |
| :--- | :--- | :--- |
| **Giao thức** | TCP (Được Upgrade từ HTTP) | Thuần HTTP/1.1 hoặc HTTP/2 |
| **Định dạng dữ liệu** | Text (JSON) - Phình to, nặng | Nhị phân (Protobuf) - Cực kỳ nhỏ gọn |
| **Tiêu hao Tài nguyên Server**| Nặng (Server phải quản lý Hub, duy trì Connection ID, Heartbeat liên tục) | Nhẹ nhàng hơn rất nhiều (Chỉ duy trì HTTP Stream) |
| **Phù hợp cho Flash Sale?** | Tốt, nhưng dễ sập nếu có 100k kết nối | **Tuyệt hảo!** Chuyên trị tải siêu cao vì đẩy data nhị phân siêu nhanh. |
| **Hạn chế** | Khó scale qua Load Balancer (Cần Sticky Sessions hoặc Redis Backplane) | Dễ scale hơn, nhưng **chỉ là 1 chiều** (Server -> Client). Nếu Client muốn gửi lên liên tục thì phải gọi gRPC Unary riêng. |

#### D. Tại sao dự án này chọn gRPC-Web?
Trong nghiệp vụ Bán vé / Flash Sale, Client chỉ cần "Nhìn" xem vé còn bao nhiêu (Luồng Đọc liên tục), và khi mua thì bấm 1 phát (Luồng Ghi 1 lần). Chúng ta KHÔNG CẦN giao tiếp 2 chiều liên tục kiểu Chat App.
Do đó, dùng gRPC-Web Server-Streaming là **nước đi tối ưu nhất về Performance và Scalability**, vượt trội hoàn toàn so với việc cõng thêm một hệ thống SignalR khổng lồ và nặng nề!
