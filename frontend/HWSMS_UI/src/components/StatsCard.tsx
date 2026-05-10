type StatsCardProps = {
  title: string;
  value: number | string;
  highlight?: boolean;
};

const StatsCard = ({ title, value, highlight = false }: StatsCardProps) => {
  return (
    <div className="hw-card w-60">
      <p className="text-xs uppercase tracking-[0.14em] text-slate-500">{title}</p>
      <h3 className={`mt-2 text-2xl font-bold ${highlight ? "text-[#b62b31]" : "text-slate-800"}`}>{value}</h3>
    </div>
  );
};

export default StatsCard;
