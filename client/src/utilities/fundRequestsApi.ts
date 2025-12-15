import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

export interface CreateFundRequestDto {
  amount: number;
  transactionNumber: string;
}

export type FundRequestStatus = "pending" | "approved" | "denied";

export const fundRequestsApi = {
  async create(dto: CreateFundRequestDto) {
    const res = await customFetch.fetch(`${baseUrl}/api/FundRequests`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto),
    });
    if (!res.ok) throw new Error("Failed to create fund request");
    return await res.json();
  },

  async list(status?: FundRequestStatus) {
    const qs = status ? `?status=${encodeURIComponent(status)}` : "";
    const res = await customFetch.fetch(`${baseUrl}/api/FundRequests${qs}`, {
      method: "GET",
    });
    if (!res.ok) throw new Error("Failed to fetch fund requests");
    return await res.json();
  },

  async approve(id: string) {
    const res = await customFetch.fetch(`${baseUrl}/api/FundRequests/${encodeURIComponent(id)}/approve`, {
      method: "POST",
    });
    if (!res.ok) throw new Error("Failed to approve fund request");
    return await res.json();
  },

  async deny(id: string) {
    const res = await customFetch.fetch(`${baseUrl}/api/FundRequests/${encodeURIComponent(id)}/deny`, {
      method: "POST",
    });
    if (!res.ok) throw new Error("Failed to deny fund request");
    return await res.json();
  },
};
