import { useEffect, useState } from "react";
import { checkBackendHealth } from "../services/healthService";

export const useHealthCheck = () => {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null);
  const [isChecking, setIsChecking] = useState(true);

  useEffect(() => {
    let isMounted = true;
    let intervalId: number | null = null;

    const performHealthCheck = async () => {
      try {
        const healthy = await checkBackendHealth();
        if (!isMounted) {
          return;
        }

        setIsHealthy(healthy);
        setIsChecking(false);
        if (healthy) {
          if (intervalId) {
            window.clearInterval(intervalId);
            intervalId = null;
          }
        }
      } catch {
        if (!isMounted) {
          return;
        }

        setIsHealthy(false);
        setIsChecking(false);
      }
    };

    performHealthCheck();
    intervalId = window.setInterval(performHealthCheck, 5000);

    return () => {
      isMounted = false;
      if (intervalId) {
        window.clearInterval(intervalId);
      }
    };
  }, []);

  return { isHealthy, isChecking };
};
