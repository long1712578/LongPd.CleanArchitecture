# gRPC Guide (Zero → Pro) cho project `LongPd.CleanArchitecture`

## 1) Mục tiêu tài liệu

Tài liệu này giúp bạn:

- Nắm nền tảng `gRPC` từ đầu.
- Hiểu vì sao project dùng `gRPC` thay vì chỉ REST.
- Đọc được toàn bộ luồng realtime trong code hiện tại.
- Biết cách mở rộng an toàn theo đúng Clean Architecture.

---

## 2) gRPC là gì? Khi nào dùng?

`gRPC` là framework RPC hiệu năng cao dùng `HTTP/2` + `Protocol Buffers`.

### Ưu điểm chính

- Payload nhỏ, nhanh hơn JSON trong nhiều case.
- Hỗ trợ streaming rất tốt (server-streaming, client-streaming, bidi).
- Contract-first qua file `.proto`.

### Khi nào nên dùng trong hệ thống của bạn

- Realtime cập nhật vé còn lại.
- Kết nối giữ lâu và server push nhiều lần.
- Cần latency thấp.

Trong project này:
- REST dùng cho command/query thông thường.
- `gRPC` dùng cho realtime ticket availability.

---

## 3) Tổng quan kiến trúc đang áp dụng

## 3.1 Vai trò từng layer

- `Domain`: luật nghiệp vụ (`Ticket.Reserve`, `Ticket.CancelReservation`) + raise domain events.
- `Application`: abstraction và use cases (`ITicketAvailabilityNotifier`).
- `Api`: transport adapter (`TicketGrpcServiceImpl`) + handler nhận domain event và đẩy realtime.
- `Infrastructure`: DB, Dapper, EF Core...

## 3.2 Luồng realtime end-to-end

1. Client subscribe `StreamAvailability(eventId)`.
2. `TicketGrpcServiceImpl` đăng ký stream vào registry.
3. Service gửi snapshot ban đầu từ DB.
4. Khi có reserve/cancel:
   - `Ticket` raise domain event.
   - Domain event handler gọi `ITicketAvailabilityNotifier`.
   - `TicketGrpcServiceImpl.NotifyAvailabilityChangedAsync(...)` broadcast cho toàn bộ stream của event đó.
5. Client nhận update và render realtime.

---

## 4) Contract-first với `ticket.proto`

File `LongPd.CleanArchitecture.Api/Protos/ticket.proto` là nguồn chân lý API.

Service:
- `StreamAvailability(StreamAvailabilityRequest) returns (stream AvailabilityUpdate)`
- `GetCurrentAvailability(GetAvailabilityRequest) returns (AvailabilitySnapshot)`

Messages:
- `AvailabilityUpdate`: đơn vị update realtime cho mỗi tier.
- `AvailabilitySnapshot`: snapshot tổng hợp lần đầu.

### Best practices cho `.proto`

- Không đổi số thứ tự field đã public.
- Không xóa field cũ bừa bãi (nên deprecate).
- Chỉ thêm field mới với number mới.
- Giữ naming rõ ràng và ổn định.

---

## 5) Giải thích file `TicketGrpcService.cs` (code hiện tại)

Class chính:

- `TicketGrpcServiceImpl : TicketGrpcService.TicketGrpcServiceBase, ITicketAvailabilityNotifier`

Ý nghĩa:
- Kế thừa base gRPC để expose RPC cho client.
- Implement `ITicketAvailabilityNotifier` để nhận push nội bộ từ domain event handler.
- Không còn static coupling, dễ test, đúng DIP (Dependency Inversion Principle).

### 5.1 Stream registry thread-safe

```csharp
private static readonly ConcurrentDictionary<Guid, ImmutableList<IServerStreamWriter<AvailabilityUpdate>>> ActiveStreams = new();
```

- Key: `EventId`
- Value: immutable list các stream active của event đó.

Lý do design này tốt:
- `ConcurrentDictionary`: safe cho nhiều thread.
- `ImmutableList`: iterate an toàn khi có thread khác add/remove.
- Giảm rủi ro race condition so với `Dictionary + lock` tự quản lý thủ công.

### 5.2 `NotifyAvailabilityChangedAsync(...)`

Vai trò:
- Nhận model trung gian từ Application (`TicketAvailabilityChangedNotification`).
- Map sang message gRPC (`AvailabilityUpdate`).
- Gọi helper broadcast `PushToStreamsAsync`.

Điểm mạnh:
- Domain handler không cần biết chi tiết gRPC message format.
- Chuẩn kiến trúc: Application nói bằng abstraction, Api convert sang transport object.

### 5.3 `StreamAvailability(...)` (server-streaming)

Flow:
1. Validate `EventId` (GUID).
2. Log subscribe.
3. Đăng ký stream bằng `AddOrUpdate` atomically.
4. Query snapshot hiện tại và `WriteAsync` từng update ban đầu.
5. Treo kết nối bằng `Task.Delay(Timeout.Infinite, cancellationToken)`.
6. Client disconnect thì vào `finally` để remove stream + cleanup key rỗng.

Vì sao đúng chuẩn:
- Luôn cleanup trong `finally` tránh memory leak.
- Có snapshot ban đầu để client đồng bộ trạng thái ngay.
- Tôn trọng cancellation token của gRPC context.

### 5.4 `GetCurrentAvailability(...)` (unary)

Vai trò:
- Trả snapshot tại 1 thời điểm.

