export default function SettingsTagsPage() {
  return (
    <div>
      <h2 className="mb-1 text-16 font-semibold text-neutral-800">Tags</h2>
      <p className="mb-5 text-13 text-neutral-500">Manage reusable task tags.</p>

      <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
        <table className="w-full border-collapse text-13">
          <thead>
            <tr className="border-b border-neutral-200 bg-neutral-50">
              <th className="h-9 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Tag</th>
              <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Usage</th>
            </tr>
          </thead>
          <tbody>
            <tr className="h-9 border-b border-neutral-100">
              <td className="px-3 text-neutral-800">frontend</td>
              <td className="px-3 text-neutral-500">42 tasks</td>
            </tr>
            <tr className="h-9 border-b border-neutral-100">
              <td className="px-3 text-neutral-800">backend</td>
              <td className="px-3 text-neutral-500">37 tasks</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
