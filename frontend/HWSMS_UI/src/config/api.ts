const LOCAL_API_BASE_URL = "http://localhost:5162";
const PRODUCTION_API_BASE_URL = "https://hsmsbackend-e9acfpeff8bycuax.indonesiacentral-01.azurewebsites.net";

const normalizeBaseUrl = (value: string) => value.replace(/\/$/, "");

export const resolveApiBaseUrl = () => {
  const explicitBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (explicitBaseUrl) {
    return normalizeBaseUrl(explicitBaseUrl);
  }

  const legacyProductUrl = import.meta.env.VITE_API_URL as string | undefined;
  if (legacyProductUrl) {
    return normalizeBaseUrl(legacyProductUrl.replace(/\/api\/Product\/?$/i, ""));
  }

  return import.meta.env.PROD ? PRODUCTION_API_BASE_URL : LOCAL_API_BASE_URL;
};

export const API_BASE_URL = resolveApiBaseUrl();
