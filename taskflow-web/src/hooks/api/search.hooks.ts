import { useQuery } from '@tanstack/react-query';
import api from '../../lib/api';
import type { SearchResultDto } from '../../types/api';

export function useSearch(q: string) {
  return useQuery({
    queryKey: ['search', q],
    enabled: q.length >= 2,
    staleTime: 30_000,
    queryFn: async () => {
      const { data } = await api.get<SearchResultDto>('/Search', { params: { q, limit: 5 } });
      return data;
    },
  });
}
