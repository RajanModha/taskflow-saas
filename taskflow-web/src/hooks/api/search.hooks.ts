import { useQuery } from '@tanstack/react-query';
import api from '../../lib/api';
import type { SearchResultDto } from '../../types/api';

export function useSearch(q: string, options?: { limit?: number }) {
  const limit = options?.limit ?? 5;

  return useQuery({
    queryKey: ['search', q, limit],
    enabled: q.length >= 2,
    staleTime: 30_000,
    queryFn: async () => {
      const { data } = await api.get<SearchResultDto>('/Search', { params: { q, limit } });
      return data;
    },
  });
}
