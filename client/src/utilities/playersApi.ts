import { baseUrl } from "@core/baseUrl.ts";
import { customFetch } from "@utilities/customFetch.ts";

export interface CreatePlayerRequestDto {
  name: string;
  email: string;
  phoneNumber: string;
  password: string;
}

export interface CreatePlayerBoardsRequestDto {
  selectedNumbers: number[];
  repeatWeeks: number;
}

export const playersApi = {
  async getPlayers() {
    const res = await customFetch.fetch(`${baseUrl}/api/Players`, {
      method: "GET",
    });
    if (!res.ok) throw new Error("Failed to fetch players");
    return await res.json();
  },

  async getPlayer(id: string) {
    const res = await customFetch.fetch(`${baseUrl}/api/Players/${encodeURIComponent(id)}`, {
      method: "GET",
    });
    if (!res.ok) throw new Error("Failed to fetch player");
    return await res.json();
  },

  async createPlayer(dto: CreatePlayerRequestDto) {
    const res = await customFetch.fetch(`${baseUrl}/api/Players`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto),
    });
    if (!res.ok) throw new Error("Failed to create player");
    return (await res.json());
  },

  async createBoards(playerId: string, dto: CreatePlayerBoardsRequestDto) {
    const res = await customFetch.fetch(
      `${baseUrl}/api/Players/${encodeURIComponent(playerId)}/boards`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto),
      }
    );
    if (!res.ok) throw new Error("Failed to create boards");
    return (await res.json());
  },

  async updatePlayer(id: string, dto: { name?: string; email?: string; phonenumber?: string }) {
    const res = await customFetch.fetch(`${baseUrl}/api/Players/${encodeURIComponent(id)}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto),
    });
    if (!res.ok) throw new Error("Failed to update player");
    return await res.json();
  },

  async changePassword(id: string, currentPassword: string, newPassword: string) {
    const res = await customFetch.fetch(
      `${baseUrl}/api/Players/${encodeURIComponent(id)}/change-password`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ currentPassword, newPassword }),
      }
    );
    if (!res.ok) throw new Error("Failed to change password");
    return true;
  },

  async softDelete(id: string) {
    const res = await customFetch.fetch(`${baseUrl}/api/Players/${encodeURIComponent(id)}/soft-delete`, {
      method: "PATCH",
    });
    if (!res.ok) throw new Error("Failed to soft-delete player");
    return await res.json();
  },

  async restore(id: string) {
    const res = await customFetch.fetch(`${baseUrl}/api/Players/${encodeURIComponent(id)}/restore`, {
      method: "PATCH",
    });
    if (!res.ok) throw new Error("Failed to restore player");
    return await res.json();
  },
};
