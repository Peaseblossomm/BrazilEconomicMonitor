import heroImg from './assets/hero.png'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import './App.css'
import MetricCard from "./MetricCard";
import { useEffect, useState } from "react";
import "./App.css";

interface Observation {
    date: string;
    value: number;
}
interface Metric {
    name: string;
    value: number;
    date: string;
    unit?: string;
}

function App() {

    const [metrics, setMetrics] =
        useState<Metric[]>([]);

    useEffect(() => {
        async function loadMetrics() {
            const gdpResponse = await fetch(
                "https://localhost:7206/api/dashboard/series/4382/1"
            );

            const gdp: Observation[] =
                await gdpResponse.json();

            const primaryBalanceResponse = await fetch(
                "https://localhost:7206/api/dashboard/series/10.07.1_TTM/1"
            );

            const primaryBalance: Observation[] =
                await primaryBalanceResponse.json();

            const loadedMetrics: Metric[] = [
                {
                    name: "Nominal GDP",
                    value: gdp[0].value,
                    date: gdp[0].date,
                    unit: "BRL"
                },

                {
                    name: "Primary Balance / GDP",
                    value: primaryBalance[0].value,
                    date: primaryBalance[0].date,
                    unit: "%"
                }
            ];

            setMetrics(loadedMetrics);
        }

        loadMetrics();

    },
        []);

    return (
        <div className="app">

            <h1>Brazil Economic Monitor</h1>
            <p>Dashboard frontend is running.</p>

            <div className="metrics-grid">
                {metrics.map(metric => (
                    <MetricCard
                        key={metric.name}
                        name={metric.name}
                        value={metric.value}
                        date={metric.date}
                        unit={metric.unit}
                    />
                ))}
            </div>
        </div>
    );
}

export default App;