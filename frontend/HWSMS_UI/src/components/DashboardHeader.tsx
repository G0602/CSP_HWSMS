type DashboardHeaderProps = {
  totalValuation: string;
};

const DashboardHeader = ({ totalValuation }: DashboardHeaderProps) => {
  return (
    <div className="mb-8 flex items-start justify-between">
      <div>
        <h2 className="hw-title">Inventory Dashboard</h2>
        <p className="hw-subtitle">Monitor and update your store's hardware supplies.</p>
      </div>

      <div className="hw-kpi text-right">
        <p className="text-xs uppercase tracking-[0.16em] text-slate-500">Total Valuation</p>
        <h3 className="text-2xl font-bold text-[#c2500f]">Rs. {totalValuation}</h3>
      </div>
    </div>
  );
};

export default DashboardHeader;
