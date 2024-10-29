import { useLayoutEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { HOME_URL } from "@/config";
import titleIcon from "./images/dataScreen-title.png";
import HeaderTime from "./components/HeaderTime";
import RealTimeAccessChart from "./components/RealTimeAccessChart";
import MaleFemaleRatioChart from "./components/MaleFemaleRatioChart";
import ChinaMapChart from "./components/ChinaMapChart";
import HotPlateChart from "./components/HotPlateChart";
import OverNext30Chart from "./components/OverNext30Chart";
import AgeRatioChart from "./components/AgeRatioChart";
import AnnualUseChart from "./components/AnnualUseChart";
import PlatformSourceChart from "./components/PlatformSourceChart";
import "./index.less";

const DataScreen = () => {
  const navigate = useNavigate();
  const dataScreenRef = useRef<HTMLDivElement>(null);

  // Infer scaling based on browser size
  const getScale = (width = 1920, height = 1080) => {
    let ww = window.innerWidth / width;
    let wh = window.innerHeight / height;
    return ww < wh ? ww : wh;
  };

  // Set Transform
  const setDataScreenStyle = () => {
    if (dataScreenRef.current) {
      dataScreenRef.current.style.transform = `scale(${getScale()}) translate(-50%, -50%)`;
      dataScreenRef.current.style.width = `1920px`;
      dataScreenRef.current.style.height = `1080px`;
    }
  };

  useLayoutEffect(() => {
    setDataScreenStyle();
    window.addEventListener("resize", setDataScreenStyle);
    return () => {
      window.removeEventListener("resize", setDataScreenStyle);
    };
  }, []);

  return (
    <div className="dataScreen-container">
      <div className="dataScreen-content" ref={dataScreenRef}>
        <div className="dataScreen-header">
          <div className="header-lf">
            <span className="header-screening" onClick={() => navigate(HOME_URL)}>
              首页
            </span>
          </div>
          <div className="header-ct">
            <div className="header-ct-title">
              <span>智慧旅游可视化大数据展示平台</span>
              <div className="header-ct-warning">平台高峰预警信息（2条）</div>
            </div>
          </div>
          <div className="header-ri">
            <span className="header-download">统计报告</span>
            <span className="header-time">
              当前时间：
              <HeaderTime />
            </span>
          </div>
        </div>
        <div className="dataScreen-main">
          <div className="dataScreen-lf">
            <div className="dataScreen-top">
              <div className="dataScreen-main-title">
                <span>实时游客统计</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <RealTimeAccessChart />
              </div>
            </div>
            <div className="dataScreen-center">
              <div className="dataScreen-main-title">
                <span>男女比例</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <MaleFemaleRatioChart />
              </div>
            </div>
            <div className="dataScreen-bottom">
              <div className="dataScreen-main-title">
                <span>年龄比例</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <AgeRatioChart />
              </div>
            </div>
          </div>
          <div className="dataScreen-ct">
            <div className="dataScreen-map">
              <div className="dataScreen-map-title">景区实时客流量</div>
              <ChinaMapChart />
            </div>
            <div className="dataScreen-cb">
              <div className="dataScreen-main-title">
                <span>未来30天游客量趋势图</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <OverNext30Chart />
              </div>
            </div>
          </div>
          <div className="dataScreen-rg">
            <div className="dataScreen-top">
              <div className="dataScreen-main-title">
                <span>热门景区排行</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <HotPlateChart />
              </div>
            </div>
            <div className="dataScreen-center">
              <div className="dataScreen-main-title">
                <span>年度游客量对比</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <AnnualUseChart />
              </div>
            </div>
            <div className="dataScreen-bottom">
              <div className="dataScreen-main-title">
                <span>预约渠道数据统计</span>
                <img src={titleIcon} alt="" />
              </div>
              <div className="dataScreen-main-chart">
                <PlatformSourceChart />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DataScreen;
