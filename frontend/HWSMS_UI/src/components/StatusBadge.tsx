type StatusBadgeProps = {
  quantity: number;
};

const StatusBadge = ({ quantity }: StatusBadgeProps) => {
  const isLow = quantity < 10;

  return (
    <span className={`px-3 py-1 rounded-full text-sm font-medium ${isLow ? "bg-red-100 text-red-600" : "bg-green-100 text-green-600"}`}>
      {isLow ? "Low Stock" : "In Stock"}
    </span>
  );
};

export default StatusBadge;
