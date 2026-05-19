"use client";

import { useEffect, useState, useRef } from "react";
import { ticketClient } from "@/grpc/grpc-client";
import { AvailabilityUpdate } from "@/grpc-gen/ticket";

export function useTicketAvailability(eventId: string) {
  const [availability, setAvailability] = useState<AvailabilityUpdate[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Keep track of abort controllers/streams to cancel properly
  const abortControllerRef = useRef<AbortController | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const retryCountRef = useRef(0);

  const connectStream = () => {
    if (!eventId) return;

    // Clean up previous attempts
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    setError(null);

    console.log(`[gRPC-Web] Subscribing to StreamAvailability for event: ${eventId}`);

    // Call the server-streaming gRPC method
    const stream = ticketClient.streamAvailability(
      { eventId },
      { abort: abortController.signal }
    );

    setIsConnected(true);
    retryCountRef.current = 0; // Reset retry count on successful connection start

    // Read updates from the stream asynchronously
    (async () => {
      try {
        for await (const message of stream.responses) {
          console.log("[gRPC-Web] Received update:", message);
          
          setAvailability((prev) => {
            // Update or add the tier update
            const index = prev.findIndex((item) => item.ticketId === message.ticketId);
            if (index >= 0) {
              const updated = [...prev];
              updated[index] = message;
              return updated;
            }
            return [...prev, message];
          });
        }
      } catch (err: any) {
        // If aborted intentionally, ignore the error
        if (abortController.signal.aborted) {
          console.log("[gRPC-Web] Stream aborted intentionally.");
          return;
        }

        console.error("[gRPC-Web] Stream error:", err);
        setIsConnected(false);
        setError("Kết nối bị gián đoạn. Đang kết nối lại...");

        // Auto-reconnect with exponential backoff (max 30s)
        const delay = Math.min(1000 * Math.pow(2, retryCountRef.current), 30000);
        retryCountRef.current += 1;
        
        console.log(`[gRPC-Web] Attempting reconnect in ${delay}ms...`);
        reconnectTimeoutRef.current = setTimeout(() => {
          // Fetch snapshot once before reconnecting stream to resync
          syncSnapshot();
          connectStream();
        }, delay);
      }
    })();
  };

  // Helper to fetch current snapshot (Unary call) to sync data
  const syncSnapshot = async () => {
    if (!eventId) return;
    try {
      console.log(`[gRPC-Web] Fetching snapshot for event: ${eventId}`);
      const response = await ticketClient.getCurrentAvailability({ eventId });
      setAvailability(response.response.tiers);
    } catch (err) {
      console.error("[gRPC-Web] Failed to fetch availability snapshot:", err);
    }
  };

  useEffect(() => {
    // Initial fetch of snapshot
    syncSnapshot();
    
    // Connect to realtime stream
    connectStream();

    return () => {
      // Cleanup on unmount
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
    };
  }, [eventId]);

  return { availability, isConnected, error, refetch: syncSnapshot };
}
