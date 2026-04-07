import { useHealthCheck } from "../hooks/useHealthCheck";
import { getApiBaseUrl } from "../services/healthService";

const API_BASE_URL = getApiBaseUrl();

const bannerBaseClassName =
  "sticky top-0 z-50 px-4 py-3 text-sm font-medium shadow-sm";

export default function BackendHealthBanner() {
  const { isHealthy, isChecking } = useHealthCheck();

  if (isHealthy) {
    return null;
  }

  if (isChecking) {
    return (
      <div className={`${bannerBaseClassName} bg-amber-100 text-amber-950`}>
        Checking backend connection at {API_BASE_URL}...
      </div>
    );
  }

  return (
    <div className={`${bannerBaseClassName} bg-red-600 text-white`}>
      The frontend cannot reach the backend right now. Some pages or actions may fail until the API
      is available at {API_BASE_URL}.
    </div>
  );
}
