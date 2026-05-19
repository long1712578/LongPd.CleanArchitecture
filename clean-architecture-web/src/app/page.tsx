"use client";

import React, { useState, useEffect } from "react";
import { useTicketAvailability } from "@/hooks/useTicketAvailability";
import {
  Ticket, Flame, ShieldAlert, CheckCircle, Wifi, WifiOff, Terminal, Plus, Minus,
  RefreshCw, Settings, User, PlusCircle, Trash2, Calendar, MapPin, Layers
} from "lucide-react";

interface TicketTierForm {
  tierName: string;
  priceAmount: number;
  priceCurrency: string;
  quantity: number;
}

interface EventSummary {
  id: string;
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  venue: string;
  totalCapacity: number;
  isPublished: boolean;
}

export default function Home() {
  // Navigation Tabs: 'customer' | 'admin'
  const [activeTab, setActiveTab] = useState<"customer" | "admin">("customer");

  // Active event for the customer realtime stream
  const [activeEventId, setActiveEventId] = useState("");

  const [activeEvents, setActiveEvents] = useState<EventSummary[]>([]);
  const [isFetchingEvents, setIsFetchingEvents] = useState(false);

  const fetchActiveEvents = async () => {
    setIsFetchingEvents(true);
    try {
      const res = await fetch("https://localhost:7121/api/events");
      if (res.ok) {
        const data = await res.json();
        setActiveEvents(data);
        // Tự động kết nối tới sự kiện đầu tiên nếu chưa chọn
        if (data.length > 0) {
          setActiveEventId((prev) => prev ? prev : data[0].id);
        }
      }
    } catch (e) {
      console.error("Failed to fetch events", e);
    } finally {
      setIsFetchingEvents(false);
    }
  };

  useEffect(() => {
    if (activeTab === "customer") {
      fetchActiveEvents();
    }
  }, [activeTab]);

  // Realtime hook
  const { availability, isConnected, error, refetch } = useTicketAvailability(activeEventId);
  const [actionLogs, setActionLogs] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState<Record<string, boolean>>({});

  // ─── ADMIN FORM STATES ──────────────────────────────────────────────────────
  const [eventName, setEventName] = useState("Live Concert Hà Anh Tuấn - Chân Trời Rực Rỡ 2026");
  const [eventDesc, setEventDesc] = useState("Đêm nhạc Live Concert đặc biệt quy tụ dàn âm thanh ánh sáng chuẩn quốc tế.");
  const [venue, setVenue] = useState("Sân Vận Động Hoa Lư, TP. Ninh Bình");
  const [capacity, setCapacity] = useState(1000);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");

  useEffect(() => {
    setStartDate(new Date().toISOString().split("T")[0]);
    setEndDate(new Date(Date.now() + 86400000).toISOString().split("T")[0]);
  }, []);

  const [ticketTiers, setTicketTiers] = useState<TicketTierForm[]>([
    { tierName: "VIP", priceAmount: 1500000, priceCurrency: "VND", quantity: 50 },
    { tierName: "Standard", priceAmount: 600000, priceCurrency: "VND", quantity: 200 }
  ]);

  const [createdEventId, setCreatedEventId] = useState<string | null>(null);
  const [adminError, setAdminError] = useState<string | null>(null);

  // Sync logs when availability gets new stream messages
  useEffect(() => {
    if (availability.length > 0) {
      const latestUpdate = availability[availability.length - 1];
      const timeStr = new Date().toLocaleTimeString();
      addLog(`[gRPC-Web Stream] ${timeStr} - Cập nhật tier '${latestUpdate.tierName}': Còn ${latestUpdate.availableQuantity}/${latestUpdate.totalQuantity} vé`);
    }
  }, [availability]);

  const addLog = (message: string) => {
    setActionLogs((prev) => [message, ...prev.slice(0, 19)]);
  };

  const handleApplyEvent = (id: string) => {
    const trimmedId = id.trim();
    if (!trimmedId) return;
    setActiveEventId(trimmedId);
    addLog(`[System] Đang kết nối tới Event ID: ${trimmedId}`);
  };

  // ─── ADMIN API ACTIONS ──────────────────────────────────────────────────────
  const handleAddTierRow = () => {
    setTicketTiers((prev) => [
      ...prev,
      { tierName: `Tier ${prev.length + 1}`, priceAmount: 500000, priceCurrency: "VND", quantity: 100 }
    ]);
  };

  const handleRemoveTierRow = (index: number) => {
    if (ticketTiers.length <= 1) return;
    setTicketTiers((prev) => prev.filter((_, i) => i !== index));
  };

  const handleTierChange = (index: number, field: keyof TicketTierForm, value: any) => {
    setTicketTiers((prev) => {
      const updated = [...prev];
      updated[index] = { ...updated[index], [field]: value };
      return updated;
    });
  };

  const handleCreateAndPublishEvent = async () => {
    setAdminError(null);
    setCreatedEventId(null);
    const timeStr = new Date().toLocaleTimeString();
    const actionKey = "create-event-flow";
    setIsSubmitting((prev) => ({ ...prev, [actionKey]: true }));

    try {
      addLog(`[REST Request] ${timeStr} - Bắt đầu luồng Tạo & Công bố sự kiện...`);

      // 1. Create Event (Draft state)
      const createResponse = await fetch("https://localhost:7121/api/events", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: eventName,
          description: eventDesc,
          startDate: new Date(startDate).toISOString(),
          endDate: new Date(endDate).toISOString(),
          venue: venue,
          totalCapacity: capacity
        })
      });

      if (!createResponse.ok) {
        const errorData = await createResponse.json().catch(() => ({}));
        throw new Error(errorData.detail || "Không thể tạo sự kiện nháp");
      }

      const createData = await createResponse.json();
      const newEventId = createData.id;
      addLog(`[REST Request] ${timeStr} - Đã tạo sự kiện nháp thành công. ID: ${newEventId}`);

      // 2. Publish Event with Ticket Tiers
      const publishResponse = await fetch("https://localhost:7121/api/events/publish", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          eventId: newEventId,
          ticketTiers: ticketTiers.map(t => ({
            tierName: t.tierName,
            priceAmount: t.priceAmount,
            priceCurrency: t.priceCurrency,
            quantity: t.quantity
          }))
        })
      });

      if (!publishResponse.ok) {
        const errorData = await publishResponse.json().catch(() => ({}));
        throw new Error(errorData.detail || "Không thể công bố sự kiện và tạo vé");
      }

      addLog(`[REST Request] ${timeStr} - Đã công bố sự kiện thành công! Vé đã sẵn sàng.`);
      setCreatedEventId(newEventId);
    } catch (err: any) {
      console.error(err);
      setAdminError(err.message);
      addLog(`[REST Request] ${timeStr} - Lỗi tiến trình admin: ${err.message}`);
    } finally {
      setIsSubmitting((prev) => ({ ...prev, [actionKey]: false }));
    }
  };

  const handleDeleteEvent = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation(); // Ngăn chọn sự kiện khi đang click nút Xóa
    if (!window.confirm("Bạn có chắc chắn muốn xóa sự kiện nháp này không? Thao tác này không thể hoàn tác.")) return;
    
    try {
      const res = await fetch(`https://localhost:7121/api/events/${id}`, { method: 'DELETE' });
      if (res.ok) {
        addLog(`[System] Đã xóa sự kiện ID: ${id}`);
        fetchActiveEvents(); // reload danh sách
      } else {
        const err = await res.json();
        alert(`Lỗi xóa: ${err.detail}`);
      }
    } catch (err) {
      console.error(err);
      alert("Lỗi mạng khi xóa sự kiện.");
    }
  };

  // ─── CUSTOMER REST ACTIONS ──────────────────────────────────────────────────
  const handleReserve = async (ticketId: string, count: number) => {
    const key = `reserve-${ticketId}`;
    setIsSubmitting((prev) => ({ ...prev, [key]: true }));
    const timeStr = new Date().toLocaleTimeString();

    try {
      const response = await fetch("https://localhost:7121/api/tickets/reserve", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Correlation-Id": crypto.randomUUID(),
        },
        body: JSON.stringify({
          ticketId,
          count,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.detail || "Đặt vé thất bại");
      }

      addLog(`[REST Request] ${timeStr} - Đặt thành công ${count} vé cho ticket ${ticketId}`);
    } catch (err: any) {
      console.error(err);
      addLog(`[REST Request] ${timeStr} - Lỗi đặt vé: ${err.message}`);
    } finally {
      setIsSubmitting((prev) => ({ ...prev, [key]: false }));
    }
  };

  const handleCancel = async (ticketId: string, count: number) => {
    const key = `cancel-${ticketId}`;
    setIsSubmitting((prev) => ({ ...prev, [key]: true }));
    const timeStr = new Date().toLocaleTimeString();

    try {
      const response = await fetch("https://localhost:7121/api/tickets/cancel", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          ticketId,
          count,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.detail || "Hủy đặt vé thất bại");
      }

      addLog(`[REST Request] ${timeStr} - Trả lại thành công ${count} vé cho ticket ${ticketId}`);
    } catch (err: any) {
      console.error(err);
      addLog(`[REST Request] ${timeStr} - Lỗi hủy đặt vé: ${err.message}`);
    } finally {
      setIsSubmitting((prev) => ({ ...prev, [key]: false }));
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-cyan-500 selection:text-slate-950 relative overflow-hidden">
      {/* Background gradients */}
      <div className="absolute top-0 left-0 w-full h-[500px] bg-gradient-to-b from-cyan-950/20 via-transparent to-transparent pointer-events-none" />
      <div className="absolute top-[20%] right-[10%] w-[300px] h-[300px] bg-violet-600/10 rounded-full blur-[120px] pointer-events-none" />
      <div className="absolute bottom-[20%] left-[5%] w-[400px] h-[400px] bg-cyan-600/10 rounded-full blur-[150px] pointer-events-none" />

      {/* Navigation Header */}
      <header className="sticky top-0 z-50 backdrop-blur-md bg-slate-950/80 border-b border-slate-800/80 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-cyan-500 to-violet-500 flex items-center justify-center shadow-lg shadow-cyan-500/20">
            <Ticket className="w-5 h-5 text-slate-950 stroke-[2.5]" />
          </div>
          <div>
            <h1 className="font-bold text-lg bg-gradient-to-r from-cyan-400 to-violet-400 bg-clip-text text-transparent">
              L-Ticket Clean Portal
            </h1>
            <p className="text-xs text-slate-400">Full-Stack Realtime MVP</p>
          </div>
        </div>

        {/* Tab Switchers */}
        <div className="flex bg-slate-900 border border-slate-800 p-1 rounded-xl">
          <button
            onClick={() => setActiveTab("customer")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition-all ${activeTab === "customer"
                ? "bg-gradient-to-r from-cyan-500 to-cyan-600 text-slate-950 font-bold"
                : "text-slate-400 hover:text-slate-200"
              }`}
          >
            <User className="w-3.5 h-3.5" />
            Mua Vé (Customer)
          </button>
          <button
            onClick={() => setActiveTab("admin")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition-all ${activeTab === "admin"
                ? "bg-gradient-to-r from-violet-500 to-violet-600 text-white font-bold"
                : "text-slate-400 hover:text-slate-200"
              }`}
          >
            <Settings className="w-3.5 h-3.5" />
            Tạo Vé & Event (Admin)
          </button>
        </div>

        {/* Status indicator */}
        <div className="hidden md:flex items-center gap-3">
          <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-semibold border backdrop-blur-sm transition-all duration-300 ${isConnected
              ? "bg-emerald-950/40 border-emerald-800 text-emerald-400"
              : "bg-rose-950/40 border-rose-800 text-rose-400 animate-pulse"
            }`}>
            {isConnected ? (
              <>
                <Wifi className="w-3.5 h-3.5 animate-bounce" />
                <span>gRPC-Web Live</span>
              </>
            ) : (
              <>
                <WifiOff className="w-3.5 h-3.5" />
                <span>Offline</span>
              </>
            )}
          </div>
        </div>
      </header>

      {/* Main Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto p-6 md:p-8 grid grid-cols-1 lg:grid-cols-12 gap-8 z-10">

        {/* Left Area (Interactive Screen based on Active Tab) */}
        <div className="lg:col-span-8 flex flex-col gap-6">

          {activeTab === "customer" ? (
            /* ==================================================================
               CUSTOMER VIEW (BUY TICKETS)
               ================================================================== */
            <>
              {/* Event Selector */}
              <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-xl shadow-xl">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-sm font-semibold uppercase tracking-wider text-cyan-400 flex items-center gap-2">
                    <Flame className="w-4 h-4" /> Danh Sách Sự Kiện (Chọn để kết nối)
                  </h2>
                  <button onClick={fetchActiveEvents} className="text-slate-400 hover:text-cyan-400" title="Tải lại">
                    <RefreshCw className={`w-4 h-4 ${isFetchingEvents ? "animate-spin" : ""}`} />
                  </button>
                </div>
                
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 max-h-[320px] overflow-y-auto pr-2">
                  {activeEvents.map(event => {
                    const now = new Date();
                    const start = new Date(event.startDate);
                    const end = new Date(event.endDate);
                    
                    let statusColor = "bg-slate-800 text-slate-400";
                    let statusText = "Chưa rõ";
                    let isExpired = false;
                    
                    if (now > end) {
                      statusColor = "bg-slate-900/80 border-slate-800 text-slate-500";
                      statusText = "Đã kết thúc";
                      isExpired = true;
                    } else if (now >= start && now <= end) {
                      statusColor = "bg-emerald-500/10 border-emerald-500/50 text-emerald-400";
                      statusText = "Đang diễn ra";
                    } else if (now < start) {
                      statusColor = "bg-cyan-500/10 border-cyan-500/50 text-cyan-400";
                      statusText = "Sắp diễn ra";
                    }

                    const isSelected = activeEventId === event.id;

                    return (
                      <div 
                        key={event.id}
                        onClick={() => handleApplyEvent(event.id)}
                        className={`p-4 rounded-xl border transition-all cursor-pointer flex flex-col gap-2 relative overflow-hidden ${
                          isSelected 
                            ? "bg-cyan-950/20 border-cyan-500 shadow-lg shadow-cyan-500/10 ring-1 ring-cyan-500" 
                            : isExpired
                              ? "bg-slate-950 border-slate-800/40 opacity-70 hover:opacity-100 grayscale hover:grayscale-0"
                              : "bg-slate-950 border-slate-800/80 hover:border-slate-700"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <h3 className={`font-bold text-sm leading-tight ${isSelected ? "text-cyan-400" : "text-white"} line-clamp-2`}>
                            {event.name}
                          </h3>
                          {!event.isPublished && (
                            <button
                              onClick={(e) => handleDeleteEvent(e, event.id)}
                              className="p-1.5 rounded bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 transition-colors border border-rose-500/20 hover:border-rose-500/50"
                              title="Xóa sự kiện nháp này"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </button>
                          )}
                        </div>
                        <div className="mt-1 flex flex-wrap gap-2">
                          <span className={`text-[10px] font-bold px-2 py-0.5 rounded border whitespace-nowrap ${statusColor}`}>
                            {statusText}
                          </span>
                          {!event.isPublished && (
                            <span className="text-[10px] font-bold px-2 py-0.5 rounded border bg-rose-500/10 border-rose-500/50 text-rose-400">
                              Chưa Publish
                            </span>
                          )}
                        </div>
                        <div className="flex items-center gap-1.5 text-xs text-slate-500 mt-auto pt-2 border-t border-slate-800/50">
                          <Calendar className="w-3 h-3" />
                          <span className="text-[11px]">{start.toLocaleDateString('vi-VN')} - {end.toLocaleDateString('vi-VN')}</span>
                        </div>
                      </div>
                    );
                  })}
                  
                  {activeEvents.length === 0 && !isFetchingEvents && (
                    <div className="col-span-full py-12 text-center text-slate-500 text-sm border border-dashed border-slate-800 rounded-xl">
                      <p className="mb-2">Không có sự kiện nào trong hệ thống.</p>
                      <button onClick={() => setActiveTab("admin")} className="text-cyan-400 hover:text-cyan-300 font-semibold underline underline-offset-4">Chuyển sang tab Admin để tạo mới</button>
                    </div>
                  )}
                </div>
                {error && (
                  <div className="mt-4 flex items-start gap-2 text-rose-400 text-xs bg-rose-950/20 border border-rose-900/50 p-3 rounded-lg">
                    <ShieldAlert className="w-4 h-4 shrink-0" />
                    <span>{error}</span>
                  </div>
                )}
              </div>

              {/* Event Live Detail card */}
              <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-xl shadow-xl flex-1 flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <div className="inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-violet-950/50 border border-violet-850 text-violet-400">
                      Chi tiết sự kiện (Realtime)
                    </div>
                    <button
                      onClick={refetch}
                      className="p-1.5 rounded-lg bg-slate-950 border border-slate-800 text-slate-400 hover:text-white transition-colors"
                      title="Làm mới Snapshot"
                    >
                      <RefreshCw className="w-3.5 h-3.5" />
                    </button>
                  </div>
                  <h3 className="text-xl md:text-2xl font-bold text-white mb-2">
                    {activeEvents.find(e => e.id === activeEventId)?.name || "Chưa chọn sự kiện"}
                  </h3>
                  <p className="text-slate-400 text-sm mb-6 leading-relaxed flex items-center gap-2">
                    <span className="bg-slate-900 px-2 py-1 rounded text-xs">Event ID:</span> 
                    <span className="font-mono text-cyan-400 text-xs break-all">{activeEventId || "N/A"}</span>
                  </p>

                  {/* Tiers List */}
                  <div className="flex flex-col gap-4">
                    {availability.length === 0 ? (
                      <div className="text-center py-16 border border-dashed border-slate-800 rounded-xl text-slate-500 flex flex-col items-center justify-center gap-3">
                        <Layers className="w-8 h-8 text-slate-600 animate-pulse" />
                        <div>
                          <p className="font-semibold text-slate-400">Chưa có thông tin vé hoặc sự kiện chưa công bố</p>
                          <p className="text-xs text-slate-600 mt-1">Sang tab "Tạo Vé & Event (Admin)" để tạo một sự kiện mới và trải nghiệm!</p>
                        </div>
                      </div>
                    ) : (
                      availability.map((tier) => {
                        const ratio = tier.totalQuantity > 0 ? (tier.availableQuantity / tier.totalQuantity) * 100 : 0;
                        const isSoldOut = tier.availableQuantity === 0 || tier.isSoldOut;

                        const isVip = tier.tierName.toLowerCase().includes("vip");
                        const themeColor = isVip ? "from-violet-500 to-fuchsia-500" : "from-cyan-500 to-teal-500";
                        const progressColor = isVip ? "bg-gradient-to-r from-violet-500 to-fuchsia-500" : "bg-gradient-to-r from-cyan-500 to-teal-500";

                        const selectedEvent = activeEvents.find(e => e.id === activeEventId);
                        const isExpired = selectedEvent ? new Date() > new Date(selectedEvent.endDate) : false;
                        const isNotPublished = selectedEvent ? !selectedEvent.isPublished : false;
                        
                        const isBuyDisabled = isSoldOut || isSubmitting[`reserve-${tier.ticketId}`] || isExpired || isNotPublished;
                        const isCancelDisabled = isSubmitting[`cancel-${tier.ticketId}`] || isExpired || isNotPublished;

                        let buyBtnText = "MUA 1 VÉ";
                        if (isExpired) buyBtnText = "ĐÃ KẾT THÚC";
                        else if (isNotPublished) buyBtnText = "CHƯA PUBLISH";
                        else if (isSoldOut) buyBtnText = "HẾT VÉ";

                        return (
                          <div key={tier.ticketId} className={`p-4 rounded-xl border transition-all duration-300 ${isExpired || isNotPublished ? "bg-slate-950/50 border-slate-800/40 grayscale opacity-80" : "bg-slate-950 border-slate-800/80 hover:border-slate-700/80"}`}>
                            <div className="flex items-center justify-between mb-2">
                              <div className="flex items-center gap-2">
                                <span className={`px-2.5 py-0.5 rounded text-[10px] font-extrabold tracking-wider uppercase bg-gradient-to-r ${themeColor} text-slate-950`}>
                                  {tier.tierName}
                                </span>
                                <span className="text-xs text-slate-500 font-mono">ID: {tier.ticketId.slice(0, 8)}...</span>
                              </div>

                              <div className="flex items-baseline gap-1 text-sm font-semibold">
                                <span className="text-lg text-white font-mono">{tier.availableQuantity}</span>
                                <span className="text-slate-500 font-mono">/ {tier.totalQuantity} còn lại</span>
                              </div>
                            </div>

                            {/* Progress bar */}
                            <div className="w-full h-2 bg-slate-900 rounded-full overflow-hidden mb-4 border border-slate-850">
                              <div
                                className={`h-full rounded-full transition-all duration-500 ease-out ${progressColor}`}
                                style={{ width: `${ratio}%` }}
                              />
                            </div>

                            {/* Action Buttons */}
                            <div className="flex gap-3">
                              <button
                                onClick={() => handleReserve(tier.ticketId, 1)}
                                disabled={isBuyDisabled}
                                className={`flex-1 flex items-center justify-center gap-2 py-2 px-4 rounded-lg font-bold text-xs transition-all active:scale-95 ${
                                    isBuyDisabled
                                    ? "bg-slate-900 border border-slate-800 text-slate-600 cursor-not-allowed"
                                    : "bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-md shadow-cyan-500/5"
                                  }`}
                              >
                                {isSubmitting[`reserve-${tier.ticketId}`] ? (
                                  <div className="w-4 h-4 border-2 border-slate-950 border-t-transparent rounded-full animate-spin" />
                                ) : (
                                  <Plus className="w-3.5 h-3.5 stroke-[3]" />
                                )}
                                {buyBtnText}
                              </button>

                              <button
                                onClick={() => handleCancel(tier.ticketId, 1)}
                                disabled={isCancelDisabled}
                                className={`font-semibold py-2 px-4 rounded-lg text-xs transition-all flex items-center gap-2 ${
                                  isCancelDisabled 
                                    ? "bg-slate-900 border border-slate-800 text-slate-700 cursor-not-allowed" 
                                    : "bg-slate-900 hover:bg-slate-800 border border-slate-850 hover:border-slate-700 text-slate-300 active:scale-95"
                                }`}
                                title="Trả lại vé (Hủy vé)"
                              >
                                {isSubmitting[`cancel-${tier.ticketId}`] ? (
                                  <div className="w-4 h-4 border-2 border-slate-300 border-t-transparent rounded-full animate-spin" />
                                ) : (
                                  <Minus className="w-3.5 h-3.5" />
                                )}
                                HỦY VÉ
                              </button>
                            </div>
                          </div>
                        );
                      })
                    )}
                  </div>
                </div>

                <div className="mt-6 pt-4 border-t border-slate-800/80 flex items-center gap-2 text-xs text-slate-500">
                  <CheckCircle className="w-4 h-4 text-cyan-500 shrink-0" />
                  <span>Mua hoặc Hủy vé sẽ kích hoạt REST API sinh Domain Event trên BE  broadcast realtime qua gRPC-Web stream.</span>
                </div>
              </div>
            </>
          ) : (
            /* ==================================================================
               ADMIN VIEW (CREATE & PUBLISH EVENT)
               ================================================================== */
            <div className="p-6 rounded-2xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-xl shadow-xl flex flex-col gap-6">
              <div>
                <h2 className="text-xl font-bold text-white mb-1 flex items-center gap-2">
                  <PlusCircle className="w-5 h-5 text-violet-400" /> Tạo & Công Bố Sự Kiện Mới
                </h2>
                <p className="text-xs text-slate-400">
                  Hệ thống sẽ thực hiện 2 bước tự động: Tạo sự kiện nháp (REST) và Thiết lập các hạng vé (Publish REST API).
                </p>
              </div>

              {/* Event Form fields */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold text-slate-400 uppercase">Tên sự kiện</label>
                  <input
                    type="text"
                    value={eventName}
                    onChange={(e) => setEventName(e.target.value)}
                    className="bg-slate-950 border border-slate-850 focus:border-violet-500 rounded-xl px-4 py-2.5 text-sm focus:outline-none text-slate-200"
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold text-slate-400 uppercase">Tổng Sức Chứa (Capacity)</label>
                  <input
                    type="number"
                    value={capacity}
                    onChange={(e) => setCapacity(Number(e.target.value))}
                    className="bg-slate-950 border border-slate-850 focus:border-violet-500 rounded-xl px-4 py-2.5 text-sm focus:outline-none text-slate-200"
                  />
                </div>
                <div className="flex flex-col gap-2 md:col-span-2">
                  <label className="text-xs font-semibold text-slate-400 uppercase">Mô tả chi tiết</label>
                  <textarea
                    value={eventDesc}
                    onChange={(e) => setEventDesc(e.target.value)}
                    rows={2}
                    className="bg-slate-950 border border-slate-850 focus:border-violet-500 rounded-xl px-4 py-2.5 text-sm focus:outline-none text-slate-200"
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold text-slate-400 uppercase flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-slate-500" /> Địa điểm tổ chức
                  </label>
                  <input
                    type="text"
                    value={venue}
                    onChange={(e) => setVenue(e.target.value)}
                    className="bg-slate-950 border border-slate-850 focus:border-violet-500 rounded-xl px-4 py-2.5 text-sm focus:outline-none text-slate-200"
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="flex flex-col gap-2">
                    <label className="text-xs font-semibold text-slate-400 uppercase flex items-center gap-1">
                      <Calendar className="w-3.5 h-3.5 text-slate-500" /> Ngày bắt đầu
                    </label>
                    <input
                      type="date"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                      className="bg-slate-950 border border-slate-855 rounded-xl px-3 py-2 text-sm focus:outline-none text-slate-200 font-mono"
                    />
                  </div>
                  <div className="flex flex-col gap-2">
                    <label className="text-xs font-semibold text-slate-400 uppercase flex items-center gap-1">
                      <Calendar className="w-3.5 h-3.5 text-slate-500" /> Ngày kết thúc
                    </label>
                    <input
                      type="date"
                      value={endDate}
                      onChange={(e) => setEndDate(e.target.value)}
                      className="bg-slate-950 border border-slate-855 rounded-xl px-3 py-2 text-sm focus:outline-none text-slate-200 font-mono"
                    />
                  </div>
                </div>
              </div>

              {/* Tiers List Config */}
              <div className="border-t border-slate-850 pt-4 mt-2">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                    <Layers className="w-4 h-4 text-violet-400" /> Thiết lập các hạng vé
                  </h3>
                  <button
                    onClick={handleAddTierRow}
                    className="text-xs font-bold text-violet-400 hover:text-violet-300 flex items-center gap-1 bg-violet-955/30 border border-violet-900 px-3 py-1.5 rounded-lg transition-all"
                  >
                    <Plus className="w-3.5 h-3.5" /> Thêm hạng vé
                  </button>
                </div>

                <div className="flex flex-col gap-3">
                  {ticketTiers.map((tier, index) => (
                    <div key={index} className="flex flex-col sm:flex-row gap-3 items-end sm:items-center bg-slate-950 p-3 rounded-xl border border-slate-855 relative">
                      <div className="flex-1 w-full grid grid-cols-1 sm:grid-cols-3 gap-3">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[10px] font-bold text-slate-500 uppercase">Tên hạng vé</label>
                          <input
                            type="text"
                            value={tier.tierName}
                            placeholder="VIP, Standard..."
                            onChange={(e) => handleTierChange(index, "tierName", e.target.value)}
                            className="bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-violet-500"
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[10px] font-bold text-slate-500 uppercase">Giá vé (VND)</label>
                          <input
                            type="number"
                            value={tier.priceAmount}
                            onChange={(e) => handleTierChange(index, "priceAmount", Number(e.target.value))}
                            className="bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-violet-500"
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[10px] font-bold text-slate-500 uppercase">Số lượng vé bán</label>
                          <input
                            type="number"
                            value={tier.quantity}
                            onChange={(e) => handleTierChange(index, "quantity", Number(e.target.value))}
                            className="bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-violet-500 font-mono"
                          />
                        </div>
                      </div>
                      <button
                        onClick={() => handleRemoveTierRow(index)}
                        disabled={ticketTiers.length <= 1}
                        className={`p-2 rounded-lg border text-rose-400 transition-colors ${ticketTiers.length <= 1
                            ? "border-slate-900 text-slate-700 cursor-not-allowed"
                            : "border-slate-800 bg-slate-900 hover:bg-slate-850 hover:border-slate-700"
                          }`}
                        title="Xóa hạng vé này"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  ))}
                </div>
              </div>

              {/* Actions & Alerts */}
              <div className="border-t border-slate-850 pt-4 mt-2 flex flex-col gap-4">
                {adminError && (
                  <div className="flex items-start gap-2 text-rose-400 text-xs bg-rose-950/20 border border-rose-900/50 p-3 rounded-lg">
                    <ShieldAlert className="w-4 h-4 shrink-0" />
                    <span>Lỗi: {adminError}</span>
                  </div>
                )}

                {createdEventId && (
                  <div className="flex flex-col gap-3 text-emerald-400 text-xs bg-emerald-955/20 border border-emerald-900/50 p-4 rounded-xl">
                    <div className="flex items-start gap-2">
                      <CheckCircle className="w-4 h-4 shrink-0 text-emerald-400" />
                      <div>
                        <p className="font-bold text-sm">Tạo & Công Bố Thành Công!</p>
                        <p className="text-slate-400 mt-1">Sự kiện đã được publish và tạo vé trên Database thành công.</p>
                        <p className="font-mono text-cyan-400 mt-1.5 text-[11px]">Event ID: {createdEventId}</p>
                      </div>
                    </div>
                    <button
                      onClick={() => {
                        handleApplyEvent(createdEventId);
                        setActiveTab("customer");
                        setCreatedEventId(null);
                      }}
                      className="bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold py-2 px-4 rounded-lg transition-all text-xs"
                    >
                      Kết nối gRPC Stream sự kiện này ngay lập tức →
                    </button>
                  </div>
                )}

                <button
                  onClick={handleCreateAndPublishEvent}
                  disabled={isSubmitting["create-event-flow"]}
                  className="w-full bg-gradient-to-r from-violet-500 to-fuchsia-600 hover:from-violet-400 hover:to-fuchsia-500 text-white font-bold py-3.5 px-6 rounded-xl transition-all shadow-lg shadow-violet-500/10 flex items-center justify-center gap-2 text-sm active:scale-98"
                >
                  {isSubmitting["create-event-flow"] ? (
                    <>
                      <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                      <span>Đang xử lý tạo & công bố...</span>
                    </>
                  ) : (
                    <span>Tạo & Công Bố Sự Kiện (Publish Event)</span>
                  )}
                </button>
              </div>

            </div>
          )}

        </div>

        {/* Right Column: Real-time Terminal Log Console */}
        <div className="lg:col-span-4 flex flex-col">
          <div className="flex-1 p-6 rounded-2xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-xl shadow-xl flex flex-col h-[500px] lg:h-auto">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-violet-400 mb-4 flex items-center gap-2 shrink-0">
              <Terminal className="w-4 h-4" /> Terminal Logs (gRPC-Web)
            </h2>

            {/* Log viewport */}
            <div className="flex-1 bg-slate-950 border border-slate-850 rounded-xl p-4 font-mono text-xs overflow-y-auto flex flex-col gap-2 shadow-inner">
              {actionLogs.length === 0 ? (
                <div className="text-slate-600 italic">Đang chờ tín hiệu stream từ gRPC server...</div>
              ) : (
                actionLogs.map((log, index) => {
                  let colorClass = "text-slate-400";
                  if (log.includes("[gRPC-Web Stream]")) colorClass = "text-cyan-400";
                  if (log.includes("Lỗi")) colorClass = "text-rose-400 font-semibold";
                  if (log.includes("đặt thành công") || log.includes("trả lại thành công") || log.includes("thành công!")) colorClass = "text-emerald-400";

                  return (
                    <div key={index} className={`pb-1.5 border-b border-slate-900 last:border-0 ${colorClass} break-all`}>
                      {log}
                    </div>
                  );
                })
              )}
            </div>

            <div className="mt-4 flex justify-between items-center text-[10px] text-slate-500">
              <span>Hiển thị tối đa 20 logs mới nhất</span>
              <button
                onClick={() => setActionLogs([])}
                className="hover:underline text-cyan-500 hover:text-cyan-400"
              >
                Xóa màn hình
              </button>
            </div>
          </div>
        </div>

      </main>

      {/* Footer */}
      <footer className="py-6 text-center text-xs text-slate-650 border-t border-slate-900 mt-auto">
        <p>© 2026 LongPd.CleanArchitecture. Built with .NET 9, Next.js, and gRPC-Web.</p>
      </footer>
    </div>
  );
}
