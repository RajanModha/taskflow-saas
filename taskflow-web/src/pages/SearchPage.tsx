import { CheckSquare, ExternalLink, FolderOpen, MessageSquare, Search, SearchX } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { TaskDetailSlideOver } from '../components/tasks/TaskDetailSlideOver';
import { Spinner } from '../components/ui/Spinner';
import { useSearch } from '../hooks/api/search.hooks';
import { useDebounce } from '../hooks/useDebounce';

export default function SearchPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get('q') ?? '';
  const [localQ, setLocalQ] = useState(q);
  const debouncedQ = useDebounce(localQ, 400);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const { data, isFetching } = useSearch(debouncedQ, { limit: 50 });

  useEffect(() => {
    setLocalQ(q);
  }, [q]);

  useEffect(() => {
    if (debouncedQ !== q) {
      setSearchParams({ q: debouncedQ }, { replace: true });
    }
  }, [debouncedQ, q, setSearchParams]);

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <h1 className="page-title">Search</h1>
      </div>

      <div className="relative mb-6 max-w-lg">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-neutral-400" />
        <input
          autoFocus
          value={localQ}
          onChange={(event) => setLocalQ(event.target.value)}
          placeholder="Search tasks, projects, comments..."
          className="h-10 w-full rounded-md border border-neutral-200 bg-white pl-10 pr-4 text-14 outline-none focus:border-primary-400 focus:ring-2 focus:ring-primary-100"
        />
        {isFetching ? <Spinner size="sm" className="absolute right-3 top-1/2 -translate-y-1/2" /> : null}
      </div>

      {data && debouncedQ.length >= 2 ? (
        <p className="mb-4 text-12 text-neutral-500">
          {data.totalResults} result{data.totalResults !== 1 ? 's' : ''} for "{data.query}"
        </p>
      ) : null}

      {debouncedQ.length < 2 ? (
        <div className="py-16 text-center">
          <Search className="mx-auto mb-3 h-10 w-10 text-neutral-200" />
          <p className="text-14 font-medium text-neutral-500">Type at least 2 characters to search</p>
        </div>
      ) : null}

      {data && data.totalResults === 0 && debouncedQ.length >= 2 ? (
        <div className="py-16 text-center">
          <SearchX className="mx-auto mb-3 h-10 w-10 text-neutral-200" />
          <p className="text-14 font-medium text-neutral-500">No results for "{debouncedQ}"</p>
          <p className="mt-1 text-12 text-neutral-400">Try different keywords or check your spelling</p>
        </div>
      ) : null}

      {data && data.totalResults > 0 ? (
        <div className="space-y-6">
          {data.projects.length > 0 ? (
            <section>
              <h2 className="section-heading mb-3">Projects ({data.projects.length})</h2>
              <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
                {data.projects.map((hit) => (
                  <button
                    key={hit.id}
                    className="flex w-full items-start gap-3 border-b px-4 py-3 text-left last:border-0 hover:bg-neutral-50"
                    onClick={() => navigate(`/projects/${hit.id}/board`)}
                  >
                    <FolderOpen className="mt-0.5 h-4 w-4 flex-shrink-0 text-neutral-400" />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-13 font-medium text-neutral-800">{hit.title}</p>
                      {hit.snippet ? <p className="mt-0.5 line-clamp-1 text-12 text-neutral-500">{hit.snippet}</p> : null}
                    </div>
                    <ExternalLink className="mt-0.5 h-3.5 w-3.5 flex-shrink-0 text-neutral-300" />
                  </button>
                ))}
              </div>
            </section>
          ) : null}

          {data.tasks.length > 0 ? (
            <section>
              <h2 className="section-heading mb-3">Tasks ({data.tasks.length})</h2>
              <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
                {data.tasks.map((hit) => {
                  const meta = hit.metadata as { status?: string; projectName?: string } | null;
                  return (
                    <button
                      key={hit.id}
                      className="flex w-full items-start gap-3 border-b px-4 py-3 text-left last:border-0 hover:bg-neutral-50"
                      onClick={() => setSelectedTaskId(hit.id)}
                    >
                      <CheckSquare className="mt-0.5 h-4 w-4 flex-shrink-0 text-primary-400" />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-13 font-medium text-neutral-800">{hit.title}</p>
                        {hit.snippet ? <p className="mt-0.5 line-clamp-2 text-12 text-neutral-500">{hit.snippet}</p> : null}
                        {meta?.projectName ? <p className="mt-1 text-11 text-neutral-400">in {meta.projectName}</p> : null}
                      </div>
                      {meta?.status ? (
                        <span className="mt-0.5 flex-shrink-0 rounded-sm bg-neutral-100 px-2 py-0.5 text-11 text-neutral-500">
                          {meta.status}
                        </span>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            </section>
          ) : null}

          {data.comments.length > 0 ? (
            <section>
              <h2 className="section-heading mb-3">Comments ({data.comments.length})</h2>
              <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
                {data.comments.map((hit) => (
                  <div key={hit.id} className="border-b px-4 py-3 last:border-0">
                    <div className="flex items-start gap-3">
                      <MessageSquare className="mt-0.5 h-4 w-4 flex-shrink-0 text-neutral-400" />
                      <div className="min-w-0 flex-1">
                        <p className="mb-1 text-12 text-neutral-500">
                          Comment on: <span className="font-medium text-neutral-700">{hit.title}</span>
                        </p>
                        {hit.snippet ? <p className="line-clamp-2 text-12 italic text-neutral-600">"{hit.snippet}"</p> : null}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          ) : null}
        </div>
      ) : null}

      {selectedTaskId ? <TaskDetailSlideOver taskId={selectedTaskId} onClose={() => setSelectedTaskId(null)} /> : null}
    </div>
  );
}