Use cases:
- Fallback khi stream fail.
- Reload thủ công.
- Đồng bộ lại state khi reconnect.

### 5.5 `PushToStreamsAsync(...)`

- Lấy snapshot immutable list hiện tại.
- Gửi update cho từng stream.
- Stream nào lỗi thì đưa vào `staleWriters`.
- Sau đó `AddOrUpdate` để remove stale atomically.

Đây là pattern cleanup đúng cho long-lived connections.

### 5.6 `GetCurrentSnapshotAsync(...)`

- Dùng Dapper query nhanh, read-only.
- Trả danh sách `AvailabilityUpdate`.
- Có `IsSoldOut` computed tại SQL.

Lưu ý:
- Hiện query dùng `dbConnectionFactory.CreateAsync(ct)` là hợp lý cho async path.

---

## 6) Domain Event → gRPC bridge

File liên quan: `Api/DomainEventHandlers/TicketDomainEventHandlers.cs`

- Handler nhận `TicketReservedDomainEvent` và `TicketCancelledDomainEvent`.
- Gọi `ITicketAvailabilityNotifier.NotifyAvailabilityChangedAsync(...)`.

Điểm kiến trúc quan trọng:
- Handler không gọi trực tiếp static method của gRPC service.
- Giảm coupling giữa domain event flow và transport implementation.
- Test unit handler dễ hơn (mock interface).

---

## 7) Vì sao phải có cả REST và gRPC?

- REST: dễ debug, phù hợp CRUD/query API phổ thông.
- gRPC streaming: phù hợp realtime push liên tục.

Trong app ticketing:
- Đặt vé / hủy vé: REST (command).
- Cập nhật tồn vé realtime: gRPC streaming.

Đây là hybrid API design rất thực tế.

---

## 8) gRPC-Web trong project

Do client trình duyệt không dùng gRPC HTTP/2 native như backend client, cần `gRPC-Web`.

Project đã bật:
- `app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });`
- `app.MapGrpcService<TicketGrpcServiceImpl>().EnableGrpcWeb();`

=> React có thể kết nối stream qua gRPC-Web.

---

## 9) Những lỗi thường gặp và cách tránh

1. **Quên cleanup stream khi disconnect**
   - Hậu quả: leak memory.
   - Cách tránh: remove ở `finally`.

2. **Dùng collection không thread-safe cho stream registry**
   - Hậu quả: race condition.
   - Cách tránh: `ConcurrentDictionary` + immutable snapshot.

3. **Gộp business logic vào gRPC service**
   - Hậu quả: vi phạm Clean Architecture.
   - Cách tránh: để logic ở Domain/Application, gRPC chỉ orchestration + transport.

4. **Không chuẩn hóa lỗi gRPC**
   - Hậu quả: client khó xử lý.
   - Cách tránh: dùng `RpcException` với `StatusCode` phù hợp (`InvalidArgument`, ...).

5. **Thay đổi field number trong proto**
   - Hậu quả: breaking contract.
   - Cách tránh: giữ nguyên number cũ, chỉ add number mới.

---

## 10) Nâng cấp từ intermediate → pro cho module này

## 10.1 Nên làm tiếp

- Thêm `GrpcServerInterceptor` để:
  - log request/response thống nhất,
  - đo latency,
  - map exception tập trung.
- Thêm metrics:
  - active streams tổng,
  - active streams theo event,
  - broadcast success/fail count,
  - average broadcast latency.
- Thêm auth cho stream (nếu production multi-tenant).

## 10.2 Scale nhiều instance

Hiện tại `ActiveStreams` là in-memory của 1 node.
Nếu chạy nhiều instance:
- Domain event phát sinh ở node A không tự broadcast tới stream ở node B.

Giải pháp production:
- Dùng broker/pub-sub (Redis, RabbitMQ, Kafka...) để fan-out cross-node.
- Mỗi node nhận message rồi broadcast tới stream local của node đó.

## 10.3 Resilience

- Client auto-reconnect stream.
- Có unary snapshot để resync sau reconnect.
- Thêm retry/backoff có kiểm soát ở client.

---

## 11) Checklist review nhanh cho mọi thay đổi gRPC

- [ ] Có update `.proto` nếu contract đổi?
- [ ] Không breaking field number?
- [ ] Có validate input và trả `RpcException` đúng mã?
- [ ] Có cleanup stream trong `finally`?
- [ ] Có tôn trọng cancellation token?
- [ ] Có tách business logic khỏi service?
- [ ] Có test path reconnect/disconnect?

---

## 12) Glossary ngắn

- `Unary`: 1 request → 1 response.
- `Server-streaming`: 1 request → nhiều response theo thời gian.
- `IServerStreamWriter<T>`: đối tượng server dùng để đẩy message stream.
- `RpcException`: exception chuẩn để trả lỗi gRPC.
- `Proto`: file hợp đồng API của gRPC.

---

## 13) Kết luận

Module `Api/Grpc` hiện tại của bạn đã đi đúng hướng kiến trúc:

- Có contract rõ (`proto`).
- Có stream realtime chuẩn.
- Có thread-safe registry.
- Có bridge domain event qua abstraction (`ITicketAvailabilityNotifier`).
- Có fallback unary snapshot.

Nếu bạn muốn, bước tiếp theo mình có thể viết thêm:

1. `docs/grpc-client-react-guide.md` (React + gRPC-Web subscribe/reconnect), hoặc
2. `docs/grpc-interceptor-guide.md` (thiết kế interceptor logging/metrics/error mapping chuẩn production).
