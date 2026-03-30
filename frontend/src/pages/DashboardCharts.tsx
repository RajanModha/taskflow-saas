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
        <ResponsiveContainer width="100%" height={280}>
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(15,23,42,0.08)" />
            <XAxis dataKey="status" />
            <YAxis allowDecimals={false} />
            <Tooltip />
            <Bar dataKey="count" fill="#6366f1" radius={[8, 8, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      <div className="chart-card">
        <h3>Status distribution</h3>
        <ResponsiveContainer width="100%" height={280}>
          <PieChart>
            <Pie
              data={data}
              dataKey="count"
              nameKey="status"
              cx="50%"
              cy="50%"
              outerRadius={95}
              label
            >
              {data.map((entry, index) => (
                <Cell key={entry.status} fill={PIE_COLORS[index % PIE_COLORS.length]} />
              ))}
            </Pie>
            <Tooltip />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

