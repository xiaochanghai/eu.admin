import { ECOption } from "@/components/Echarts/config";

export const trendOptionsFn = (data: number[]): ECOption => {
  return {
    grid: { left: 0, top: 10, bottom: 10, right: 0 },
    xAxis: { show: false, type: "category" },
    yAxis: { show: false },
    series: [
      {
        data,
        type: "line",
        showSymbol: false,
        smooth: true,
        lineStyle: { width: 4, color: "#fafafa" }
      }
    ]
  };
};
