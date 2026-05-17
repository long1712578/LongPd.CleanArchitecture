ayer 1: Domain Layer (Pure C#, Zero NuGet dependencies)
Common:

IDomainEvent.cs
: Marker interface để phát tín hiệu event.

BaseEntity.cs
: Quản lý ID tự sinh phía client (Guid) và danh sách domain events.

AuditableEntity.cs
: Định nghĩa audit fields (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy).

ISoftDelete.cs
: Định nghĩa soft delete contract.
Entities (Rich Domain Model - Encapsulation hoàn hảo):

Event.cs
: Aggregate root quản lý sự kiện. Đóng gói phương thức .Publish(), .UpdateDetails().

Ticket.cs
: Thực thể hot-path. Đóng gói logic Atomic .Reserve() và .CancelReservation(), tích hợp cơ chế optimistic concurrency (xmin PostgreSQL).
Value Objects & Enums:

Money.cs
: Thực thể Money bất biến (immutable), tự bảo vệ invariant (không âm, currency match).

TicketStatus.cs
: Quản lý trạng thái (Available, Reserved, Sold, Cancelled).
Exceptions & Events:

DomainException.cs
: Exception gốc của Domain.

NotFoundException.cs
: Các Exception đặc thù (TicketNotFound, EventNotFound).

DomainEvents.cs
: Bản ghi event immutable (TicketReservedDomainEvent, v.v.).
Interfaces (Write-Side):

IRepository.cs
 (Không leak IQueryable).

IEventRepository.cs
 & 

ITicketRepository.cs

IUnitOfWork.cs
: Điều phối transaction cho Write-Side.
 Layer 2: Application Layer (MediatR CQRS + FluentValidation + Caching)
Common & Abstractions:

Result.cs
: Railway-oriented Programming với thực thể Result và Result<T> (không bao giờ throw exception ra ngoài API).

Error.cs
: Khai báo mã lỗi kiểu safe-type (Ticket.ConcurrencyConflict, Event.NotFound).

PagedList.cs
: Cấu trúc phân trang.

ICommand.cs
: Abstraction phân biệt Command (Write) và Query (Read).

ICurrentUserService.cs
: Abstraction lấy JWT Identity.

ICacheService.cs
 & 

IDbConnectionFactory.cs
Pipeline Behaviors (Cross-cutting Concerns):

LoggingBehavior.cs
: Tự động đo thời gian chạy và log payload request.

ValidationBehavior.cs
: Quét tự động và validate payload thông qua FluentValidation trước khi vào Handler.

CachingBehavior.cs
: Tự động chặn và lấy từ Cache (IMemoryCache) đối với các Query implement ICacheableQuery.
Vertical Slice Features:
CreateEvent: CreateEventCommand, CreateEventCommandValidator, CreateEventCommandHandler.
GetEventById (Read optimized - Dapper): GetEventByIdQuery, GetEventByIdQueryHandler.
ReserveTicket (Hot Path - Optimistic Concurrency Check): ReserveTicketCommand, ReserveTicketCommandValidator, ReserveTicketCommandHandler.
GetAvailableTickets (Read optimized - Dapper): GetAvailableTicketsQuery, GetAvailableTicketsQueryHandler.
Layer 3: Infrastructure Layer (EF Core PostgreSQL + Dapper connection + MemoryCache)
Persistence & Configuration:

AppDbContext.cs
: EF DbContext. Cấu hình chuyển múi giờ tự động sang UTC timestamp cho PostgreSQL.

EventConfiguration.cs
: Cấu hình fluent API, global query filter IsDeleted = false.

TicketConfiguration.cs
: Ánh xạ Money Value Object thành 2 column, đặc biệt sử dụng PostgreSQL xmin system column làm Concurrency Token (UseXminAsConcurrencyToken()).
Repositories & Transactions:
Repository<T>, EventRepository, TicketRepository.

UnitOfWork.cs
: Tự động quét ChangeTracker để điền thông tin audit (CreatedBy, v.v.) và dispatch Domain Event sau khi lưu database thành công.
Read Factory & Cache:

NpgsqlConnectionFactory.cs
: Khởi tạo kết nối raw PostgreSQL cho Dapper.

MemoryCacheService.cs
: Bộ nhớ đệm L1 cache (IMemoryCache) tối ưu, có tích hợp cơ chế xóa cache theo Prefix.
🌐 Layer 4: Presentation / API Layer (Minimal APIs + gRPC streaming + Exception Handling)
Endpoints:

IEndpointDefinition.cs
 & 

EndpointExtensions.cs
: Tự động phát hiện và đăng ký routes mà không cần khai báo tay.

EventEndpoints.cs
 & 

TicketEndpoints.cs
.
gRPC Layer (Server streaming):

ticket.proto
: Protobuf file định nghĩa stream real-time số lượng vé.

TicketGrpcService.cs
: Server Streamer quản lý danh sách client kết nối và broadcast real-time.

TicketDomainEventHandlers.cs
: MediatR NotificationHandler lắng nghe Domain Event từ database, sau đó đẩy data tức thì lên gRPC stream.
Cross-cutting concerns:

GlobalExceptionHandler.cs
: Bắt mọi exception bất thường chuyển sang RFC 7807 ProblemDetails tiêu chuẩn.

ResultExtensions.cs
: Map tự động Result sang Http Status code tương ứng.

CurrentUserService.cs
: Đọc JWT Claim lấy UserId hiện tại.

appsettings.json
: Cấu hình chuỗi kết nối PostgreSQL: Host=localhost;Port=5432;Database=TodoApp;Username=postgres;Password=123456.

Program.cs
: Khởi động siêu gọn nhẹ, tích hợp giao diện API Scalar, gRPC-Web (cho React), và tự động ánh xạ endpoints.
🛡️ Điểm Sáng Kiến Trúc Bạn Có Thể Học Hỏi (TechLead / Architect level)
PostgreSQL Optimistic Concurrency (xmin): Chúng ta không cần cột RowVersion byte[] thủ công nữa! Cấu hình builder.UseXminAsConcurrencyToken(); trong TicketConfiguration giúp EF Core tự động so sánh system column xmin của PostgreSQL. Khi xảy ra xung đột đặt vé đồng thời (Thundering herd), database sẽ chặn đứng và quăng lỗi DbUpdateConcurrencyException, giúp xử lý cực nhanh mà không bị khóa hàng chờ (lock congestion).
gRPC-Web Streaming Integration: Tích hợp trực tiếp UseGrpcWeb và MapGrpcService<TicketGrpcService>().EnableGrpcWeb(). Điều này giải quyết bài toán React JS Client chạy trên browser không kết nối được qua HTTP/2 gRPC gốc, cho phép stream dữ liệu real-time qua HTTP/1.1 cực kỳ mượt mà.
Dapper + EF Core Dual ORM (CQRS):
Ghi (Command): Dùng IUnitOfWork + EF Core để track state, tự động xử lý audit và dispatch Domain Event.
Đọc (Query): Inject IDbConnectionFactory và viết Dapper SQL raw để map kết quả nhanh kỷ lục (không tracking memory overhead).
Scalar API Documentation: Thay vì Swagger UI nhàm chán, tôi cấu hình Scalar API Reference với theme DeepSpace tuyệt đẹp tại đường dẫn Swagger mặc định để tạo ấn tượng mạnh về mặt mỹ thuật cho hệ thống của bạn.