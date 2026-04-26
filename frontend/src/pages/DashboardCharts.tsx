import { useMemo } from "react";
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  CartesianGrid,
  XAxis,
  YAxis,
  Tooltip,
  PieChart,
  Pie,
  Cell,
} from "recharts";
import type { DashboardStats } from "../api/dashboardApi";

const PIE_COLORS = ["#6366f1", "#4f46e5", "#818cf8", "#a5b4fc", "#4338ca"];

export function DashboardCharts({ stats }: { stats: DashboardStats }) {
  const data = useMemo(() => stats.tasksByStatus ?? [], [stats.tasksByStatus]);

  return (
    <div className="charts-grid">
      <div className="chart-card">
        <h3>Tasks by status</h3>
        <ResponsiveContainer width="100%" minHeight={280}>
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(15,23,42,0.08)" />
            <XAxis dataKey="status" tick={{ fontSize: 12, fill: "#64748b" }} />
            <YAxis allowDecimals={false} tick={{ fontSize: 12, fill: "#64748b" }} />
            <Tooltip />
            <Bar dataKey="count" fill="#6366f1" radius={[8, 8, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      <div className="chart-card">
        <h3>Status distribution</h3>
        <ResponsiveContainer width="100%" minHeight={280}>
          <PieChart>
            <Pie
              data={data}
              dataKey="count"
              nameKey="status"
              cx="50%"
              cy="47%"
              outerRadius={90}
            >
              {data.map((entry, index) => (
                <Cell key={entry.status} fill={PIE_COLORS[index % PIE_COLORS.length]} />
              ))}
            </Pie>
            <Tooltip />
          </PieChart>
        </ResponsiveContainer>
        <div className="pie-legend">
          {data.map((entry, index) => (
            <div key={entry.status} className="pie-legend-item">
              <span className="pie-dot" style={{ backgroundColor: PIE_COLORS[index % PIE_COLORS.length] }} />
              <span className="pie-label">{entry.status}</span>
              <span className="pie-count">{entry.count}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

