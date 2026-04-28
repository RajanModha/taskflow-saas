import { useQuery } from '@tanstack/react-query';
import api from '../../lib/api';
import type { DashboardMyStatsDto, DashboardStatsDto } from '../../types/api';

export function useDashboardStats() {
  return useQuery({
    queryKey: ['dashboard', 'stats'],
    staleTime: 60_000,
    queryFn: async () => {
      const { data } = await api.get<DashboardStatsDto>('/Dashboard/stats');
      return data;
    },
  });
}

export function useMyStats() {
  return useQuery({
    queryKey: ['dashboard', 'my-stats'],
    staleTime: 30_000,
    queryFn: async () => {
      const { data } = await api.get<DashboardMyStatsDto>('/Dashboard/my-stats');
      return data;
    },
  });
}
