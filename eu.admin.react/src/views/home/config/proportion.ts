import { ECOption } from "@/components/Echarts/config";

export const pieOptionsFn = (isDark: boolean): ECOption => {
  return {
    title: {
      text: "Active Users",
      top: "39%",
      left: "center",
      textStyle: { color: isDark ? "#d1d1d1" : "#222222" }
    },
    tooltip: { trigger: "item" },
    legend: {
      bottom: "0%",
      padding: 20,
      itemGap: 20,
      itemWidth: 30,
      itemHeight: 16,
      orient: "horizontal",
      textStyle: {
        color: isDark ? "#c5c5c5" : "#222222"
      }
    },
    series: [
      {
        name: "Access From",
        type: "pie",
        radius: ["42%", "72%"],
        center: ["50%", "42%"],
        avoidLabelOverlap: false,
        legendHoverLink: false,
        itemStyle: { borderRadius: 12, borderColor: "#fff", borderWidth: 6 },
        label: { show: false },
        emphasis: { label: { show: false } },
        labelLine: { show: false },
        data: [
          {
            value: 620,
            name: "Email",
            itemStyle: {
              color: {
                type: "linear",
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: "#3dd3b2" },
                  { offset: 1, color: "#10a0e6" }
                ]
              }
            }
          },
          {
            value: 735,
            name: "Direct",
            itemStyle: {
              color: {
                type: "linear",
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: "#fa9797" },
                  { offset: 1, color: "#ff75b2" }
                ]
              }
            }
          },
          {
            value: 1048,
            name: "Search Engine",
            itemStyle: {
              color: {
                type: "linear",
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: "#31c5de" },
                  { offset: 1, color: "#4776f7" }
                ]
              }
            }
          },

          {
            value: 604,
            name: "Union Ads",
            itemStyle: {
              color: {
                type: "linear",
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: "#ea83f7" },
                  { offset: 1, color: "#7773f3" }
                ]
              }
            }
          }
        ]
      }
    ]
  };
};
