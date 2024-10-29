import { ECOption } from "@/components/Echarts/config";

export const option1Fn = (isDark: boolean): ECOption => {
  return {
    xAxis: {
      type: "category",
      data: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]
    },
    grid: {
      left: "5%",
      right: "5%",
      bottom: "3%",
      top: "8%",
      containLabel: true
    },
    yAxis: {
      type: "value",
      splitLine: {
        show: true,
        lineStyle: {
          color: isDark ? "#5e5e5e" : "#e0e6f1"
        }
      }
    },
    series: [
      {
        data: [120, 200, 150, 80, 70, 110, 130],
        type: "bar",
        color: "#1890FF",
        barWidth: 30
      }
    ]
  };
};

export const option2Fn = (isDark: boolean): ECOption => {
  return {
    tooltip: {
      trigger: "axis"
    },
    grid: {
      left: "3%",
      right: "4%",
      bottom: "3%",
      top: "3%",
      containLabel: true
    },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]
    },
    yAxis: {
      type: "value",
      splitLine: {
        show: true,
        lineStyle: {
          color: isDark ? "#5e5e5e" : "#e0e6f1"
        }
      }
    },
    series: [
      {
        name: "Email",
        type: "line",
        stack: "Total",
        data: [120, 132, 101, 134, 90, 230, 210]
      },
      {
        name: "Union Ads",
        type: "line",
        stack: "Total",
        data: [220, 182, 191, 234, 290, 330, 310]
      },
      {
        name: "Video Ads",
        type: "line",
        stack: "Total",
        data: [150, 232, 201, 154, 190, 330, 410]
      },
      {
        name: "Direct",
        type: "line",
        stack: "Total",
        data: [320, 332, 301, 334, 390, 330, 320]
      }
    ]
  };
};

export const option3Fn = (isDark: boolean): ECOption => {
  return {
    grid: {
      left: "3%",
      right: "4%",
      bottom: "3%",
      top: "3%",
      containLabel: true
    },
    xAxis: {
      axisLabel: {
        color: isDark ? "#757780" : "#5e5e5e"
      },
      axisLine: {
        lineStyle: {
          color: isDark ? "#5e5e5e" : "#e0e6f1"
        }
      },
      splitLine: {
        lineStyle: {
          color: isDark ? "#5e5e5e" : "#e0e6f1"
        }
      }
    },
    yAxis: {
      axisLabel: {
        color: isDark ? "#757780" : "#5e5e5e"
      },
      axisLine: {
        lineStyle: {
          color: isDark ? "#5e5e5e" : "#e0e6f1"
        }
      },
      splitLine: {
        lineStyle: {
          color: isDark ? "#5e5e5e" : "#e0e6f1"
        }
      }
    },
    series: [
      {
        symbolSize: 20,
        data: [
          [10.0, 8.04],
          [8.07, 6.95],
          [13.0, 7.58],
          [9.05, 2.81],
          [12.2, 1.83]
        ],
        type: "scatter"
      },
      {
        symbolSize: 20,
        data: [
          [11.0, 8.33],
          [14.0, 7.66],
          [13.4, 2.81],
          [10.0, 6.33],
          [3.03, 4.23]
        ],
        type: "scatter"
      },
      {
        symbolSize: 20,
        data: [
          [14.0, 8.96],
          [12.5, 6.82],
          [9.15, 7.2],
          [12.5, 3.2],
          [2.02, 4.47]
        ],
        type: "scatter"
      },
      {
        symbolSize: 20,
        data: [
          [1.05, 3.33],
          [4.05, 4.96],
          [6.03, 7.24],
          [12.0, 6.26],
          [12.0, 8.84],
          [7.08, 5.82],
          [5.02, 5.68]
        ],
        type: "scatter"
      }
    ]
  };
};

export const option4Fn = (): ECOption => {
  return {
    radar: {
      indicator: [
        { name: "Sales" },
        { name: "Administration" },
        { name: "Information Technology" },
        { name: "Customer Support" },
        { name: "Development" },
        { name: "Marketing" }
      ],
      alignTicks: true
    },
    series: [
      {
        name: "Budget vs spending",
        type: "radar",
        data: [
          {
            value: [4200, 3000, 20000, 35000, 50000, 18000],
            name: "Allocated Budget"
          },
          {
            value: [5000, 14000, 28000, 26000, 42000, 21000],
            name: "Actual Spending"
          }
        ]
      }
    ]
  };
};

export const option5Fn = (): ECOption => {
  return {
    tooltip: {
      trigger: "item"
    },
    series: [
      {
        name: "Access From",
        type: "pie",
        radius: ["47%", "80%"],
        itemStyle: {
          borderRadius: 10,
          borderColor: "#fff",
          borderWidth: 5
        },
        label: {
          show: true,
          fontSize: 12
        },
        labelLine: {
          show: true
        },
        data: [
          { value: 1048, name: "Search Engine" },
          { value: 735, name: "Direct" },
          { value: 580, name: "Email" },
          { value: 484, name: "Union Ads" },
          { value: 300, name: "Video Ads" }
        ]
      }
    ]
  };
};

export const option6Fn = (): ECOption => {
  return {
    tooltip: {
      formatter: "{a} <br/>{b} : {c}%"
    },
    series: [
      {
        name: "Pressure",
        type: "gauge",
        radius: "90%",
        progress: {
          show: true
        },
        detail: {
          valueAnimation: true,
          formatter: "{value}"
        },
        data: [
          {
            value: 60,
            name: "SCORE",
            itemStyle: {}
          }
        ]
      }
    ]
  };
};
