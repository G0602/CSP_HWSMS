type DashboardHeaderProps = {
  totalValuation: string;
};

const DashboardHeader = ({ totalValuation }: DashboardHeaderProps) => {
  return (
    <div className="flex justify-between items-start mb-8">
      <div>
        <h2 className="text-3xl font-bold text-gray-800">Inventory Dashboard</h2>
        <p className="text-gray-500 mt-1">Monitor and update your store's hardware supplies.</p>
      </div>

      <div className="text-right">
        <p className="text-sm text-gray-500 uppercase tracking-wide">Total Valuation</p>
        <h3 className="text-2xl font-bold text-blue-600">Rs. {totalValuation}</h3>
      </div>
    </div>
  );
};

export default DashboardHeader;
