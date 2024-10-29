import echarts, { ECOption } from "@/components/Echarts/config";

export const overviewTabs = [
  { label: "Today", key: "1" },
  { label: "Past Month", key: "2" },
  { label: "All Time", key: "3" }
];

export const overviewOptionsFn = (isDark: boolean): ECOption => {
  return {
    grid: { top: 30, left: 75, right: 50, bottom: 60 },
    tooltip: {
      trigger: "axis",
      formatter: "<span class='title'>{b0}</span><br /><span class='value'>${c0}</span>"
    },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"],
      axisLine: { show: true }
    },
    yAxis: {
      type: "value",
      min: 0,
      max: 200,
      interval: 50,
      splitLine: { show: true, lineStyle: { color: isDark ? "#484753" : "#dadbde" } }
    },
    series: [
      {
        data: [0, 120, 90, 100, 160, 90, 70, 40, 120, 140, 180, 75],
        type: "line",
        symbolSize: 16,
        smooth: true,
        showSymbol: false,
        symbol: "circle",
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: isDark ? "rgba(83, 227, 196, 0.6)" : "rgba(176, 246, 231, 0.8)" },
            { offset: 1, color: "rgba(194, 246, 235, 0.2)" }
          ])
        },
        itemStyle: { color: "#fff", borderWidth: 4, borderColor: "#2fce9e" },
        lineStyle: { width: 4, color: "#2fce9e" }
      }
    ]
  };
};
