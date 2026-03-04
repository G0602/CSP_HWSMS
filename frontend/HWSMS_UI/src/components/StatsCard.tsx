type StatsCardProps = {
  title: string;
  value: number | string;
  highlight?: boolean;
};

const StatsCard = ({ title, value, highlight = false }: StatsCardProps) => {
  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6 w-60">
      <p className="text-sm text-gray-500 uppercase">{title}</p>
      <h3 className={`text-2xl font-bold mt-2 ${highlight ? "text-red-500" : "text-gray-800"}`}>{value}</h3>
    </div>
  );
};

export default StatsCard;
