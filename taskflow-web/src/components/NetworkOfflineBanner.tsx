import { WifiOff } from 'lucide-react';
import { useEffect, useState } from 'react';

export function NetworkOfflineBanner() {
  const [offline, setOffline] = useState(!navigator.onLine);

  useEffect(() => {
    const on = () => setOffline(false);
    const off = () => setOffline(true);
    window.addEventListener('online', on);
    window.addEventListener('offline', off);
    return () => {
      window.removeEventListener('online', on);
      window.removeEventListener('offline', off);
    };
  }, []);

  if (!offline) return null;

  return (
    <div className="fixed left-0 right-0 top-0 z-[100] flex items-center justify-center gap-2 bg-amber-500 py-2 text-center text-13 font-medium text-white">
      <WifiOff className="h-4 w-4" />
      You're offline. Some features may be unavailable.
    </div>
  );
}
