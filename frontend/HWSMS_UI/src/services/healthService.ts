import axios from "axios";
import { API_BASE_URL } from "../config/api";

export type HealthCheckResponse = {
  status: string;
  timestamp: string;
};

const HEALTH_CHECK_URL = `${API_BASE_URL}/api/health`;

// Create a separate Axios instance for health checks to avoid timeout issues
const healthCheckAxios = axios.create({
  timeout: 5000, // 5 second timeout for health checks
});

export const checkBackendHealth = async (): Promise<boolean> => {
  try {
    const response = await healthCheckAxios.get<HealthCheckResponse>(HEALTH_CHECK_URL);
    return response.status === 200 && response.data.status === "healthy";
  } catch {
    return false;
  }
};

export const getApiBaseUrl = () => API_BASE_URL;
