type StatusBadgeProps = {
  quantity: number;
};

const StatusBadge = ({ quantity }: StatusBadgeProps) => {
  const isLow = quantity < 10;

  return (
    <span
      className={`rounded-full px-3 py-1 text-sm font-semibold ${isLow ? "bg-[#fce4e5] text-[#a8272d]" : "bg-[#dff5eb] text-[#0f7a5a]"}`}
    >
      {isLow ? "Low Stock" : "In Stock"}
    </span>
  );
};

export default StatusBadge;
