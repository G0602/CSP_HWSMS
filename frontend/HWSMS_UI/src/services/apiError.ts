import axios from "axios";

export const getApiErrorMessage = (error: unknown, fallback: string) => {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  const data = error.response?.data;
  if (typeof data === "string" && data.trim()) {
    return data;
  }

  if (data && typeof data === "object" && "title" in data && typeof data.title === "string") {
    return data.title;
  }

  return fallback;
};

export const isUnauthorized = (error: unknown) => axios.isAxiosError(error) && error.response?.status === 401;

export const isForbidden = (error: unknown) => axios.isAxiosError(error) && error.response?.status === 403;
