interface MetricCardProps {
    name: string;
    value: number;
    date: string;
    unit?: string;
}

function MetricCard({
    name,
    value,
    date,
    unit
}: MetricCardProps) {
    return (
        <div className="metric-card">
            <h2>{name}</h2>

            <div className="metric-value">
                {value} {unit}
            </div>

            <div className="metric-date">
                {date}
            </div>
        </div>
    );
}

export default MetricCard;