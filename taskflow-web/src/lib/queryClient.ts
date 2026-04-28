import { QueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { getApiError } from './api';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
    mutations: {
      onError: (error) => {
        toast.error(getApiError(error));
      },
    },
  },
});
