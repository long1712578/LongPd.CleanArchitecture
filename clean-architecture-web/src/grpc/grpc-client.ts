import { GrpcWebFetchTransport } from "@protobuf-ts/grpcweb-transport";
import { TicketGrpcServiceClient } from "@/grpc-gen/ticket.client";

// Connect to the .NET 9 API (HTTPS port)
const transport = new GrpcWebFetchTransport({
  baseUrl: process.env.NEXT_PUBLIC_API_URL || "https://localhost:7121",
});

export const ticketClient = new TicketGrpcServiceClient(transport);
