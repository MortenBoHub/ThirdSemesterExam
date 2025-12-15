import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

export const boardsApi = {
  async getActive() {
    const res = await customFetch.fetch(`${baseUrl}/api/Boards/active`, {
      method: "GET",
    });
    if (!res.ok) throw new Error("Failed to fetch active board");
    return await res.json();
  },

  async getParticipants() {
    const res = await customFetch.fetch(`${baseUrl}/api/Boards/participants`, {
      method: "GET",
    });
    if (!res.ok) throw new Error("Failed to fetch participants");
    return await res.json();
  },

  async getHistory(take: number = 10) {
    const res = await customFetch.fetch(`${baseUrl}/api/Boards/history?take=${take}`, {
      method: "GET",
    });
    if (!res.ok) throw new Error("Failed to fetch game history");
    return await res.json();
  },

  async draw(numbers: number[]) {
    const res = await customFetch.fetch(`${baseUrl}/api/Boards/draw`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ numbers }),
    });
    if (!res.ok) throw new Error("Failed to submit drawn numbers");
    return true;
  },
};
