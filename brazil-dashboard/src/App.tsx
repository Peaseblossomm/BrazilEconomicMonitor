import './App.css'
import MetricCard from "./MetricCard";
import { useEffect, useState } from "react";
import "./App.css";


import {
    ResponsiveContainer,
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip
} from "recharts";

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

    const [graphData, setGraphData] =
        useState<Observation[]>([]);

        console.log(graphData);
        

    useEffect(() => {
        async function loadMetrics() {
            const gdpResponse = await fetch(
                "https://localhost:7206/api/dashboard/series/4382/1"
            );

            const gdp: Observation[] =
                await gdpResponse.json();

            const primaryBalanceOverGdpResponse = await fetch(
                "https://localhost:7206/api/dashboard/series/10.07.1_PrimaryBalanceOverGdp/1"
            );

            const primaryBalance: Observation[] =
                await primaryBalanceOverGdpResponse.json();

            const inflationCurrentResponse = await fetch(
                "https://localhost:7206/api/dashboard/series/13522/1"
            );

            const inflation: Observation[] =
                await inflationCurrentResponse.json();

            const selicRateCurrentResponse = await fetch(
                "https://localhost:7206/api/dashboard/series/432/1"
            );

            const selicRate: Observation[] =
                await selicRateCurrentResponse.json();


            const loadedMetrics: Metric[] = [
                {
                    name: "Nominal GDP",
                    value: gdp[0].value,
                    date: new Date(gdp[0].date).toLocaleDateString("en-GB"),
                    unit: "BRL"
                },

                {
                    name: "Primary Balance over GDP",
                    value: primaryBalance[0].value,
                    date: new Date(primaryBalance[0].date).toLocaleDateString("en-GB"),
                    unit: "BRL"
                },

                {
                    name: "Inflation Current",
                    value: inflation[0].value,
                    date: new Date(inflation[0].date).toLocaleDateString("en-GB"),
                    unit: "%"
                },

                {
                    name: "Selic Rate",
                    value: selicRate[0].value,
                    date: new Date(selicRate[0].date).toLocaleDateString("en-GB"),
                    unit: "%"
                }

            ];

            setMetrics(loadedMetrics);
        }

        loadMetrics();

    },
        []);

    useEffect(() => { 

        async function loadGraphData() {

            const response = await fetch(
            "https://localhost:7206/api/dashboard/series/4382/12"

            );

            const data: Observation[] = await response.json();

            setGraphData(data);
        }

        loadGraphData();

        }, []);




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

            <div style={{ width: "600px", height: "300px" }}>
            <ResponsiveContainer width="100%" height="100%">
            <LineChart data={graphData}>
                <CartesianGrid strokeDasharray="3 3" />

                <XAxis dataKey="date" />

                <YAxis />

                <Tooltip />

                <Line
                    type="monotone"
                    dataKey="value"
                />
                </LineChart>
            </ResponsiveContainer>
            </div>
        </div>
    );
}

export default App;